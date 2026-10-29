using UnityEngine;

namespace NoWayOut.Rendering.Fog
{
    [ExecuteAlways]
    public sealed class LocalFogVolume : MonoBehaviour
    {
        [SerializeField] private FogVolumeSet volumeSet;
        [SerializeField] private Color albedo = Color.white;
        [SerializeField] private float meanFreePath = 10f;
        [SerializeField] private Vector3 size = new Vector3(10f, 10f, 10f);
        [SerializeField] private Vector3 positiveFade = new Vector3(0.1f, 0.1f, 0.1f);
        [SerializeField] private Vector3 negativeFade = new Vector3(0.1f, 0.1f, 0.1f);
        [SerializeField] private Vector3 noiseTiling = Vector3.one;
        [SerializeField] private Vector3 noiseScrollSpeed;

        public Color Albedo => albedo;
        public Vector3 PositiveFade => positiveFade;
        public Vector3 NegativeFade => negativeFade;
        public Vector3 NoiseScrollSpeed => noiseScrollSpeed;
        public Vector3 NoiseOffset { get; set; }

        public float MeanFreePath
        {
            get => meanFreePath;
            set => meanFreePath = value;
        }

        public Vector3 Size
        {
            get => size;
            set => size = value;
        }

        public Vector3 NoiseTiling
        {
            get => noiseTiling;
            set => noiseTiling = value;
        }


        private void OnEnable()
        {
            if (volumeSet != null)
            {
                volumeSet.Add(this);
            }
        }


        private void OnDisable()
        {
            if (volumeSet != null)
            {
                volumeSet.Remove(this);
            }
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = albedo;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, size);
        }
    }
}
