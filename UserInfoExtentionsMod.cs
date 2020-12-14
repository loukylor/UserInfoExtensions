using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using Harmony;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.UI;
[assembly: MelonInfo(typeof(UserInfoExtensions.UserInfoExtensionsMod), "UserInfoExtensions", "1.1.1", "loukylor (https://github.com/loukylor/UserInfoExtensions)")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace UserInfoExtensions
{
    class UserInfoExtensionsMod : MelonMod
    {
        public static MenuController menuController;
        public static MethodBase popupV2;
        public static MethodBase popupV1;
        public static MethodBase closePopup;

        public override void OnApplicationStart()
        {
            UIExpansionKit.API.ICustomLayoutedMenu userDetailsMenu = UIExpansionKit.API.ExpansionKitApi.GetExpandedMenu(UIExpansionKit.API.ExpandedMenu.UserDetailsMenu);
            harmonyInstance.Patch(AccessTools.Method(typeof(MenuController), "Method_Public_Void_APIUser_0"), postfix: new HarmonyMethod(typeof(UserInfoExtensionsMod).GetMethod("OnUserInfoOpen", BindingFlags.Static | BindingFlags.Public)));

            userDetailsMenu.AddSimpleButton("Go to Quick Menu", QuickMenuFromSocial.ToQuickMenu);
            userDetailsMenu.AddSimpleButton("Avatar Author", AuthorFromSocialMenu.GetAvatarAuthor);
            userDetailsMenu.AddSimpleButton("Bio", BioButton.GetBio);

            popupV2 = typeof(VRCUiPopupManager).GetMethods()
                .Where(mb => mb.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_") && !mb.Name.Contains("PDM") && CheckMethod(mb, "UserInterface/MenuContent/Popups/StandardPopupV2")).First();
            popupV1 = typeof(VRCUiPopupManager).GetMethods()
                .Where(mb => mb.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_") && !mb.Name.Contains("PDM") && CheckMethod(mb, "UserInterface/MenuContent/Popups/StandardPopup")).First();
            closePopup = typeof(VRCUiPopupManager).GetMethods()
                .Where(mb => mb.Name.StartsWith("Method_Public_Void_") && mb.Name.Length <= 21 && !mb.Name.Contains("PDM") && CheckMethod(mb, "POPUP")).First();

            QuickMenuFromSocial.Init();

            MelonLogger.Log("Initialized!");
        }
        public static void OpenPopupV2(string title, string text, string buttonText, Action onButtonClick) => popupV2.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, new object[5] { title, text, buttonText, (Il2CppSystem.Action) onButtonClick, null });
        public static void OpenPopupV1(string title, string text, string buttonText, Action onButtonClick) => popupV1.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, new object[5] { title, text, buttonText, (Il2CppSystem.Action) onButtonClick, null });

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

        public static void OnUserInfoOpen(MenuController __instance)
        {
            menuController = __instance;
            AuthorFromSocialMenu.OnUserInfoOpen();
        }
    }

    public class QuickMenuFromSocial
    {
        public static MethodBase closeMenu;
        public static MethodBase openQuickMenu;

        public static void Init()
        {
            closeMenu = typeof(VRCUiManager).GetMethods()
                            .Where(mb => mb.Name.StartsWith("Method_Public_Void_Boolean_") && mb.Name.Length <= 29 && UserInfoExtensionsMod.CheckUsed(mb, "Method_Public_Void_Vector3_Quaternion_SpawnOrientation_Boolean_Boolean_")).First();
            openQuickMenu = typeof(QuickMenu).GetMethods()
                                .Where(mb => mb.Name.StartsWith("Method_Public_Void_Boolean_") && mb.Name.Length <= 29 && !mb.Name.Contains("PDM")).First();
        }
        public static void ToQuickMenu()
        {
            APIUser user = UserInfoExtensionsMod.menuController.activeUser;

            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
            {
                if (player.field_Private_APIUser_0 == null) continue;
                if (player.field_Private_APIUser_0.id == user.id)
                {
                    closeMenu.Invoke(VRCUiManager.prop_VRCUiManager_0, new object[] { false }); //Closes Big Menu
                    openQuickMenu.Invoke(QuickMenu.prop_QuickMenu_0, new object[] { true }); //Opens Quick Menu
                    QuickMenu.prop_QuickMenu_0.Method_Public_Void_Player_0(PlayerManager.Method_Public_Static_Player_String_0(user.id)); //Does the rest lmao
                    return;
                }
            }
            UserInfoExtensionsMod.OpenPopupV2("Notice:", "You cannot show this user on the Quick Menu because they are not in the same instance", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
        }
    }

    public class AuthorFromSocialMenu
    {
        public static Uri avatarLink;
        public static bool canGet = true;

        public static void OnUserInfoOpen()
        {
            avatarLink = new Uri(UserInfoExtensionsMod.menuController.activeUser.currentAvatarImageUrl);

            string adjustedLink = string.Format("https://{0}", avatarLink.Authority);

            for (int i = 0; i < avatarLink.Segments.Length - 2; i++)
            {
                adjustedLink += avatarLink.Segments[i];
            }

            avatarLink = new Uri(adjustedLink.Trim("/".ToCharArray()));
        }
        public static void GetAvatarAuthor()
        {
            void OnSuccess(APIUser user)
            {
                if (user.id == UserInfoExtensionsMod.menuController.activeUser.id)
                {
                    UserInfoExtensionsMod.OpenPopupV2("Notice:", "You are already viewing the avatar author", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
                    return;
                }
                GameObject gameObject = GameObject.Find("UserInterface/MenuContent/Screens/UserInfo");
                VRCUiPage vrcUiPage = gameObject.GetComponent<VRCUiPage>();
                VRCUiManager.prop_VRCUiManager_0.Method_Public_VRCUiPage_VRCUiPage_0(vrcUiPage);
                vrcUiPage.Cast<PageUserInfo>().Method_Public_Void_APIUser_PDM_0(user);
            }

            if (avatarLink == null) return;

            if (!canGet)
            {
                UserInfoExtensionsMod.OpenPopupV2("Slow down!", "Please wait a little in between button presses", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
                return;
            }
            MelonCoroutines.Start(StartTimer());

            WebRequest request = WebRequest.Create(avatarLink);

            WebResponse response;
            try
            {
                response = request.GetResponse();
            }
            catch (WebException)
            {
                UserInfoExtensionsMod.OpenPopupV2("Error!", "Something went wrong and the author could not be retreived. Please try again", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
                return;
            }

            if (((HttpWebResponse) response).StatusCode != HttpStatusCode.OK) return;
            using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
            {
                JObject jsonData = (JObject) JsonSerializer.CreateDefault().Deserialize(streamReader, typeof(JObject));

                JsonData requestedData = jsonData.ToObject<JsonData>();
                APIUser.FetchUser(requestedData.ownerId, new Action<APIUser>(OnSuccess), new Action<string>((thing) => { }));
            }

            response.Close();
        }
        public static IEnumerator StartTimer()
        {
            canGet = false;

            float endTime = Time.time + 3.5f;

            while (Time.time < endTime)
            {
                yield return null;
            }

            canGet = true;
            yield break;
        }

        public class JsonData
        {
            [JsonProperty("ownerId")]
            public string ownerId;
        }
    }
    public class BioButton
    {
        public static void GetBio()
        {
            if (UserInfoExtensionsMod.menuController.activeUser.bio.Length >= 100)
            {
                UserInfoExtensionsMod.OpenPopupV1("Bio:", UserInfoExtensionsMod.menuController.activeUser.bio, "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
            }
            else
            {
                UserInfoExtensionsMod.OpenPopupV2("Bio:", UserInfoExtensionsMod.menuController.activeUser.bio, "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
            }
        }
    }
}
