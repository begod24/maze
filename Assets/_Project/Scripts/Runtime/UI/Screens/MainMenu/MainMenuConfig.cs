using PrimeTween;
using UnityEngine;

namespace NoWayOut.UI.Screens.MainMenu
{
    [CreateAssetMenu(menuName = "No Way Out/Configs/Main Menu Config", fileName = "MainMenuConfig")]
    public sealed class MainMenuConfig : ScriptableObject
    {
        public Vector2Int screenResolution = new Vector2Int(512, 384);
        public float idleDistortion = 0.05f;
        public float hoverDistortion = 0.45f;
        public float selectDistortion = 1f;
        public float hoverDuration = 0.25f;
        public Ease hoverEase = Ease.OutQuad;
        public float focusDuration = 1.2f;
        public Ease focusEase = Ease.InOutCubic;
        public float returnDuration = 0.8f;
        public Ease returnEase = Ease.InOutCubic;
        public float lightBoost = 1.5f;
        public float labelHeight = 56f;
        public float errorDisplaySeconds = 2.5f;
    }
}
