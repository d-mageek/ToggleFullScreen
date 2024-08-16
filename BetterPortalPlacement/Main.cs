﻿using MelonLoader;
using System;
using System.Reflection;
using Harmony;
using UnhollowerRuntimeLib;
using UnityEngine;
using BetterPortalPlacement.Utils;
using UnityEngine.XR;
using VRC;
using System.Linq;

[assembly: AssemblyCopyright("Created by " + BetterPortalPlacement.BuildInfo.Author)]
[assembly: MelonInfo(typeof(BetterPortalPlacement.Main), BetterPortalPlacement.BuildInfo.Name, BetterPortalPlacement.BuildInfo.Version, BetterPortalPlacement.BuildInfo.Author)]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonColor(ConsoleColor.DarkMagenta)]

// This mod was firstly proposed and pre-developed by gompo and I continued/finished it
namespace BetterPortalPlacement
{
    public static class BuildInfo
    {
        public const string Name = "BetterPortalPlacement";
        public const string Author = "Elaina";
        public const string Version = "1.0.0";
    }

    public class Main : MelonMod
    {
        private static MelonMod Instance;
        private static PortalPtr portalPtr;
        public static MelonPreferences_Entry<bool> IsModOn;
        public static MelonPreferences_Entry<bool> IsOnlyOnError;
        public static HarmonyInstance HarmonyInstance => Instance.Harmony;
        public static bool PtrIsOn() => portalPtr.enabled;
        public static Vector3 PtrCurrentPos() => portalPtr.position;

        public override void OnApplicationStart()
        {
            Instance = this;
            ClassInjector.RegisterTypeInIl2Cpp<PortalPtr>();
            ClassInjector.RegisterTypeInIl2Cpp<EnableDisableListener>();
            MelonPreferences.CreateCategory("BetterPortalPlacement", "BetterPortalPlacement Settings");
            IsModOn = (MelonPreferences_Entry<bool>)MelonPreferences.CreateEntry("BetterPortalPlacement", nameof(IsModOn), true, "Enable BetterPortalPlacement");
            IsOnlyOnError = (MelonPreferences_Entry<bool>)MelonPreferences.CreateEntry("BetterPortalPlacement", nameof(IsOnlyOnError), false, "Use only on error?");
            Utils.Patches.ApplyPatches();
            MelonLogger.Msg("Successfully loaded!");
        }

        public override void OnUpdate() => VRUtils.OnUpdate();

        public override void VRChat_OnUiManagerInit()
        {
            portalPtr = Utilities.GetPtrObj().AddComponent<PortalPtr>();
            if (XRDevice.isPresent) VRUtils.VRChat_OnUiManagerInit();
            EnableDisableListener QMListener = GameObject.Find("UserInterface/QuickMenu/QuickMenu_NewElements").gameObject.AddComponent<EnableDisableListener>();
            QMListener.OnEnabled += delegate { if (portalPtr.enabled) DisablePointer(); };
            QMListener.OnDisabled += delegate { VRUtils.OnQMDisable(); };
            DisablePointer();
        }

        public static void EnablePointer()
        {
            portalPtr.enabled = true;
            try { VRCUiPopupManager.prop_VRCUiPopupManager_0.Method_Public_Void_0(); } catch { }
        }

        public static void DisablePointer() => portalPtr.enabled = false;

        public static bool CanPlace() => 
            !((from p in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0.ToArray()
                where p != null && p.field_Private_APIUser_0.id != Player.prop_Player_0.field_Private_APIUser_0.id 
                && Vector3.Distance(p.transform.position, portalPtr.position) <= 1.75f
                select p).Count() != 0 ||
                Vector3.Distance(Player.prop_Player_0.transform.position, portalPtr.position) <= 1.75f || // Change this to 1 once I figure that patching out
              (from s in SpawnManager.field_Private_Static_SpawnManager_0.field_Private_List_1_Spawn_0.ToArray()
                where (portalPtr.position - s.transform.position).sqrMagnitude < 9
                select s).Count() != 0);

        public static void RecreatePortal()
        {
            if (!CanPlace())
            {
                portalPtr.audio.Play();
                return;
            }
            var forward = VRCPlayer.field_Internal_Static_VRCPlayer_0.transform.forward;
            Utils.Patches.CreatePortal(
                Utils.Patches.CurrentInfo.ApiWorld,
                Utils.Patches.CurrentInfo.ApiWorldInstance,
                portalPtr.position + (XRDevice.isPresent ? Vector3.one / 2 : - forward * 2),
                XRDevice.isPresent ? VRUtils.GetControllerTransform().forward : forward,
                Utils.Patches.CurrentInfo.WithUIErrors
            );
            DisablePointer();
        }
    }
}