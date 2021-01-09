using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BestHTTP;
using Harmony;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnhollowerRuntimeLib;
using UnityEngine;
using VRC;
using VRC.Core;
using VRC.UI;
[assembly: MelonInfo(typeof(UserInfoExtensions.UserInfoExtensionsMod), "UserInfoExtensions", "2.0.2", "loukylor", "https://github.com/loukylor/UserInfoExtensions")]
[assembly: MelonGame("VRChat", "VRChat")]

namespace UserInfoExtensions
{
    class UserInfoExtensionsMod : MelonMod
    {
        public static MenuController menuController;
        public static MethodBase popupV2;
        public static MethodBase popupV1;
        public static MethodBase closePopup;

        public static UIExpansionKit.API.ICustomLayoutedMenu userDetailsMenu;
        public static UIExpansionKit.API.ICustomShowableLayoutedMenu menu;

        public override void OnApplicationStart()
        {
            harmonyInstance.Patch(AccessTools.Method(typeof(MenuController), "Method_Public_Void_APIUser_0"), postfix: new HarmonyMethod(typeof(UserInfoExtensionsMod).GetMethod("OnUserInfoOpen", BindingFlags.Static | BindingFlags.Public)));
            harmonyInstance.Patch(AccessTools.Method(typeof(PageUserInfo), "Back"), postfix: new HarmonyMethod(typeof(UserInfoExtensionsMod).GetMethod("OnUserInfoClose", BindingFlags.Static | BindingFlags.Public)));

            popupV2 = typeof(VRCUiPopupManager).GetMethods()
                .Where(mb => mb.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_") && !mb.Name.Contains("PDM") && CheckMethod(mb, "UserInterface/MenuContent/Popups/StandardPopupV2")).First();
            popupV1 = typeof(VRCUiPopupManager).GetMethods()
                .Where(mb => mb.Name.StartsWith("Method_Public_Void_String_String_String_Action_Action_1_VRCUiPopup_") && !mb.Name.Contains("PDM") && CheckMethod(mb, "UserInterface/MenuContent/Popups/StandardPopup") && !CheckMethod(mb, "UserInterface/MenuContent/Popups/StandardPopupV2")).First();
            closePopup = typeof(VRCUiPopupManager).GetMethods()
                .Where(mb => mb.Name.StartsWith("Method_Public_Void_") && mb.Name.Length <= 21 && !mb.Name.Contains("PDM") && CheckMethod(mb, "POPUP")).First();
            userDetailsMenu = UIExpansionKit.API.ExpansionKitApi.GetExpandedMenu(UIExpansionKit.API.ExpandedMenu.UserDetailsMenu);

            UIExpansionKit.API.LayoutDescription popupLayout = new UIExpansionKit.API.LayoutDescription
            {
                RowHeight = 80,
                NumColumns = 3,
                NumRows = 6
            };
            menu = UIExpansionKit.API.ExpansionKitApi.CreateCustomFullMenuPopup(popupLayout);
            menu.AddLabel("General Things");
            menu.AddSpacer();
            menu.AddSimpleButton("Back", () => menu.Hide());
            userDetailsMenu.AddSimpleButton("UserInfoExtensions", () => { menu.Show(); closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); });

            UserInfoExtensionsSettings.RegisterSettings();
            QuickMenuFromSocial.Init();
            AuthorFromSocialMenu.Init();
            BioButtons.Init();
            OpenInBrowser.Init();

            MelonLogger.Log("Initialized!");
        }
        public override void VRChat_OnUiManagerInit()
        {
            BioButtons.UiInit();
            MelonLogger.Log("UI Initialized!");
        }
        public override void OnModSettingsApplied()
        {
            UserInfoExtensionsSettings.OnModSettingsApplied();
        }

