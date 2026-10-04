using UnityEditor;
using UnityEngine;
using Sovereign.Core;
using Sovereign.Data;
using Sovereign.Presentation;

namespace Sovereign.EditorTools
{
    /// <summary>
    /// GDD 18. Attaches a CityscapeView to every nation on the map and hands it the
    /// building prefabs, exactly as dragging them in the Inspector would. The home
    /// nation draws its own economy; the others draw what they are known for.
    /// </summary>
    public static partial class ProjectBuilder
    {
        static CityscapeParameters _cityscapeParameters;

        static CityscapeParameters BuildCityscapeParameters()
        {
            // Field initialisers carry the tuning; stamping keeps a rebuild honest.
            return Asset<CityscapeParameters>(ParamPath + "SO_CityscapeParameters.asset", c =>
            {
                c.millionsPerHouse = 42f;
                c.blockPopulation = 340f;
                c.towerPopulation = 420f;
                c.gdpSharePerBuilding = 0.06f;
            });
        }

        /// <summary>What a foreign nation is known for, by archetype.</summary>
        static SectorId SignatureSector(NationArchetype archetype)
        {
            switch (archetype)
            {
                case NationArchetype.ExportManufacturer: return SectorId.Manufacturing;
                case NationArchetype.OilState: return SectorId.Energy;
                case NationArchetype.EmergingDebtor: return SectorId.Agriculture;
                case NationArchetype.FinancialHaven: return SectorId.Finance;
                case NationArchetype.ResourceDemocracy: return SectorId.Energy;
                default: return SectorId.ServicesRetail;
            }
        }

        static void WireCityscapes(SimulationRunner runner)
        {
            if (BuiltNations == null) return;

            for (int i = 0; i < BuiltNations.Length && i < _nations.Length; i++)
            {
                CityscapeView view = BuiltNations[i].gameObject.AddComponent<CityscapeView>();
                SerializedObject s = new SerializedObject(view);

                s.FindProperty("runner").objectReferenceValue = runner;
                s.FindProperty("parameters").objectReferenceValue = _cityscapeParameters;
                s.FindProperty("landMesh").objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Data/Map/Mesh_Land.asset");
                s.FindProperty("nationIndex").intValue = i;
                s.FindProperty("isHomeNation").boolValue = _nations[i].isPlayerNation;
                s.FindProperty("foreignSector").enumValueIndex = (int)SignatureSector(_nations[i].archetype);

                FillArray(s, "housingPrefabs", _housingPrefabs);
                FillArray(s, "sectorPrefabs", _sectorBuildingPrefabs);
                s.FindProperty("farmlandPrefab").objectReferenceValue = _farmlandPrefab;

                s.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
