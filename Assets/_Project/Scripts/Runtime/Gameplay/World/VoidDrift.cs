using UnityEngine;

namespace NoWayOut.Gameplay.World
{
    public sealed class VoidDrift : MonoBehaviour
    {
        private const float FULL_TURN_RADIANS = Mathf.PI * 2f;

        private Vector3 origin;
        private Vector3 spinAxis;
        private float amplitude;
        private float angularFrequency;
        private float phase;
        private float spinSpeed;


        public void Begin(float amplitude, float period, float phase, Vector3 spinAxis, float spinSpeed)
        {
            origin = transform.localPosition;
            this.amplitude = amplitude;
            angularFrequency = FULL_TURN_RADIANS / period;
            this.phase = phase;
            this.spinAxis = spinAxis;
            this.spinSpeed = spinSpeed;
        }


        private void Update()
        {
            float offset = Mathf.Sin(Time.time * angularFrequency + phase) * amplitude;
            transform.localPosition = origin + Vector3.up * offset;
            transform.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
        }
    }
}
