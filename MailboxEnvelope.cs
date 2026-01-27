using System;
using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace CallThePlumber
{
    public class MailboxEnvelope : MonoBehaviour
    {
        public GameObject envelopeContent;
        public float billValue;
        public Action onInvoicePaid;
        
        bool isEnvelopeOpen;

        GameObject parentsHouseMailbox;
        PlayMakerFSM openMenuFsm;
        FsmBool playerInMenu;

        void OpenEnvelope ()
        {
            MasterAudio.PlaySound3DAndForget(sType: "HouseFoley", sourceTrans: this.gameObject.transform, variationName: "mail_envelope_open");
            playerInMenu.Value = true;
            openMenuFsm.enabled = false;

            envelopeContent.transform.Find("Camera").tag = "MainCamera";
            isEnvelopeOpen = true;
            envelopeContent.transform.Find("Foreground/PaymentButton").GetComponent<Invoice>().cost = billValue;
            envelopeContent.SetActive(isEnvelopeOpen);
        }


        void ReturnEnvelopeToMailbox()
        {
            MasterAudio.PlaySound3DAndForget(sType: "HouseFoley", sourceTrans: this.gameObject.transform, variationName: "mail_envelope_close");
            playerInMenu.Value = false;
            openMenuFsm.enabled = true;

            envelopeContent.transform.Find("Camera").tag = "Untagged";
            isEnvelopeOpen = false;
            envelopeContent.SetActive(isEnvelopeOpen);
        }

        void SendEnvelopeBack()
        {
            playerInMenu.Value = false;
            openMenuFsm.enabled = true;

            envelopeContent.transform.Find("Camera").tag = "Untagged";
            isEnvelopeOpen = false;
            envelopeContent.SetActive(isEnvelopeOpen);
            this.gameObject.SetActive(false);
        }

        void PayBill()
        {
            FsmFloat playerMoney = FsmVariables.GlobalVariables.GetFsmFloat("PlayerMoney");
            playerMoney.Value -= billValue;
            SendEnvelopeBack();
            onInvoicePaid();
        }

        void Update()
        { 
            if (isEnvelopeOpen) {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ReturnEnvelopeToMailbox();
                }
            }
        }
        
        void Awake()
        {
            isEnvelopeOpen = false;

            openMenuFsm = GameObject.Find("Systems/OptionsDB").GetPlayMaker("Open Menu");
            playerInMenu = FsmVariables.GlobalVariables.GetFsmBool("PlayerInMenu");

            GameObject sheets = GameObject.Find("Sheets");
            parentsHouseMailbox = GameObject.Find("YARD/Others/PlayerMailBox1/");

            // TODO: learn to load assets directly as to not rely on vanilla objects
            GameObject vanillaEnvelopeMesh = parentsHouseMailbox.transform.Find("EnvelopePhoneBill1/envelopemesh").gameObject;

            GameObject envelopeMesh = new("envelopemesh");
            envelopeMesh.transform.SetParent(this.gameObject.transform, worldPositionStays: false);
            envelopeMesh.transform.localPosition = new Vector3(0.0013f, 0.0257f, -0.2023f);
            envelopeMesh.transform.localEulerAngles = new Vector3(0f, 276.15f, 0f);
            envelopeMesh.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);

            MeshFilter meshFilter = envelopeMesh.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = vanillaEnvelopeMesh.GetComponent<MeshFilter>().sharedMesh;

            MeshRenderer meshRenderer = envelopeMesh.AddComponent<MeshRenderer>();
            meshRenderer.material = vanillaEnvelopeMesh.GetComponent<MeshRenderer>().material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = true;

            envelopeContent = sheets.transform.Find("PlumbingBill").gameObject;
            envelopeContent.SetActive(false);

            envelopeContent.transform.Find("Foreground/PaymentButton").gameObject.AddComponent<Invoice>();
            envelopeContent.transform.Find("Foreground/PaymentButton").GetComponent<Invoice>().onButtonClicked = () =>
            {   
                bool playerHasEnoughMoney = FsmVariables.GlobalVariables.GetFsmFloat("PlayerMoney").Value >= billValue;

                if (playerHasEnoughMoney)
                    PayBill();
                else
                    // TODO: Show "Not enough money" message before closing
                    ReturnEnvelopeToMailbox();
            };

        }

        // TODO: Refactor mouse events to MSCLoader unified raycast because mouse events go too far. 
        void OnMouseUpAsButton()
        {
            OpenEnvelope();
        }
        
        void OnMouseOver()
        {
            FsmVariables.GlobalVariables.GetFsmBool("GUIuse").Value = true;
            FsmVariables.GlobalVariables.GetFsmString("GUIinteraction").Value = "Plumbing Service";
        }

        void OnMouseExit()
        {
            FsmVariables.GlobalVariables.GetFsmBool("GUIuse").Value = false;
            FsmVariables.GlobalVariables.GetFsmString("GUIinteraction").Value = "";
        }
    }
}
