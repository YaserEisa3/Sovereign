using UnityEngine;
using Sovereign.Core;

namespace Sovereign.Presentation
{
    /// <summary>
    /// GDD 18. What a building on the map IS. The map draws a country's economy as
    /// silhouettes; hovering one says which sector it belongs to and how that sector
    /// is actually doing, so the picture is readable without a legend.
    ///
    /// Hit testing is done by distance rather than by physics: these are flat unlit
    /// meshes with no colliders, and a hundred buildings is a cheap loop.
    /// </summary>
    public partial class CityscapeView
    {
        /// <summary>The building nearest a world point, within reach - or null.</summary>
        public Transform BuildingAt(Vector3 worldPoint, float reach)
        {
            Transform best = null;
            float closest = reach * reach;

            foreach (Building building in _buildings)
            {
                if (building.instance == null || building.target <= 0f) continue;
                // Buildings stand ON their point, so measure to the middle of the body
                // rather than to the pivot at its feet.
                // Flat comparison: the map lives on one plane, and the pointer's depth is
                // whatever the camera happened to hand back.
                Vector3 centre = building.instance.transform.position + new Vector3(0f, 0.12f, 0f);
                Vector2 flat = new Vector2(centre.x - worldPoint.x, centre.y - worldPoint.y);
                float distance = flat.sqrMagnitude;
                if (distance > closest) continue;
                closest = distance;
                best = building.instance.transform;
            }
            return best;
        }

        /// <summary>The headline for a hovered building: what it is.</summary>
        public string TitleFor(Transform building)
        {
            if (building == null) return "";
            string name = building.name;

            if (name.StartsWith("Building_Housing")) return "Housing";
            if (name.StartsWith("Building_Farmland")) return "Farmland";

            EconomicSector sector = SectorFor(name);
            return sector != null ? sector.name : name.Replace("Building_", "");
        }

        /// <summary>And how that part of the economy is doing, right now.</summary>
        public string DescriptionFor(Transform building)
        {
            EconomyState state = runner == null ? null : runner.State;
            if (building == null || state == null) return "";
            string name = building.name;

            if (name.StartsWith("Building_Housing"))
                return state.population.Total.ToString("0") + "M people, "
                     + state.unemployment.ToString("0.0") + "% of them out of work. "
                     + "Houses become blocks and then towers as the population grows.";

            EconomicSector sector = SectorFor(name);
            if (sector == null) return "";

            string health = sector.health >= 65f ? "in good shape"
                          : sector.health >= 45f ? "struggling" : "in trouble";
            return (sector.gdpShare * 100f).ToString("0.0") + "% of GDP, "
                 + sector.headcount.ToString("0.0") + "M employed at "
                 + (sector.averageWage / 1000f).ToString("0") + "k average, health "
                 + sector.health.ToString("0") + " - " + health + "."
                 + (name.StartsWith("Building_Farmland") ? " Worked land, not buildings." : "");
        }

        EconomicSector SectorFor(string buildingName)
        {
            EconomyState state = runner == null ? null : runner.State;
            if (state == null) return null;

            if (buildingName.StartsWith("Building_Farmland")) return state.GetSector(SectorId.Agriculture);

            foreach (EconomicSector sector in state.sectors)
                if (buildingName.StartsWith("Building_" + sector.id)) return sector;
            return null;
        }
    }
}
