using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TMEditorSimple
{
   public class TilemapTool_Select : TilemapEditorTool
   {
      [Range(0, 10)] public float b;

      public bool selectTouching;

      private bool debug = true;

      private Color _modeColor = Color.red;
      public Color modeColor => _modeColor;

      //private bool _selecting;

      private Bounds selectionBounds;

      private List<GameObject> selection = new List<GameObject>();

      internal override void OnMouseDown(int button, CellData cellData)
      {
         GUIUtility.hotControl = TMEditor.BlockingID;
        // _selecting = true;
         selection.Clear();

         Bounds bounds = TMEditorUtils.GetCellBounds(cellData.cell, editorSettings.cellSize, out _);
         selectionBounds = bounds;
         Event.current.Use();
      }

      internal override void OnMouseDrag(int button, CellData cellData)
      {
         if (math.any(cellData.cell != cellData.lastCell))
         {
            // Debug.Log($"{cellData.cell} : {cellData.lastCell}");
            // Debug.Log($"button:{button} Drag");

            selectionBounds.Encapsulate(TMEditorUtils.GetCellBounds(cellData.cell, editorSettings.cellSize, out _));
         }
      }

      internal override void OnMouseUp(int button, CellData cellData)
      {
         var objects = Physics.OverlapBox(selectionBounds.center, selectTouching ? selectionBounds.extents : selectionBounds.extents * .9f)
            .Where(o => o.GetComponent<TileBounds>() != null)
            .Select(c => (Object) c.gameObject)
            .ToArray();
         Selection.objects = objects;
      }

      internal override void OnKeyDown(CellData cellData, KeyCode key)
      {
         //Debug.Log(Event.current.keyCode);
      }

      internal override void OnKeyUp(CellData cellData, KeyCode key)
      {
         Debug.Log(Event.current.keyCode);
      }

      internal override void OnDrawHandles(CellData cellData, Camera camera, out Color color)
      {
         Handles.color = Color.magenta;
         Handles.DrawWireCube(selectionBounds.center, selectionBounds.size);

         float3 cellSize = editorSettings.cellSize;
         Handles.matrix = Matrix4x4.TRS(
            cellData.cell * cellSize + cellSize / 2,
            Quaternion.identity,
            Vector3.one);

         // Handles.color = Color.yellow;
         // Handles.DrawWireCube(Vector3.zero, cellSize);

         var floorPos = cellData.worldPosition;
         floorPos.y = 0;
         Handles.matrix = Matrix4x4.identity;
         Handles.color = Color.red;
         Handles.DrawLine(floorPos, cellData.worldPosition);

         color = new Color(0f, 1f, 0f, 0.33f);
      }

      internal override void OnMouseMove(CellData cellData)
      {
         //if(debug) Debug.Log($"{nameof(OnMouseMove)} : {cellData}");         
      }

   }
}