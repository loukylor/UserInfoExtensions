using System;
using System.Collections;
using MelonLoader;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using UnityEngine.Networking;
using UserInfoExtensions;

namespace UserInfoExtentions
{
    //Learned from Knah's UIExpansionKit (https://github.com/knah/VRCMods/blob/master/UIExpansionKit/Components/EnableDisableListener.cs)
    public class BioLinksPopup : VRCUiPopup
    {
        public UnityEngine.UI.Button openLinkButton;
        public UnityEngine.UI.ToggleGroup toggleGroup;
        public UnityEngine.UI.Text[] linkTexts;
        public UnityEngine.UI.RawImage[] icons;
        public GameObject[] linkStates;
        public string currentLink;

        public new void OnEnable()
        {
            base.OnEnable();
            for (int i = 0; i < linkStates.Length; i++)
            {
                linkStates[i].SetActive(true);
                if (i < UserInfoExtensionsMod.menuController.activeUser.bioLinks.Count)
                {
                    MelonCoroutines.Start(DownloadTexture(i));
                }
                else
                {
                    linkStates[i].SetActive(false);
                }
            }
        }
        public new void OnDisable()
        {
            foreach (GameObject linkstate in linkStates) linkstate.GetComponent<UnityEngine.UI.Toggle>().isOn = false;
        }

        [method: HideFromIl2Cpp]
        public IEnumerator DownloadTexture(int index)
        {
            Uri link = new Uri(UserInfoExtensionsMod.menuController.activeUser.bioLinks[index]);
            linkTexts[index].text = link.Host;
            UnityWebRequest iconRequest = UnityWebRequestTexture.GetTexture($"http://www.google.com/s2/favicons?domain_url={link.Host}&sz=128");
            try
            {
                UnityWebRequestAsyncOperation asyncOperation = iconRequest.SendWebRequest();
                while (!asyncOperation.isDone) yield return null;
                icons[index].texture = DownloadHandlerTexture.GetContent(iconRequest);
            }
            finally
            {
                iconRequest.Dispose();
            }
            yield break;
        }

        public void OnOpenLink()
        {
            if (currentLink != "")
            {
                System.Diagnostics.Process.Start(currentLink);
                Close();
                UserInfoExtensionsMod.OpenPopupV2("Notice:", "Link has been opened in the default browser", "Close", new Action(() => { UserInfoExtensionsMod.closePopup.Invoke(VRCUiPopupManager.prop_VRCUiPopupManager_0, null); }));
            }
        }

        public unsafe BioLinksPopup(IntPtr obj0) : base(obj0)
        {
        }
    }
    public class BioLanguagesPopup : VRCUiPopup
    {
        public UnityEngine.UI.Button closeButton;
        public UnityEngine.UI.Text[] languageTexts;
        public GameObject[] languageStates;

        public new void OnEnable()
        {
            base.OnEnable();
            for (int i = 0; i < languageStates.Length; i++)
            {
                if (i < BioButtons.userLanguages.Count)
                {
                    languageTexts[i].text = BioButtons.userLanguages[i];
                    languageStates[i].SetActive(true);
                }
                else
                {
                    languageStates[i].SetActive(false);
                }
            }
        }

        public unsafe BioLanguagesPopup(IntPtr obj0) : base(obj0)
        {
        }
    }
}