        public static void OnUserInfoOpen(MenuController __instance)
        {
            menuController = __instance;
            AuthorFromSocialMenu.OnUserInfoOpen();
            BioButtons.OnUserInfoOpen();
        }
        public static void OnUserInfoClose()
        {
            menu.Hide();
        }

        public static void HideAll()
        {
            BioButtons.bioLanguagesPopup.Close();
            BioButtons.bioLinksPopup.Close();
            menu.Hide();
            closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null);
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
    }

    public class QuickMenuFromSocial
    {
        public static MethodBase closeMenu;
        public static MethodBase openQuickMenu;

        public static void Init()
        {
            if (UserInfoExtensionsSettings.QuickMenuFromSocialButton) UserInfoExtensionsMod.userDetailsMenu.AddSimpleButton("To Quick Menu", ToQuickMenu);
            UserInfoExtensionsMod.menu.AddSimpleButton("To Quick Menu", ToQuickMenu);

            closeMenu = typeof(VRCUiManager).GetMethods()
                            .Where(mb => mb.Name.StartsWith("Method_Public_Void_Boolean_") && mb.Name.Length <= 29 && UserInfoExtensionsMod.CheckUsed(mb, "Method_Public_Void_Vector3_Quaternion_SpawnOrientation_Boolean_Boolean_")).First();
            openQuickMenu = typeof(QuickMenu).GetMethods()
                                .Where(mb => mb.Name.StartsWith("Method_Public_Void_Boolean_") && mb.Name.Length <= 29 && !mb.Name.Contains("PDM")).First();
        }
        public static void ToQuickMenu()
        {
            UserInfoExtensionsMod.HideAll();

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
        public static Il2CppSystem.Uri avatarLink;
        public static bool canGet = true;

        public static void Init()
        {
            if (UserInfoExtensionsSettings.AuthorFromSocialMenuButton) UserInfoExtensionsMod.userDetailsMenu.AddSimpleButton("Avatar Author", GetAvatarAuthor);
            UserInfoExtensionsMod.menu.AddSimpleButton("Avatar Author", GetAvatarAuthor);
            UserInfoExtensionsMod.menu.AddSpacer();
        }
        public static void OnUserInfoOpen()
        {
            avatarLink = new Il2CppSystem.Uri(UserInfoExtensionsMod.menuController.activeUser.currentAvatarImageUrl);

            string adjustedLink = string.Format("https://{0}", avatarLink.Authority);

            for (int i = 0; i < avatarLink.Segments.Length - 2; i++)
            {
                adjustedLink += avatarLink.Segments[i];
            }

            avatarLink = new Il2CppSystem.Uri(adjustedLink.Trim("/".ToCharArray()));
        }

        public static void GetAvatarAuthor()
        {
            UserInfoExtensionsMod.HideAll();

            if (!canGet)
            {
                UserInfoExtensionsMod.OpenPopupV2("Slow down!", "Please wait a little in between button presses", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
                return;
            }

            MelonCoroutines.Start(StartTimer());

            HTTPRequest request = new HTTPRequest(avatarLink, new Action<HTTPRequest, HTTPResponse>((HTTPRequest rq, HTTPResponse resp) => OnAvatarInfoReceived(resp)));

            try
            {
                request.Send();
            }
            catch (Exception)
            {
                UserInfoExtensionsMod.OpenPopupV2("Error!", "Something went wrong and the author could not be retreived. Please try again", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
                return;
            }
            finally
            {
                request.Dispose();
            }
        }
        private static void OnAvatarInfoReceived(HTTPResponse response)
        {
            JObject jsonData = JObject.Parse(response.DataAsText);
            JsonData requestedData = jsonData.ToObject<JsonData>();
            APIUser.FetchUser(requestedData.ownerId, new Action<APIUser>(OnUserFetched), new Action<string>((thing) => { }));
        }
        private static void OnUserFetched(APIUser user)
        {
            if (user.id == UserInfoExtensionsMod.menuController.activeUser.id)
            {
                UserInfoExtensionsMod.OpenPopupV2("Notice:", "You are already viewing the avatar author", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
                return;
            }
            GameObject gameObject = GameObject.Find("UserInterface/MenuContent/Screens/UserInfo");
            VRCUiPage vrcUiPage = gameObject.GetComponent<VRCUiPage>();
            vrcUiPage.Cast<PageUserInfo>().Method_Public_Void_APIUser_PDM_0(user);
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

    public class BioButtons
    {
        public static UserInfoExtentions.BioLinksPopup bioLinksPopup;
        public static UserInfoExtentions.BioLanguagesPopup bioLanguagesPopup;
        public static List<Uri> bioLinks = new List<Uri>();
        public static List<string> userLanguages = new List<string>();
        public readonly static Dictionary<string, string> languageLookup = new Dictionary<string, string>
        {
            { "eng", "[ eng ] English" },
            { "kor", "[ kor ] 한국어" },
            { "rus", "[ rus ] Русский" },
            { "spa", "[ spa ] Español" },
            { "por", "[ por ] Português" },
            { "zho", "[ zho ] 中文" },
            { "deu", "[ deu ] Deutsch" },
            { "jpn", "[ jpn ] 日本語" },
            { "fra", "[ fra ] Français" },
            { "swe", "[ swe ] Svenska" },
            { "nld", "[ nld ] Nederlands" },
            { "pol", "[ pol ] Polski" },
            { "dan", "[ dan ] Dansk" },
            { "nor", "[ nor ] Norsk" },
            { "ita", "[ ita ] Italiano" },
            { "tha", "[ tha ] ภาษาไทย" },
            { "fin", "[ fin ] Suomi" },
            { "hun", "[ hun ] Magyar" },
            { "ces", "[ ces ] Čeština" },
            { "tur", "[ tur ] Türkçe" },
            { "ara", "[ ara ] العربية" },
            { "ron", "[ ron ] Română" },
            { "vie", "[ vie ] Tiếng Việt" },
            { "ase", "[ ase ] American Sign Language" },
            { "bfi", "[ bfi ] British Sign Language" },
            { "dse", "[ dse ] Dutch Sign Language" },
            { "fsl", "[ fsl ] French Sign Language" },
            { "kvk", "[ kvk ] Korean Sign Language" },
        };

        public static void Init()
        {
            if (UserInfoExtensionsSettings.BioButton) UserInfoExtensionsMod.userDetailsMenu.AddSimpleButton("Bio", GetBio);
            if (UserInfoExtensionsSettings.BioLinksButton) UserInfoExtensionsMod.userDetailsMenu.AddSimpleButton("Bio Links", ShowBioLinksPopup);
            if (UserInfoExtensionsSettings.BioLanguagesButton) UserInfoExtensionsMod.userDetailsMenu.AddSimpleButton("Bio Languages", ShowBioLanguagesPopup);

            UserInfoExtensionsMod.menu.AddLabel("Bio Related Things");
            UserInfoExtensionsMod.menu.AddSpacer();
            UserInfoExtensionsMod.menu.AddSpacer();
            UserInfoExtensionsMod.menu.AddSimpleButton("Bio", GetBio);
            UserInfoExtensionsMod.menu.AddSimpleButton("Bio Links", ShowBioLinksPopup);
            UserInfoExtensionsMod.menu.AddSimpleButton("Bio Languages", ShowBioLanguagesPopup);
        }
        public static void UiInit() //This is a shit show but it works so shshshhhshh
        {
            ClassInjector.RegisterTypeInIl2Cpp<UserInfoExtentions.BioLinksPopup>();
            GameObject popupGameObject = GameObject.Find("UserInterface/MenuContent/Popups/UpdateStatusPopup");
            popupGameObject = UnityEngine.Object.Instantiate(popupGameObject, popupGameObject.transform.parent);
            UnityEngine.Object.DestroyImmediate(popupGameObject.GetComponent<PopupUpdateStatus>());
            UnityEngine.Object.DestroyImmediate(popupGameObject.transform.Find("Popup/StatusSettings/DoNotDisturbStatus").gameObject);
            UnityEngine.Object.DestroyImmediate(popupGameObject.transform.Find("Popup/StatusSettings/OfflineStatus").gameObject);
            UnityEngine.Object.DestroyImmediate(popupGameObject.transform.Find("Popup/InputFieldStatus").gameObject);
            foreach (I2.Loc.Localize component in popupGameObject.GetComponentsInChildren<I2.Loc.Localize>()) UnityEngine.Object.Destroy(component);

            bioLinksPopup = popupGameObject.AddComponent<UserInfoExtentions.BioLinksPopup>();

            bioLinksPopup.screenType = "LINKS_POPUP"; //Required to make popup work

            bioLinksPopup.closePopupButton = popupGameObject.transform.Find("Popup/ExitButton").GetComponent<UnityEngine.UI.Button>();
            bioLinksPopup.closePopupButton.onClick.AddListener((UnityEngine.Events.UnityAction) (() => bioLinksPopup.Close()));

            bioLinksPopup.toggleGroup = popupGameObject.GetComponent<UnityEngine.UI.ToggleGroup>();

            bioLinksPopup.openLinkButton = popupGameObject.transform.Find("Popup/Buttons/UpdateButton").GetComponent<UnityEngine.UI.Button>();
            bioLinksPopup.openLinkButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            bioLinksPopup.openLinkButton.onClick.AddListener(new Action(() => bioLinksPopup.OnOpenLink()));
            bioLinksPopup.openLinkButton.gameObject.name = "OpenLinkButton";
            bioLinksPopup.openLinkButton.transform.GetComponentInChildren<UnityEngine.UI.Text>().text = "Open Link";

            popupGameObject.transform.Find("Popup/UpdateStatusTitleText").GetComponent<UnityEngine.UI.Text>().text = "Open Bio Link";

            bioLinksPopup.linkTexts = new UnityEngine.UI.Text[3];
            bioLinksPopup.icons = new UnityEngine.UI.RawImage[3];
            bioLinksPopup.linkStates = new GameObject[3];
            Transform statusSettings = popupGameObject.transform.Find("Popup/StatusSettings");
            for (int i = 0; i < 3; i++)
            {
                UnityEngine.UI.Toggle toggle = statusSettings.GetChild(i).GetComponent<UnityEngine.UI.Toggle>();
                bioLinksPopup.linkStates[i] = toggle.gameObject;

                UnityEngine.Object.DestroyImmediate(toggle.transform.FindChild("StatusIcon").GetComponent<UiStatusIcon>());
                bioLinksPopup.icons[i] = toggle.transform.FindChild("StatusIcon").GetComponent<UnityEngine.UI.RawImage>();
            
                toggle.transform.FindChild("StatusIcon").name = "WebsiteIcon";

                bioLinksPopup.toggleGroup.RegisterToggle(toggle);
                toggle.group = bioLinksPopup.toggleGroup;
                toggle.onValueChanged.AddListener((UnityEngine.Events.UnityAction<bool>) new Action<bool>((state) =>
                {
                    if (!state)
                    {
                        toggle.transform.FindChild("Description").GetComponent<UnityEngine.UI.Text>().color = new Color(0.5f, 0.5f, 0.5f);
                    }
                    else
                    {
                        toggle.transform.FindChild("Description").GetComponent<UnityEngine.UI.Text>().color = Color.white;
                        
                        bioLinksPopup.currentLink = bioLinks[toggle.transform.GetSiblingIndex()];
                    }
                }));

                toggle.transform.FindChild("Description").GetComponent<UnityEngine.UI.Text>().color = new Color(0.5f, 0.5f, 0.5f);

                bioLinksPopup.linkTexts[i] = toggle.transform.FindChild("Description").GetComponent<UnityEngine.UI.Text>();

                toggle.transform.gameObject.name = $"BioLink{i + 1}";
            }

            bioLinksPopup.gameObject.name = "BioLinksPopup";

            ClassInjector.RegisterTypeInIl2Cpp<UserInfoExtentions.BioLanguagesPopup>();
            popupGameObject = GameObject.Find("UserInterface/MenuContent/Popups/UpdateStatusPopup");
            popupGameObject = UnityEngine.Object.Instantiate(popupGameObject, popupGameObject.transform.parent);
            UnityEngine.Object.DestroyImmediate(popupGameObject.GetComponent<PopupUpdateStatus>());
            UnityEngine.Object.DestroyImmediate(popupGameObject.transform.Find("Popup/StatusSettings/DoNotDisturbStatus").gameObject);
            UnityEngine.Object.DestroyImmediate(popupGameObject.transform.Find("Popup/StatusSettings/OfflineStatus").gameObject);
            UnityEngine.Object.DestroyImmediate(popupGameObject.transform.Find("Popup/InputFieldStatus").gameObject);
            foreach (I2.Loc.Localize component in popupGameObject.GetComponentsInChildren<I2.Loc.Localize>()) UnityEngine.Object.Destroy(component);

            bioLanguagesPopup = popupGameObject.AddComponent<UserInfoExtentions.BioLanguagesPopup>();

            bioLanguagesPopup.screenType = "LANGUAGES_POPUP"; //Required to make popup work

            bioLanguagesPopup.closePopupButton = popupGameObject.transform.Find("Popup/ExitButton").GetComponent<UnityEngine.UI.Button>();
            bioLanguagesPopup.closePopupButton.onClick.AddListener((UnityEngine.Events.UnityAction)(() => bioLanguagesPopup.Close()));

            bioLanguagesPopup.closeButton = popupGameObject.transform.Find("Popup/Buttons/UpdateButton").GetComponent<UnityEngine.UI.Button>();
            bioLanguagesPopup.closeButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
            bioLanguagesPopup.closeButton.onClick.AddListener(new Action(() => bioLanguagesPopup.Close()));
            bioLanguagesPopup.closeButton.gameObject.name = "CloseButton";
            bioLanguagesPopup.closeButton.transform.GetComponentInChildren<UnityEngine.UI.Text>().text = "Close";

            popupGameObject.transform.Find("Popup/UpdateStatusTitleText").GetComponent<UnityEngine.UI.Text>().text = "Bio Languages";

            bioLanguagesPopup.languageTexts = new UnityEngine.UI.Text[3];
            bioLanguagesPopup.languageStates = new GameObject[3];
            statusSettings = popupGameObject.transform.Find("Popup/StatusSettings");
            for (int i = 0; i < 3; i++)
            {
                UnityEngine.Object.Destroy(statusSettings.GetChild(i).GetComponent<UnityEngine.UI.Toggle>());
                bioLanguagesPopup.languageStates[i] = statusSettings.GetChild(i).gameObject;

                UnityEngine.Object.DestroyImmediate(statusSettings.GetChild(i).transform.FindChild("StatusIcon").GetComponent<UiStatusIcon>());

                statusSettings.GetChild(i).transform.FindChild("StatusIcon").GetComponent<UnityEngine.UI.RawImage>().color = Color.white;
                UnityEngine.Object.DestroyImmediate(statusSettings.GetChild(i).transform.FindChild("Highlight").gameObject);

                statusSettings.GetChild(i).transform.FindChild("Description").GetComponent<UnityEngine.UI.Text>().color = Color.white;

                bioLanguagesPopup.languageTexts[i] = statusSettings.GetChild(i).transform.FindChild("Description").GetComponent<UnityEngine.UI.Text>();

                statusSettings.GetChild(i).transform.gameObject.name = $"BioLanguage{i + 1}";
            }

            bioLanguagesPopup.gameObject.name = "BioLanguagesPopup";
        }
        public static void OnUserInfoOpen()
        {
            userLanguages.Clear();
            foreach (string tag in UserInfoExtensionsMod.menuController.activeUser.tags) //Cant use where here because Il2Cpp List and regular List
            {
                if (tag.StartsWith("language_")) userLanguages.Add(languageLookup[tag.Substring(9)]);
            }
        }

        public static void GetBio()
        {
            UserInfoExtensionsMod.HideAll();

            if (UserInfoExtensionsMod.menuController.activeUser.bio != null && UserInfoExtensionsMod.menuController.activeUser.bio.Length >= 100)
            {
                UserInfoExtensionsMod.OpenPopupV1("Bio:", UserInfoExtensionsMod.menuController.activeUser.bio, "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
            }
            else
            {
                UserInfoExtensionsMod.OpenPopupV2("Bio:", UserInfoExtensionsMod.menuController.activeUser.bio, "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
            }
        }
        public static void ShowBioLinksPopup()
        {
            UserInfoExtensionsMod.HideAll();

            CheckLinks(UserInfoExtensionsMod.menuController.activeUser.bioLinks);
            if (UserInfoExtensionsMod.menuController.activeUser.bioLinks == null)
            {
                UserInfoExtensionsMod.OpenPopupV2("Notice:", "Cannot get users links", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
            }
            else if (UserInfoExtensionsMod.menuController.activeUser.bioLinks.Count == 0)
            {
                UserInfoExtensionsMod.OpenPopupV2("Notice:", "This user has no bio links", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
            }
            else if (bioLinks.Count == 0)
            {
                UserInfoExtensionsMod.OpenPopupV2("Notice:", "This user has invalid links", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
            }
            else
            {
                VRCUiManager.prop_VRCUiManager_0.ShowScreenButton("UserInterface/MenuContent/Popups/BioLinksPopup");
            }
        }
        public static void CheckLinks(Il2CppSystem.Collections.Generic.List<string> checkLinks)
        {
            bioLinks = new List<Uri>();
            foreach (string link in checkLinks)
            {
                Uri checkedLink;
                try
                {
                    checkedLink = new Uri(link);
                }
                catch
                {
                    continue;
                }
                bioLinks.Add(checkedLink);
            }
        }

        public static void ShowBioLanguagesPopup()
        {
            UserInfoExtensionsMod.HideAll();

            if (userLanguages == null || userLanguages.Count == 0)
            {
                UserInfoExtensionsMod.OpenPopupV2("Notice:", "This user has no bio languages", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
            }
            else
            {
                VRCUiManager.prop_VRCUiManager_0.ShowScreenButton("UserInterface/MenuContent/Popups/BioLanguagesPopup");
            }
        }
    }

    public class OpenInBrowser
    {
        public static void Init()
        {
            if (UserInfoExtensionsSettings.OpenUserInBrowserButton) UserInfoExtensionsMod.userDetailsMenu.AddSimpleButton("Open User in Browser", OpenUserInBrowser);

            UserInfoExtensionsMod.menu.AddLabel("Website Related Things");
            UserInfoExtensionsMod.menu.AddSpacer();
            UserInfoExtensionsMod.menu.AddSpacer();
            UserInfoExtensionsMod.menu.AddSimpleButton("Open User in Browser", OpenUserInBrowser);
            UserInfoExtensionsMod.menu.AddSpacer();
            UserInfoExtensionsMod.menu.AddSpacer();
        }

        public static void OpenUserInBrowser()
        {
            UserInfoExtensionsMod.HideAll();

            System.Diagnostics.Process.Start("https://vrchat.com/home/user/" + UserInfoExtensionsMod.menuController.activeUserId);
            UserInfoExtensionsMod.OpenPopupV2("Notice:", "User has been opened in the default browser", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
        }
    }
}