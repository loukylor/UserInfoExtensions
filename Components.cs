using System;
using System.Collections;
using BestHTTP;
using MelonLoader;
using UnhollowerBaseLib.Attributes;
using UnityEngine;
using UserInfoExtensions;

namespace UserInfoExtentions.Component
{
    //Learned from Knah's UIExpansionKit (https://github.com/knah/VRCMods/blob/master/UIExpansionKit/Components/EnableDisableListener.cs)
    public class BioLinksPopup : VRCUiPopup
    {
        public UnityEngine.UI.Button openLinkButton;
        public UnityEngine.UI.ToggleGroup toggleGroup;
        public UnityEngine.UI.Text[] linkTexts;
        public UnityEngine.UI.RawImage[] icons;
        public GameObject[] linkStates;
        public Uri currentLink;

        public new void OnEnable()
        {
            base.OnEnable();
            for (int i = 0; i < linkStates.Length; i++)
            {
                linkStates[i].SetActive(true);
                if (i < BioButtons.bioLinks.Count)
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
            base.OnDisable();
            foreach (GameObject linkstate in linkStates) linkstate.GetComponent<UnityEngine.UI.Toggle>().isOn = false;
        }

        [method: HideFromIl2Cpp]
        public IEnumerator DownloadTexture(int index)
        {
            linkTexts[index].text = BioButtons.bioLinks[index].OriginalString.Length >= 43 ? BioButtons.bioLinks[index].OriginalString.Substring(0, 43) : BioButtons.bioLinks[index].OriginalString;
            HTTPRequest iconRequest = new HTTPRequest(new Il2CppSystem.Uri($"http://www.google.com/s2/favicons?domain_url={BioButtons.bioLinks[index].Host}&sz=128"), new Action<HTTPRequest, HTTPResponse>((HTTPRequest rq, HTTPResponse resp) => OnTextureLoaded(resp, index)));
            try
            {
                iconRequest.Send();
            }
            finally
            {
                iconRequest.Dispose();
            }
            yield break;
        }
        [method: HideFromIl2Cpp]
        public void OnTextureLoaded(HTTPResponse response, int index) => icons[index].texture = response.DataAsTexture2D;

        public void OnOpenLink()
        {
            if (currentLink != null)
            {
                System.Diagnostics.Process.Start(currentLink.OriginalString);
                Close();
                Utilities.OpenPopupV2("Notice:", "Link has been opened in the default browser", "Close", new Action(() => Utilities.ClosePopup()));
                currentLink = null;
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

