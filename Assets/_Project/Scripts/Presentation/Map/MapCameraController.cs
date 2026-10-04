using UnityEngine;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 18. Zoom and pan for the world map. The map is rendered through a hole in
    /// the dashboard, so input is only taken when the pointer is inside that hole -
    /// otherwise scrolling a policy list would sail the camera across the Atlantic.
    /// </summary>
    public class MapCameraController : MonoBehaviour
    {
        [SerializeField] Camera mapCamera;

        [Header("Zoom")]
        [Tooltip("Closest the camera comes in. 2 is a city, 21 is the whole world.")]
        [Range(1f, 20f)] [SerializeField] float minSize = 2.5f;
        [Tooltip("Furthest out - the framing the map is authored at.")]
        [SerializeField] float maxSize = 21f;
        [Tooltip("Size multiplier per wheel notch.")]
        [Range(1.02f, 1.5f)] [SerializeField] float zoomPerNotch = 1.18f;
        [Tooltip("Seconds the camera takes to settle after a zoom or a jump.")]
        [Range(0f, 0.6f)] [SerializeField] float smoothing = 0.12f;

        [Header("Pan")]
        [Tooltip("Hold this to drag the map. Right or middle leaves left-click free.")]
        [SerializeField] int dragButton = 1;
        [Tooltip("How far the centre may travel from the world's middle, in world units.")]
        [SerializeField] Vector2 panLimit = new Vector2(20f, 16f);

        [Header("Keyboard")]
        [Tooltip("Arrow keys and WASD walk the map, in screens of travel per second - so a key press covers the same visible distance at any zoom.")]
        [Range(0.1f, 3f)] [SerializeField] float keyboardScreensPerSecond = 0.9f;

        Vector3 _targetPosition;
        float _targetSize;
        Vector3 _dragOrigin;
        bool _dragging;

        public Camera MapCamera { get { return mapCamera; } }
        public float TargetSize { get { return _targetSize; } }

        void Awake()
        {
            if (mapCamera == null) mapCamera = GetComponent<Camera>();
            _targetPosition = transform.position;
            _targetSize = mapCamera != null ? mapCamera.orthographicSize : maxSize;
            _homePosition = _targetPosition;
            _homeSize = _targetSize;
        }

        Vector3 _homePosition;
        float _homeSize;

        void Update()
        {
            if (mapCamera == null) return;

            if (PointerOverMap())
            {
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.01f) ZoomAt(wheel, mapCamera.ScreenToWorldPoint(Input.mousePosition));

                if (Input.GetMouseButtonDown(dragButton))
                {
                    _dragging = true;
                    _dragOrigin = mapCamera.ScreenToWorldPoint(Input.mousePosition);
                }
            }

            PanWithKeys();

            if (_dragging && !Input.GetMouseButton(dragButton)) _dragging = false;
            if (_dragging)
            {
                // Drag the WORLD, not the camera: the point under the pointer stays put.
                Vector3 now = mapCamera.ScreenToWorldPoint(Input.mousePosition);
                _targetPosition += _dragOrigin - now;
                Clamp();
            }

            float t = smoothing <= 0f ? 1f : 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothing);
            mapCamera.orthographicSize = Mathf.Lerp(mapCamera.orthographicSize, _targetSize, t);
            transform.position = Vector3.Lerp(transform.position, _targetPosition, t);
        }

        /// <summary>
        /// Arrows and WASD walk the map. Speed is measured in screens rather than world
        /// units, so a keypress covers the same visible distance zoomed in on one city
        /// as it does looking at the whole world.
        /// </summary>
        void PanWithKeys()
        {
            float x = 0f, y = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x += 1f;
            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) y -= 1f;
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) y += 1f;
            if (x == 0f && y == 0f) return;

            Vector2 direction = new Vector2(x, y).normalized;
            float step = _targetSize * 2f * keyboardScreensPerSecond * Time.unscaledDeltaTime;
            _targetPosition += new Vector3(direction.x, direction.y, 0f) * step;
            Clamp();
        }

        /// <summary>Moves the view by a fraction of a screen - what one key press does.</summary>
        public void Nudge(Vector2 screens)
        {
            _targetPosition += new Vector3(screens.x, screens.y, 0f) * _targetSize * 2f;
            Clamp();
        }

        /// <summary>Where the view is heading, for the tests.</summary>
        public Vector3 TargetPosition { get { return _targetPosition; } }

        /// <summary>True while the pointer is inside the camera's slice of the screen -
        /// the transparent column the map shows through.</summary>
        bool PointerOverMap()
        {
            Rect rect = mapCamera.rect;
            Vector2 pointer = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
            return rect.Contains(pointer);
        }

        /// <summary>Zooms by notches, keeping the given world point under the pointer.</summary>
        public void ZoomAt(float notches, Vector3 worldPoint)
        {
            float before = _targetSize;
            _targetSize = Mathf.Clamp(_targetSize * Mathf.Pow(zoomPerNotch, -notches), minSize, maxSize);

            // Move the centre so the point under the cursor does not slide away.
            float ratio = before <= 0f ? 1f : _targetSize / before;
            _targetPosition = worldPoint + (_targetPosition - worldPoint) * ratio;
            _targetPosition.z = transform.position.z;
            Clamp();
        }

        /// <summary>For the on-screen buttons: zoom about the middle of the view.</summary>
        public void Zoom(float notches) { ZoomAt(notches, _targetPosition); }

        /// <summary>Frames one thing on the map - a nation, a war, a city.</summary>
        public void Focus(Transform target, float size)
        {
            if (target == null) return;
            _targetSize = Mathf.Clamp(size, minSize, maxSize);
            _targetPosition = new Vector3(target.position.x, target.position.y, transform.position.z);
            Clamp();
        }

        /// <summary>Back to the authored framing of the whole world.</summary>
        public void ResetView()
        {
            _targetPosition = _homePosition;
            _targetSize = _homeSize;
        }

        void Clamp()
        {
            _targetPosition.x = Mathf.Clamp(_targetPosition.x, _homePosition.x - panLimit.x, _homePosition.x + panLimit.x);
            _targetPosition.y = Mathf.Clamp(_targetPosition.y, _homePosition.y - panLimit.y, _homePosition.y + panLimit.y);
        }
    }
}
