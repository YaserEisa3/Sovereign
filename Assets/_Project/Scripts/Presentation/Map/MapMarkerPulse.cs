using UnityEngine;

namespace Sovereign.Presentation
{
    /// <summary>GDD 3.4: the war zone marker pulses. Attached to the prefab, tuned there.</summary>
    public class MapMarkerPulse : MonoBehaviour
    {
        [Tooltip("Pulses per second.")]
        [Range(0.1f, 5f)] [SerializeField] float pulseRate = 1.2f;
        [Tooltip("How far the marker grows at the top of a pulse, as a fraction of its size.")]
        [Range(0f, 1f)] [SerializeField] float amplitude = 0.25f;

        Vector3 _baseScale;

        void Awake() { _baseScale = transform.localScale; }

        void Update()
        {
            float pulse = 1f + amplitude * (0.5f + 0.5f * Mathf.Sin(Time.time * pulseRate * Mathf.PI * 2f));
            transform.localScale = _baseScale * pulse;
        }
    }
}
