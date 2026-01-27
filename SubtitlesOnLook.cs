using HutongGames.PlayMaker;
using MSCLoader;
using UnityEngine;

namespace CallThePlumber
{
    internal class SubtitlesOnLook : MonoBehaviour
    {
        public Collider collider;
        public string subtitleText;
        public float maxHitDistance;

        bool wasOverCollider;
        FsmString vanillaSubtitles;
        RaycastHit hit;

        void Awake()
        {
            wasOverCollider = false;
            maxHitDistance = 1f;
            vanillaSubtitles = FsmVariables.GlobalVariables.GetFsmString("GUIsubtitle");
        }

        void Update()
        {
            hit = UnifiedRaycast.GetRaycastHit();
            bool isColliderHit = hit.collider == collider && hit.distance < maxHitDistance;

            if (isColliderHit)
            {
                vanillaSubtitles.Value = subtitleText;
                wasOverCollider = true;
            }
            else if (wasOverCollider)
            {
                vanillaSubtitles.Value = "";
                wasOverCollider = false;
            }
        }
    }
}
