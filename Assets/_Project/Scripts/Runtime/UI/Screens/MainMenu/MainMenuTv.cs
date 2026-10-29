using PrimeTween;
using UnityEngine;
using VContainer;

namespace NoWayOut.UI.Screens.MainMenu
{
    public sealed class MainMenuTv : MonoBehaviour
    {
        private static readonly int SourceTextureId = Shader.PropertyToID("_SourceTex");
        private static readonly int DistortionId = Shader.PropertyToID("_Distortion");
        private static readonly int ScreenTimeId = Shader.PropertyToID("_ScreenTime");
        private static readonly int SeedId = Shader.PropertyToID("_Seed");
        private static readonly int ScreenColorMapId = Shader.PropertyToID("_EmissiveColorMap");

        [SerializeField] private MainMenuItem item;
        [SerializeField] private Texture sourceTexture;
        [SerializeField] private Material crtMaterial;
        [SerializeField] private Renderer screenRenderer;
        [SerializeField] private Renderer bodyRenderer;
        [SerializeField] private Light screenLight;
        [SerializeField] private Transform focusPoint;

        private MainMenuConfig config;
        private CustomRenderTexture screenTexture;
        private Material crtInstance;
        private Material screenInstance;
        private Tween distortionTween;
        private float distortion;
        private float baseLightIntensity;

        public MainMenuItem Item => item;
        public Transform FocusPoint => focusPoint;
        public Bounds Bounds => bodyRenderer.bounds;


        [Inject]
        private void Construct(MainMenuConfig config)
        {
            this.config = config;
        }


        public void AnimateDistortion(float target, float duration, Ease ease)
        {
            distortionTween.Stop();
            distortionTween = Tween.Custom(this, distortion, target, duration, ApplyDistortion, ease);
        }


        private void OnEnable()
        {
            crtInstance = new Material(crtMaterial);
            crtInstance.SetTexture(SourceTextureId, sourceTexture);
            crtInstance.SetFloat(SeedId, (float)item);

            screenTexture = new CustomRenderTexture(config.screenResolution.x, config.screenResolution.y, RenderTextureFormat.ARGB32);
            screenTexture.material = crtInstance;
            screenTexture.updateMode = CustomRenderTextureUpdateMode.Realtime;
            screenTexture.initializationMode = CustomRenderTextureUpdateMode.OnLoad;
            screenTexture.initializationColor = Color.black;
            screenTexture.Create();

            screenInstance = screenRenderer.material;
            screenInstance.SetTexture(ScreenColorMapId, screenTexture);

            baseLightIntensity = screenLight.intensity;
            SetDistortion(config.idleDistortion);
        }


        private void OnDisable()
        {
            distortionTween.Stop();
            screenLight.intensity = baseLightIntensity;
            screenTexture.Release();
            Destroy(screenTexture);
            Destroy(crtInstance);
            Destroy(screenInstance);
        }


        private void Update()
        {
            crtInstance.SetFloat(ScreenTimeId, Time.time);
        }


        private void SetDistortion(float value)
        {
            distortion = value;
            crtInstance.SetFloat(DistortionId, value);
            screenLight.intensity = baseLightIntensity * (1f + value * config.lightBoost);
        }


        private static void ApplyDistortion(MainMenuTv tv, float value)
        {
            tv.SetDistortion(value);
        }
    }
}
