using Unity.Mathematics;
using UnityEngine;

namespace TMEditorSimple
{
   [CreateAssetMenu(menuName = "Cookie Editor Tools/TM Editor Settings")]
   public class TMEditorSettings : ScriptableObject
   {
      //public List<GameObject> tilePrefabs;
      public float3 cellSize;

      //public List<TileGroup> TileGroups = new List<TileGroup>();
      public SerializableStringAndGameObjectDict groupData;
      public SerializableStringAndMaterialDict materialDictionary;

      public ToolMode lastActiveTool;
      public Material previewobjectMaterial;
   }

   public static class TMEditorUtils
   {
      public static Bounds GetCellBounds(float3 hitPoint, float3 cellSize, out int3 cell)
      {
         cell = WorldToCell(hitPoint, cellSize);
         float3 cellCorner = cell * cellSize;
         return new Bounds(cellCorner + cellSize / 2, cellSize);
      }

      public static int3 WorldToCell(float3 hitPoint, float3 cellSize)
      {
         return (int3) math.floor(hitPoint / cellSize);
      }
      
   }
}