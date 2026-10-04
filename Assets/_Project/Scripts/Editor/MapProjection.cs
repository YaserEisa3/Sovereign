using UnityEngine;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// Web Mercator, the projection every online map uses - which is why it reads as
    /// "a real map" rather than a diagram. It also happens to suit the dashboard's
    /// tall narrow column: Mercator stretches high latitudes, so the world comes out
    /// roughly square instead of the 2:1 band an equirectangular projection gives.
    ///
    /// Latitude is clamped well short of the poles because Mercator sends 90 degrees
    /// to infinity.
    /// </summary>
    public static class MapProjection
    {
        /// <summary>World units per radian of longitude. 180 degrees lands on x = +-18.</summary>
        public const float Scale = 18f / Mathf.PI;

        /// <summary>Mercator blows up at the poles; 82 degrees keeps Antarctica a band rather than infinity.</summary>
        public const float LatitudeLimit = 82f;

        public static Vector2 Project(float longitudeDegrees, float latitudeDegrees)
        {
            float lat = Mathf.Clamp(latitudeDegrees, -LatitudeLimit, LatitudeLimit);
            float x = longitudeDegrees * Mathf.Deg2Rad * Scale;
            float y = Mathf.Log(Mathf.Tan(Mathf.PI * 0.25f + lat * Mathf.Deg2Rad * 0.5f)) * Scale;
            return new Vector2(x, y);
        }

        /// <summary>Half-width of the whole world in world units.</summary>
        public static float HalfWidth { get { return Mathf.PI * Scale; } }

        /// <summary>Half-height of the projected world at the latitude limit.</summary>
        public static float HalfHeight
        {
            get { return Mathf.Log(Mathf.Tan(Mathf.PI * 0.25f + LatitudeLimit * Mathf.Deg2Rad * 0.5f)) * Scale; }
        }
    }
}
