using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace CallThePlumber
{
    internal class SubtitlesOnLook : MonoBehaviour
    {
        public Collider collider;
        public string subtitleText;

        FsmString vanillaSubtitles;

        void Awake()
        {
            vanillaSubtitles = FsmVariables.GlobalVariables.GetFsmString("GUIsubtitle");
        }

        void Update()
        {
            if (UnifiedRaycast.GetHit(collider))
                vanillaSubtitles.Value = subtitleText; 
            else vanillaSubtitles.Value = "";
        }
    }
}
