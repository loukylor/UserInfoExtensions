using System;
using System.Collections;
using System.IO;
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
[assembly: MelonInfo(typeof(UserInfoExtensions.UserInfoExtensionsMod), "UserInfoExtensions", "1.0.0", "loukylor (https://github.com/loukylor/UserInfoExtension)")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace UserInfoExtensions
{
    class UserInfoExtensionsMod : MelonMod
    {
        public static MenuController menuController;
        public override void OnApplicationStart()
        {
            UIExpansionKit.API.ICustomLayoutedMenu userDetailsMenu = UIExpansionKit.API.ExpansionKitApi.GetExpandedMenu(UIExpansionKit.API.ExpandedMenu.UserDetailsMenu);
            harmonyInstance.Patch(AccessTools.Method(typeof(MenuController), "Method_Public_Void_APIUser_0"), postfix: new HarmonyMethod(typeof(UserInfoExtensionsMod).GetMethod("OnUserInfoOpen", BindingFlags.Static | BindingFlags.Public)));

            userDetailsMenu.AddSimpleButton("Go to Quick Menu", QuickMenuFromSocial.ToQuickMenu);
            userDetailsMenu.AddSimpleButton("Avatar Author", AuthorFromSocialMenu.GetAvatarAuthor);

            MelonLogger.Log("Initialized!");
        }

        public static void OnUserInfoOpen(MenuController __instance)
        {
            menuController = __instance;
            AuthorFromSocialMenu.OnUserInfoOpen();
        }
    }

    class QuickMenuFromSocial
    {
        public static void ToQuickMenu()
        {
            APIUser user = UserInfoExtensionsMod.menuController.activeUser;

            foreach (Player player in PlayerManager.field_Private_Static_PlayerManager_0.field_Private_List_1_Player_0)
            {
                if (player.field_Private_APIUser_0 == null) continue;
                if (player.field_Private_APIUser_0.id == user.id)
                {
                    VRCUiManager.prop_VRCUiManager_0.Method_Public_Void_Boolean_0(); //Closes Big Menu
                    QuickMenu.prop_QuickMenu_0.Method_Public_Void_Boolean_0(true); //Opens Quick Menu
                    QuickMenu.prop_QuickMenu_0.Method_Public_Void_Player_0(PlayerManager.Method_Public_Static_Player_String_0(user.id)); //Does the rest lmao
                    return;
                }
            }
            VRCUiPopupManager.prop_VRCUiPopupManager_0.Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_1("Notice:", "You cannot show this user on the Quick Menu because they are not in the same instance", "Close", new Action(() => { VRCUiManager.prop_VRCUiManager_0.prop_VRCUiPopupManager_0.Method_Public_Void_3(); }));
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
                    VRCUiPopupManager.prop_VRCUiPopupManager_0.Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_1("Notice:", "You are already viewing the avatar author", "Close", new Action(() => { VRCUiManager.prop_VRCUiManager_0.prop_VRCUiPopupManager_0.Method_Public_Void_3(); }));
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
                VRCUiPopupManager.prop_VRCUiPopupManager_0.Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_1("Slow down", "Please wait a little in between button presses", "Close", new Action(() => { VRCUiManager.prop_VRCUiManager_0.prop_VRCUiPopupManager_0.Method_Public_Void_3(); }));
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
                VRCUiPopupManager.prop_VRCUiPopupManager_0.Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_1("Error!", "Something went wrong and the author could not be retreived. Please try again", "Close", new Action(() => { VRCUiManager.prop_VRCUiManager_0.prop_VRCUiPopupManager_0.Method_Public_Void_3(); }));
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
}
