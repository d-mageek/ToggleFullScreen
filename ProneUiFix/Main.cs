﻿using ProneUiFix.Utils;
using System.Linq;
using System.Reflection;
using System.Collections;
using UnhollowerRuntimeLib;
using MelonLoader;
using UnityEngine;
using UnityEngine.XR;

[assembly: AssemblyCopyright("Created by " + ProneUiFix.BuildInfo.Author)]
[assembly: MelonInfo(typeof(ProneUiFix.Main), ProneUiFix.BuildInfo.Name, ProneUiFix.BuildInfo.Version, ProneUiFix.BuildInfo.Author)]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonColor(System.ConsoleColor.DarkMagenta)]

namespace ProneUiFix
{
    public static class BuildInfo
    {
        public const string Name = "ProneUiFix";
        public const string Author = "Elaina";
        public const string Version = "1.0.2";
    }

    public class Main : MelonMod
    {
        private static MethodInfo placeUi;
        private static MethodInfo PlaceUiMethod
        {
            get
            {
                if (placeUi == null) placeUi = typeof(VRCUiManager).GetMethods()
                     .Where(m => 
                         m.Name.StartsWith("Method_Public_Void_Boolean_Boolean_") && !m.Name.Contains("PDM") &&
                         m.GetParameters().Where(p => p.RawDefaultValue.ToString().Contains("False")).Count() == 2)
                     .OrderBy(method => UnhollowerSupport.GetIl2CppMethodCallerCount(method)).First();
                return placeUi;
            }
        }

        public override void OnApplicationStart()
        {
            ClassInjector.RegisterTypeInIl2Cpp<EnableDisableListener>();

            static IEnumerator OnUiManagerInit()
            {
                while (VRCUiManager.prop_VRCUiManager_0 == null)
                    yield return null;
                VRChat_OnUiManagerInit();
            }
            MelonCoroutines.Start(OnUiManagerInit());

            MelonLogger.Msg("Successfully loaded!");
        }

        private static void VRChat_OnUiManagerInit()
        {
            if (!XRDevice.isPresent)
                GameObject.Find("UserInterface/MenuContent/Backdrop/Backdrop")
                    .AddComponent<EnableDisableListener>().OnEnabled += delegate { MelonCoroutines.Start(PlaceMenuAgain()); };
        }

        private static IEnumerator PlaceMenuAgain()
        {
            yield return new WaitForSeconds(1);
            PlaceUiMethod.Invoke(VRCUiManager.prop_VRCUiManager_0, new object[] { false, false });
        }
    }
}