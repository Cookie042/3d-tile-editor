using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TMEditorSimple
{
   internal class TilemapTool_Draw : TilemapEditorTool
   {
      [Header("Drawing")] public bool erasing = false;

      [Header("Replace Options")] public bool replaceTilesOfSameGroup;
      public bool alwaysReplace;

      [Serializable]
      public enum DrawModes
      {
         Brush,
         Line,
         Rect,
         Erase
      }

      private bool _drawing;
      private bool _resizingBrush;

      internal override void OnMouseDown(int button, CellData cellData)
      {
         GUIUtility.hotControl = TMEditor.BlockingID;


         _drawing = true;

         if (!cellData.activeTilePrefab)
            return;

         DrawToCell(cellData);
      }

      private void DrawToCell(CellData cellData)
      {
         for (var i = cellData.overlapTiles.Length - 1; i >= 0; i--)
         {
            Undo.DestroyObjectImmediate(cellData.overlapTiles[i].gameObject);
         }

         //var newTile = Instantiate(cellData.activeTilePrefab);
         var newTile = (GameObject) PrefabUtility.InstantiatePrefab(cellData.activeTilePrefab);
         newTile.isStatic = true;
         var tb = newTile.GetComponent<TileBounds>();

         var cellBounds = TMEditorUtils.GetCellBounds(cellData.worldPosition, editorSettings.cellSize, out _);

         var tileXform = tb.BoxPointToWorldMatrix(TileBounds.BoundsCorner.BottomNearLeft).inverse;
         var boxPoint = tileXform.MultiplyPoint(Vector3.zero);
         Debug.DrawLine(Vector3.zero, boxPoint);

         newTile.transform.position = cellData.cell * editorSettings.cellSize + (float3) boxPoint;
         newTile.transform.RotateAround(tb.BoxPointToWorldMatrix(TileBounds.BoundsCorner.Center).MultiplyPoint(Vector3.zero), Vector3.up, cellData.rotation);

         owningEditor.SortTileObject(newTile);
         
         Undo.RegisterCreatedObjectUndo(newTile, "Draw");
      }

      internal override void OnMouseDrag(int button, CellData cellData)
      {
         if (!math.any(cellData.cell != cellData.lastCell) || button != 0)
            return;

         if (_drawing)
            DrawToCell(cellData);
      }

      internal override void OnMouseUp(int button, CellData cellData)
      {
         _drawing = false;
         erasing = false;
      }

      internal override void OnKeyDown(CellData cellData, KeyCode key)
      {
         switch (key)
         {
            case KeyCode.D:
            {
               erasing = true;
               if (cellData.overlapTiles.Length != 0)
                  DestroyTile(cellData);
               Event.current.Use();
               break;
            }
            case KeyCode.Q:
            {
               foreach (TileBounds tile in cellData.overlapTiles)
               {
                  var ltw = tile.BoxPointToWorldMatrix(TileBounds.BoundsCorner.Center);
                  var center = ltw.MultiplyPoint(Vector3.zero);

                  tile.gameObject.transform.RotateAround(center, Vector3.up, -90);
               }

               Event.current.Use();
               break;
            }
            case KeyCode.E:
            {
               foreach (TileBounds tile in cellData.overlapTiles)
               {
                  var ltw = tile.BoxPointToWorldMatrix(TileBounds.BoundsCorner.Center);
                  var center = ltw.MultiplyPoint(Vector3.zero);

                  tile.gameObject.transform.RotateAround(center, Vector3.up, 90);
               }

               Event.current.Use();
               break;
            }
            case KeyCode.C:
            {
               if (cellData.overlapTiles.Length > 0)
               {
                  var tile = cellData.overlapTiles[0].gameObject;
                  var prefab = PrefabUtility.GetCorrespondingObjectFromSource(tile.gameObject);

                  owningEditor.activeRotation = tile.transform.rotation.eulerAngles.y;
                  
                  owningEditor.selectedTilePrefab = prefab;
                  owningEditor.Repaint();
               }
               Event.current.Use();

               break;
            }
         }
      }

      internal override void OnKeyUp(CellData cellData, KeyCode key)
      {
         if (key == KeyCode.D) erasing = false;
      }

      internal override void OnDrawHandles(CellData cellData, Camera camera, out Color boundsColor)
      {
         boundsColor = Color.cyan;

         Handles.Label(cellData.worldPosition, cellData.cell.ToString());
      }

      internal override void OnMouseMove(CellData cellData)
      {
         if (erasing && math.any(cellData.cell != cellData.lastCell))
            DestroyTile(cellData);

         if (_resizingBrush)
         {
         }
      }

      // util methods
      private static void DestroyTile(CellData cellData)
      {
         for (var i = cellData.overlapTiles.Length - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(cellData.overlapTiles[0].gameObject);
      }
   }

   [CustomEditor(typeof(TilemapTool_Draw))]
   public class TilemapTool_DrawEditor : Editor
   {
      private TilemapTool_Draw tar;

      private void OnEnable()
      {
         tar = target as TilemapTool_Draw;
         //Debug.Log("OnEnable");
      }

      public int selectedId = 0;
      private bool _toggled = true;

      public override VisualElement CreateInspectorGUI()
      {
         StyleSheet selectedTBStyle = Resources.Load<StyleSheet>("TMEditorUi");

         var ve = new VisualElement();

         var tb = new Toolbar();
         tb.Add(new ToolbarButton());
         tb.Add(new ToolbarButton());
         tb.Add(new ToolbarButton());

         foreach (var visualElement in tb.Children())
         {
            ToolbarButton child = (ToolbarButton) visualElement;
            if (child != null)
            {
               child.styleSheets.Add(selectedTBStyle);
            }
         }

         IMGUIContainer defaultInspectorIMGUI = new IMGUIContainer()
         {
            onGUIHandler = () => { DrawDefaultInspector(); }
         };

         ve.Add(tb);
         ve.Add(defaultInspectorIMGUI);

         return ve;
      }
   }
}