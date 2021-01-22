using System;
using System.Linq;
using System.Reflection;
using VRC.Core;

namespace UserInfoExtentions
{
    class Utilities
    {
        private static MethodInfo popupV2;
        private static MethodInfo popupV1;
        private static MethodInfo closePopup;

        public static MenuController menuController;
        public static APIUser ActiveUser
        {
            get { return menuController.activeUser; }
        }

        public static void Init()
        {
            popupV2 = typeof(VRCUiPopupManager).GetMethods()
                .Where(mb => mb.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_") && !mb.Name.Contains("PDM") && CheckMethod(mb, "UserInterface/MenuContent/Popups/StandardPopupV2")).First();
            popupV1 = typeof(VRCUiPopupManager).GetMethods()
                .Where(mb => mb.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_") && !mb.Name.Contains("PDM") && CheckMethod(mb, "UserInterface/MenuContent/Popups/StandardPopup") && !CheckMethod(mb, "UserInterface/MenuContent/Popups/StandardPopupV2")).First();
            closePopup = typeof(VRCUiPopupManager).GetMethods()
                .Where(mb => mb.Name.StartsWith("Method_Public_Void_") && mb.Name.Length <= 21 && !mb.Name.Contains("PDM") && CheckMethod(mb, "POPUP")).First();
        }

        public static void UiInit()
        {
            menuController = QuickMenu.prop_QuickMenu_0.menuController;
        }

        public static void OpenPopupV2(string title, string text, string buttonText, Action onButtonClick) => popupV2.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, new object[5] { title, text, buttonText, (Il2CppSystem.Action)onButtonClick, null });
        public static void OpenPopupV1(string title, string text, string buttonText, Action onButtonClick) => popupV1.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, new object[5] { title, text, buttonText, (Il2CppSystem.Action)onButtonClick, null });
        public static void ClosePopup() => closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null);

        //This method is practically stolen from https://github.com/BenjaminZehowlt/DynamicBonesSafety/blob/master/DynamicBonesSafetyMod.cs
        public static bool CheckMethod(MethodBase methodBase, string match)
        {
            try
            {
                return UnhollowerRuntimeLib.XrefScans.XrefScanner.XrefScan(methodBase)
                    .Where(instance => instance.Type == UnhollowerRuntimeLib.XrefScans.XrefType.Global && instance.ReadAsObject().ToString().Contains(match)).Any();
            }
            catch { }
            return false;
        }
        public static bool CheckUsed(MethodBase methodBase, string methodName)
        {
            try
            {
                return UnhollowerRuntimeLib.XrefScans.XrefScanner.UsedBy(methodBase)
                    .Where(instance => instance.TryResolve() == null ? false : instance.TryResolve().Name.Contains(methodName)).Any();
            }
            catch { }
            return false;

        }
    }
}
