using System;
using System.Linq;
using MSCLoader;
using UnityEngine;

namespace CallThePlumber
{
    public class Invoice : MonoBehaviour
    {
        Material buttonHoverMaterial, buttonIdleMaterial;
        public Action onButtonClicked;

        public float cost;

        void OnEnable()
        {
            this.gameObject.transform.parent.Find("TotalCost/Value").
                GetComponent<TextMesh>().text = $"{cost:F2}";
        }

        void Awake()
        { 
            buttonIdleMaterial = Resources.FindObjectsOfTypeAll<Material>().First(mat => mat.name == "paynow1");
            buttonHoverMaterial = Resources.FindObjectsOfTypeAll<Material>().First(mat => mat.name == "paynow2"); 
        }

        void OnMouseUpAsButton()
        {
            onButtonClicked();
        }

        void OnMouseEnter()
        {
            this.gameObject.GetComponent<MeshRenderer>().sharedMaterial = buttonHoverMaterial;
        }

        void OnMouseExit()
        {
            this.gameObject.GetComponent<MeshRenderer>().sharedMaterial = buttonIdleMaterial;
        }
    }
}
