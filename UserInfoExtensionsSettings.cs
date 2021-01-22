using MelonLoader;
using System.Reflection;

namespace UserInfoExtensions
{
    public class UserInfoExtensionsSettings
    {
        private const string catagory = "UserInfoExtensionsSettings";

        public static bool QuickMenuFromSocialButton;
        public static bool AuthorFromSocialMenuButton;
        public static bool AuthorFromAvatarMenuButton;
        public static bool BioButton;
        public static bool BioLinksButton;
        public static bool BioLanguagesButton;
        public static bool OpenUserInBrowserButton;

        public static void RegisterSettings()
        {
            MelonPrefs.RegisterCategory(catagory, "UserInfoExtensions Settings");

            MelonPrefs.RegisterBool(catagory, nameof(QuickMenuFromSocialButton), false, "Show \"To Quick Menu\" button");
            MelonPrefs.RegisterBool(catagory, nameof(AuthorFromSocialMenuButton), false, "Show \"Avatar Author\" button in Social Menu");
            MelonPrefs.RegisterBool(catagory, nameof(AuthorFromAvatarMenuButton), true, "Show \"Avatar Author\" button in Avatar Menu");
            MelonPrefs.RegisterBool(catagory, nameof(BioButton), false, "Show \"Bio\" button");
            MelonPrefs.RegisterBool(catagory, nameof(BioLinksButton), false, "Show \"Bio Links\" button");
            MelonPrefs.RegisterBool(catagory, nameof(BioLanguagesButton), false, "Show \"Bio Languages\" button");
            MelonPrefs.RegisterBool(catagory, nameof(OpenUserInBrowserButton), false, "Show \"Open User in Browser\" button");

            OnModSettingsApplied();
        }

        public static void OnModSettingsApplied()
        { 
            foreach (FieldInfo fieldInfo in typeof(UserInfoExtensionsSettings).GetFields())
            {
                fieldInfo.SetValue(null, MelonPrefs.GetBool(catagory, fieldInfo.Name));
            }
        }
    }
}
