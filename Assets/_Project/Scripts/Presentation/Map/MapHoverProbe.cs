using UnityEngine;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 18. Watches the pointer over the map and asks whichever country is under it
    /// what the player is looking at, then hands the answer to the dashboard to draw.
    /// Lives on the map camera, beside the zoom.
    /// </summary>
    [RequireComponent(typeof(MapCameraController))]
    public class MapHoverProbe : MonoBehaviour
    {
        [SerializeField] Camera mapCamera;
        [SerializeField] DashboardController dashboard;

        [Tooltip("How close the pointer must be to a building, in world units, at the authored zoom. Scaled with the zoom, so it stays the same on screen.")]
        [Range(0.05f, 2f)] [SerializeField] float reachAtFullZoom = 0.7f;

        CityscapeView[] _countries;
        Transform _hovered;

        void Awake()
        {
            if (mapCamera == null) mapCamera = GetComponent<Camera>();
            _countries = FindObjectsByType<CityscapeView>(FindObjectsSortMode.None);
        }

        void Update()
        {
            if (mapCamera == null || dashboard == null) return;

            if (!PointerOverMap()) { Clear(); return; }
            HoverAt(Input.mousePosition);
        }

        /// <summary>
        /// One hover, from a screen position: find what is under it and tell the
        /// dashboard. Public because a test cannot move a real mouse, and testing the
        /// tooltip without testing the LOOKUP is how this shipped broken the first time.
        /// </summary>
        public Transform HoverAt(Vector3 screenPosition)
        {
            Transform found = BuildingUnderPointer(screenPosition);
            if (found == _hovered)
            {
                if (found != null) dashboard.MoveMapTip(screenPosition);
                return found;
            }

            _hovered = found;
            if (found == null) { dashboard.HideMapTip(); return null; }

            CityscapeView owner = OwnerOf(found);
            dashboard.ShowMapTip(owner.TitleFor(found), owner.DescriptionFor(found), screenPosition);
            return found;
        }

        void Clear()
        {
            if (_hovered == null) return;
            _hovered = null;
            dashboard.HideMapTip();
        }

        public Transform BuildingUnderPointer(Vector3 screenPosition)
        {
            // ScreenToWorldPoint hands back a point on the camera's NEAR PLANE, twenty
            // units in front of the map. Measuring to that found nothing, ever - the
            // hover looked wired up and did nothing at all. The map is the z=0 plane.
            Vector3 world = mapCamera.ScreenToWorldPoint(screenPosition);
            world.z = 0f;
            // The reach follows the zoom: a building is a hair wide across the world
            // view and a block wide up close, and the pointer should feel the same.
            float reach = reachAtFullZoom * (mapCamera.orthographicSize / 21f);

            Transform best = null;
            float bestDistance = float.MaxValue;
            foreach (CityscapeView country in _countries)
            {
                if (country == null) continue;
                Transform hit = country.BuildingAt(world, reach);
                if (hit == null) continue;
                Vector2 flat = new Vector2(hit.position.x - world.x, hit.position.y - world.y);
                float distance = flat.sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = hit;
            }
            return best;
        }

        CityscapeView OwnerOf(Transform building)
        {
            foreach (CityscapeView country in _countries)
                if (country != null && building.IsChildOf(country.transform)) return country;
            return _countries.Length > 0 ? _countries[0] : null;
        }

        bool PointerOverMap()
        {
            Rect rect = mapCamera.rect;
            Vector2 pointer = new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
            return rect.Contains(pointer);
        }
    }
}
