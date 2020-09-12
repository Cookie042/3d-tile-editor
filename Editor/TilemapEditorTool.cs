using System;
using Unity.Mathematics;
using UnityEngine;

namespace TMEditorSimple
{
   public struct CellData
   {
      public int3 cell;
      public int3 lastCell;
      public float3 worldPosition;

      public GameObject activeTilePrefab;
      public float rotation;
      
      public TileBounds tile;
      public TileBounds[] overlapTiles;

      public override string ToString() => 
         $"Name: {(tile != null ? tile.gameObject.name : "NULL")} cell: {cell} CursorPos: {worldPosition}";
   }

   [Serializable]
   public class TilemapEditorTool : ScriptableObject
   {
      [NonSerialized] public TMEditorSettings editorSettings;
      [NonSerialized] public TMEditor owningEditor;

      internal virtual void OnMouseDown(int button, CellData cellData){}
      internal virtual void OnMouseDrag(int button, CellData cellData){}
      internal virtual void OnMouseUp(int button, CellData cellData){}
      internal virtual void OnKeyDown(CellData cellData, KeyCode key){}
      internal virtual void OnKeyUp(CellData cellData, KeyCode key){}
      internal virtual void OnDrawHandles(CellData cellData, Camera camera, out Color boundsColor){boundsColor = Color.red;}
      internal virtual void OnMouseMove(CellData cellData){}
   }
}