using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static TileBounds;

[Serializable]
public enum ToolMode
{
    Select, Draw, Rect
}

namespace TMEditorSimple
{
    public class TMEditor : EditorWindow
    {
        [SerializeField] private TMEditorSettings editorSettings;
        [SerializeField] public GameObject selectedTilePrefab;
        [SerializeField] public float activeRotation = 0;

        private Dictionary<string, Transform> _groupParents = new Dictionary<string, Transform>();

        private Editor _selEditor;
        private VisualElement _previewArea;

        private string _activeKey = "";
        private float _drawHeight = .5f;
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static int _blockingId;
        private int3 _lastCell = new int3(10000);
        private bool _inputDisabled;
        private MaterialPropertyBlock _materialPropertyBlock;

        private readonly List<KeyCode> downKeys = new List<KeyCode>();

        public static int BlockingID => _blockingId;

        private Dictionary<ToolMode, TilemapEditorTool> _tilemapTools;
        private Dictionary<ToolMode, Editor> _tmEditors = new Dictionary<ToolMode, Editor>();
        [SerializeField] private ToolMode _activeToolMode = ToolMode.Select;

        [MenuItem("Tools/TE &#E")]
        public static void ShowWindow()
        {
            GetWindow<TMEditor>();
        }

        private void OnEnable()
        {
            //init height at half the height of a cell
            _drawHeight = .5f;

            _blockingId = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(_blockingId);

            var root = rootVisualElement;
            var windowSo = new SerializedObject(this);

            VisualTreeAsset treeAsset = Resources.Load<VisualTreeAsset>("TMEditorUi");
            VisualTreeAsset prefabVisualElement = Resources.Load<VisualTreeAsset>("PrefabUi");

            if (treeAsset) treeAsset.CloneTree(root);

            //setting object field types for the main window input fields;
            ObjectField settingsField = root.Q<ObjectField>("settings-field");
            settingsField.objectType = typeof(TMEditorSettings);

            ObjectField activeObjectField = root.Q<ObjectField>("selected-object-field");
            activeObjectField.objectType = typeof(GameObject);

            editorSettings = Resources.Load<TMEditorSettings>(nameof(TMEditorSettings));

            _activeToolMode = editorSettings.lastActiveTool;
            //Drawing the inspector for the ScriptableObject
            var settings = root.Q<IMGUIContainer>("settings-editor");
            settings.onGUIHandler = () =>
            {
                if (editorSettings != null)
                {
                    var editor = Editor.CreateEditor(editorSettings);
                    editor.OnInspectorGUI();
                }
            };


            // toolSettings.onGUIHandler = () =>
            // {
            //    if (_tilemapTools[_activeToolMode] != null)
            //    {
            //       var toolEditor = Editor.CreateEditor(_tilemapTools[_activeToolMode]);
            //
            //       root.Add(toolEditor.CreateInspectorGUI());
            //       
            //       toolEditor.OnInspectorGUI();
            //    }
            // };

            BuildToolDictionary();
            FindGroupParents();

            var toolSettings = root.Q<VisualElement>("tool-settings-editor");
            var activeToolElement = root.Q<EnumField>("active-toolmode");

            //whenever the active tool is changed,
            //we need to change the child element of the tool settings area
            activeToolElement.RegisterCallback<ChangeEvent<Enum>>(evt =>
            {
                if (evt.newValue == null) return;

                //if there is already a child, remove it
                if (toolSettings.childCount > 0)
                {
                    toolSettings.RemoveAt(0);
                }

                // try and create a UIElements UI first,
                // this will work in the case the tool has a custom editor that returns a valid
                // visual element from the CreateInspectorGUI() call
                ToolMode newMode = (ToolMode) evt.newValue;
                var ive = _tmEditors[newMode].CreateInspectorGUI();
                if (ive != null)
                {
                    toolSettings.Add(ive);
                }
                else
                {
                    var child = new IMGUIContainer
                    {
                        onGUIHandler = () => _tmEditors[newMode].OnInspectorGUI()
                    };
                    toolSettings.Add(child);
                }

                windowSo.ApplyModifiedProperties();
                windowSo.Update();
                Repaint();
            });


            var resortButton = root.Q<Button>("resort-objects-button");
            resortButton.clicked += ResortAll;

            //
            ListView newListView = root.Q<ListView>("object-list");
            newListView.makeItem = () => prefabVisualElement.CloneTree();
            newListView.bindItem = BindListViewItem;
            newListView.onSelectionChanged += list => selectedTilePrefab = list.First() as GameObject;

            BuildPrefabGroupsToolbarButtons(newListView, root);

            _previewArea = root.Q<VisualElement>("selection-preview");

            root.Add(newListView);
            root.Bind(windowSo);

            SceneView.duringSceneGui += OnSceneGui;
        }

        private void Click(ContextClickEvent evt)
        {
            throw new NotImplementedException();
        }

        private void BuildToolDictionary()
        {
            //Debug.Log("builtTools");
            _tilemapTools = new Dictionary<ToolMode, TilemapEditorTool>();

            _tilemapTools.Add(ToolMode.Select, CreateInstance<TilemapTool_Select>());
            _tilemapTools.Add(ToolMode.Draw, CreateInstance<TilemapTool_Draw>());
            _tilemapTools.Add(ToolMode.Rect, CreateInstance<TilemapTool_Rect>());

            foreach (var tool in _tilemapTools)
            {
                tool.Value.editorSettings = editorSettings;
                tool.Value.owningEditor = this;
                _tmEditors.Add(tool.Key, Editor.CreateEditor(tool.Value));
            }
        }

        private void OnGUI()
        {
            GUIStyle bgColor = new GUIStyle {normal = {background = EditorGUIUtility.whiteTexture}};

            if (selectedTilePrefab != null)
            {
                if (_selEditor == null || _selEditor.target != selectedTilePrefab)
                    _selEditor = Editor.CreateEditor(selectedTilePrefab);
                _selEditor.OnInteractivePreviewGUI(_previewArea.layout, bgColor);
            }
        }

        private void OnSceneGui(SceneView sceneView)
        {
            if (editorSettings == null) return;
            if (!mouseOverWindow) return;

            //return;
            var curEvent = Event.current;

            //disabling input when holding the right mouse button
            //as to not block right click navigation
            if (curEvent.modifiers.HasFlag(EventModifiers.Alt))
            {
                _inputDisabled = true;
            }

            if (curEvent.isKey
                && curEvent.type == EventType.KeyUp
                && (curEvent.keyCode == KeyCode.LeftAlt || curEvent.keyCode == KeyCode.RightAlt))
            {
                _inputDisabled = false;
            }

            if (curEvent.isMouse && curEvent.type == EventType.MouseDown && curEvent.button >= 1)
            {
                _inputDisabled = true;
            }

            if (curEvent.isMouse && curEvent.type == EventType.MouseUp && curEvent.button >= 1)
            {
                _inputDisabled = false;
            }

            if (curEvent.isScrollWheel)
            {
                if (curEvent.modifiers == EventModifiers.Shift)
                {
                    Debug.Log(curEvent.delta.y);
                    _drawHeight -= curEvent.delta.y / Mathf.Abs(curEvent.delta.y);

                    curEvent.Use();
                }
            }

            FireToolEvents(sceneView);
        }

        private Plane GetCurrentWorkPlane()
        {
            return new Plane(Vector3.up, -_drawHeight * editorSettings.cellSize.y);
        }

        //!
        private void FireToolEvents(SceneView sceneView)
        {
            var tool = _tilemapTools[_activeToolMode];

            var workPlane = GetCurrentWorkPlane();

            if (GetPlaneHit(workPlane, out Vector3 hitPoint))
            {
                Bounds targetCellBounds =
                    TMEditorUtils.GetCellBounds(hitPoint, editorSettings.cellSize, out int3 targetCell);

                var cellData = GetCellData(hitPoint);

                //draw the handles for the tool
                tool.OnDrawHandles(cellData, sceneView.camera, out Color toolBoundsColor);

                //draw the cell bounds with the color returned by the active tool
                Handles.color = toolBoundsColor;
                Handles.DrawWireCube(targetCellBounds.center, targetCellBounds.size);

                var floorPos = hitPoint;
                floorPos.y = 0;

                Handles.color = _drawHeight > 0 ? new Color(0f, 1f, 0f, 0.5f) : new Color(1f, 0f, 0f, 0.5f);
                Handles.DrawLine(hitPoint, floorPos);

                if (_inputDisabled) return;

                if (Event.current.isKey)
                {
                    KeyCode eventKey = Event.current.keyCode;
                    if (Event.current.type == EventType.KeyDown)
                    {
                        //only fire if the key is not already held down
                        if (!downKeys.Contains(eventKey))
                        {
                            downKeys.Add(eventKey);
                            tool.OnKeyDown(cellData, eventKey);
                        }
                    }

                    if (Event.current.type == EventType.KeyUp)
                    {
                        tool.OnKeyUp(cellData, eventKey);
                        downKeys.Remove(eventKey);
                    }
                }

                if (Event.current.isMouse)
                {
                    if (Event.current.type == EventType.MouseDown)
                        tool.OnMouseDown(Event.current.button, cellData);
                    else if (Event.current.type == EventType.MouseUp)
                        tool.OnMouseUp(Event.current.button, cellData);
                    else if (Event.current.type == EventType.MouseDrag)
                        tool.OnMouseDrag(Event.current.button, cellData);
                    else if (Event.current.type == EventType.MouseMove)
                        tool.OnMouseMove(cellData);
                    _lastCell = cellData.cell;
                }
            }
        }

        private CellData GetCellData(Vector3 hitPoint)
        {
            CellData data = new CellData();
            data.cell = TMEditorUtils.WorldToCell(hitPoint, editorSettings.cellSize);
            data.worldPosition = hitPoint;
            data.activeTilePrefab = selectedTilePrefab;
            data.rotation = activeRotation;
            data.lastCell = _lastCell;
            data.overlapTiles = GetOverlappedTiles(hitPoint);

            if (data.overlapTiles.Length > 0)
                data.tile = data.overlapTiles[0];
            return data;
        }

        private void DrawHandles(Vector3 hitPoint, Bounds targetCellBounds)
        {
            //hit point rectangle
            Handles.color = new Color(1f, 0f, 0f, 0.5f);
            Handles.DrawWireCube(hitPoint, Vector3.one * .1f);

            Handles.Label(hitPoint, TMEditorUtils.WorldToCell(hitPoint, editorSettings.cellSize).ToString());
            //
            // var yZeroPoint = hitPoint;
            // yZeroPoint.y = 0;
            //
            // float3 min = targetCellBounds.min;
            // min.y = 0;
            // float3 max = targetCellBounds.max;
            // max.y = 0;
            // var points = new Vector3[]
            // {
            //    min,
            //    new float3(min.x, 0, max.z),
            //    max,
            //    new float3(max.x, 0, min.z)
            // };
            //
            // Handles.zTest = CompareFunction.LessEqual;
            // Handles.color = new Color(1f, 1f, 1f, 0.25f);
            // Handles.DrawSolidRectangleWithOutline(points, new Color(0f, 0f, 0f, 1f), Color.red);
            //
            // Handles.zTest = CompareFunction.Always;
            // var delta = hitPoint - yZeroPoint;
            // int c = Mathf.FloorToInt(Mathf.Abs(delta.y / tmEditorSettings.cellSize.y)) + 1;
            // //height tick marks
            // for (int i = 0; i < c; i++)
            // {
            //    Handles.DrawWireCube(yZeroPoint + delta.normalized * (i * tmEditorSettings.cellSize.y),
            //       Vector3.one * .1f);
            // }
            //
            // //vertical line from hit point to zero plane
            // Handles.DrawLine(hitPoint, yZeroPoint);
            // Handles.Label(hitPoint + Vector3.up * .2f, $"{_drawHeight:f3}");
            // Handles.DrawLine(Vector3.zero, Vector3.up);
            //
            // //draw the targeted cell bounds
            // Handles.color = new Color(1f, 0f, 0f, 0.18f);
            // //Handles.DrawWireCube(targetCellBounds.center, targetCellBounds.size);
        }

        private void RenderSelectedPrefabPreview(SceneView sceneView, int3 targetCell)
        {
            if (!selectedTilePrefab)
                return;

            var tileBounds = selectedTilePrefab.GetComponent<TileBounds>();
            var meshRenderers = selectedTilePrefab.GetComponentsInChildren<MeshRenderer>();
            var meshFilters = selectedTilePrefab.GetComponentsInChildren<MeshFilter>();

            if (meshRenderers.Length > 0 && meshFilters.Length == meshRenderers.Length)
            {
                for (var index = 0; index < meshRenderers.Length; index++)
                {
                    var mr = meshRenderers[index];
                    var mf = meshFilters[index];

                    for (var i = 0; i < mr.sharedMaterials.Length; i++)
                    {
                        var mrMat = mr.sharedMaterials[i];

                        var cornerToWorld = Matrix4x4.identity;
                        if (tileBounds != null)
                            cornerToWorld = tileBounds.BoxPointToWorldMatrix(BoundsCorner.BottomNearLeft)
                                .inverse;

                        _materialPropertyBlock = new MaterialPropertyBlock();
                        var baseTexture = mrMat.GetTexture(BaseMapId);
                        if (baseTexture) _materialPropertyBlock.SetTexture(BaseMapId, baseTexture);

                        // var centerOffsetTransform = selectedTileBounds.CornerLocalToWorld(BoundsCorner.Center);
                        // var drawMtx = Matrix4x4.Translate(targetCellBounds.min)
                        //                       * cornerToWorld
                        //                       * centerOffsetTransform
                        //                       * Matrix4x4.Rotate(quaternion.Euler(0, activeRotation * Mathf.Deg2Rad, 0))
                        //                       * centerOffsetTransform.inverse;


                        var drawMtx =
                            tileBounds.BoxPointToWorldMatrix(BoundsCorner.BottomNearLeft).inverse *
                            Matrix4x4.Translate(targetCell * (float3) editorSettings.cellSize) *
                            tileBounds.BoxPointToWorldMatrix(BoundsCorner.Center) *
                            Matrix4x4.Rotate(Quaternion.Euler(0, activeRotation, 0)) *
                            tileBounds.BoxPointToWorldMatrix(BoundsCorner.Center).inverse *
                            (mr.transform.localToWorldMatrix * tileBounds.transform.worldToLocalMatrix);


                        Graphics.DrawMesh(
                            mf.sharedMesh,
                            drawMtx,
                            editorSettings.previewobjectMaterial,
                            0, sceneView.camera, i,
                            _materialPropertyBlock);
                    }
                }
            }
        }

        private GameObject GetObjectPrefab(GameObject obj)
        {
            if (obj == null) return null;
            var path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(obj.gameObject);
            return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        private static TileBounds[] GetOverlappedTiles(Vector3 hitPoint)
        {
            var sphereOverlap = Physics.OverlapSphere(hitPoint, 3f);
            var overlapList = new List<TileBounds>();
            var oOps = new List<GameObject>();

            for (var i = 0; i < sphereOverlap.Length; i++)
            {
                var oObj = sphereOverlap[i];
                var tileBounds = oObj.GetComponent<TileBounds>();

                if (!tileBounds)
                {
                    var parent = oObj.transform.parent;
                    if (!parent) continue;

                    tileBounds = parent.GetComponent<TileBounds>();
                    if (!tileBounds) continue;
                }

                var bounds = tileBounds.Bounds;
                if (bounds.Contains(hitPoint) && !oOps.Contains(tileBounds.gameObject))
                {
                    overlapList.Add(tileBounds);
                    oOps.Add(tileBounds.gameObject);
                }
            }

            return overlapList.ToArray();
        }

        public void SortTileObject(GameObject tileObject)
        {
            var theTileBounds = tileObject.GetComponent<TileBounds>();
            if (!theTileBounds)
                return;

            //for each group of
            foreach (KeyValuePair<string, TileGroup> g in editorSettings.groupData)
            {
                //for ever tile in that group
                foreach (var tile in g.Value.tiles)
                {
                    //if it is the same object, move it into the parent group

                    var sceneSource = PrefabUtility.GetCorrespondingObjectFromSource(tileObject.gameObject);

                    if (sceneSource.name == tile.name)
                    {
                        //Debug.Log("found Match for " + tile.name);

                        tileObject.transform.parent = _groupParents[g.Key];
                    }
                }
            }
        }

        private void ResortAll()
        {
            var tileObjects = GameObject.FindObjectsOfType<TileBounds>().ToList();

            //for each group of
            foreach (KeyValuePair<string, TileGroup> g in editorSettings.groupData)
            {
                //for ever tile in that group
                foreach (var tile in g.Value.tiles)
                {
                    //for every tile object in the scene
                    foreach (var sceneTile in tileObjects)
                    {
                        //if it is the same object, move it into the parent group

                        var sceneSource = PrefabUtility.GetCorrespondingObjectFromSource(sceneTile.gameObject);

                        if (sceneSource.name == tile.name)
                        {
                            //Debug.Log("found Match for " + tile.name);

                            sceneTile.transform.parent = _groupParents[g.Key];
                        }
                    }
                }
            }
        }

        private void FindGroupParents()
        {
            var gData = editorSettings.groupData;

            Transform root = GetOrCreateGroupObject("Tile Groups");

            foreach (var groupKey in gData.Keys)
            {
                var groupObject = GetOrCreateGroupObject(groupKey);
                groupObject.parent = root;
                _groupParents.Add(groupKey, groupObject);
            }
        }

        private Transform GetOrCreateGroupObject(string objectName)
        {
            var go = GameObject.Find(objectName);
            if (go == null)
                go = new GameObject(objectName);

            return go.transform;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGui;

            editorSettings.lastActiveTool = _activeToolMode;
        }

        private void BuildPrefabGroupsToolbarButtons(ListView newListView, VisualElement root)
        {
            var PrefabGroupKey = editorSettings.groupData.Keys;
            //setting initial state for the listView to the first key in the groups
            if (PrefabGroupKey.Count > 0)
            {
                _activeKey = PrefabGroupKey.First();
                newListView.itemsSource = editorSettings.groupData[_activeKey].tiles;
                newListView.Refresh();
                if (newListView.itemsSource.Count > 0) newListView.selectedIndex = 0;
            }

            //for every key, add a button that changes the item source for the listView
            Toolbar prefabGroups = root.Q<Toolbar>("groups-toolbar");
            foreach (var groupKey in PrefabGroupKey)
            {
                prefabGroups.Add(new ToolbarButton(() =>
                {
                    _activeKey = groupKey;
                    newListView.itemsSource = editorSettings.groupData[_activeKey].tiles;
                    newListView.Refresh();
                }) {text = groupKey});
            }
        }

        private void BindListViewItem(VisualElement element, int i)
        {
            var tileGo = editorSettings.groupData[_activeKey].tiles[i];

            //looks for a tileboans component and ads one if it's missing
            var tileBounds = tileGo.GetComponent<TileBounds>();

            SerializedObject tileGoSo = new SerializedObject(tileBounds);

            //if there is no tilebounds, add one, save thje prefab, and use that for the serialized object
            if (!tileBounds)
            {
                tileBounds = tileGo.AddComponent<TileBounds>();
                PrefabUtility.SavePrefabAsset(tileGo);
                tileGoSo = new SerializedObject(tileBounds);
            }

            element.Bind(tileGoSo);
            var so = new SerializedObject(tileGo);
            element.Bind(so);

            var cornerPropField = element.Q<PropertyField>("corner-id-field");
            var cornerPopupField = cornerPropField.Q<PopupField<string>>("unity-input-cornerId");
            cornerPopupField.RegisterCallback<ChangeEvent<string>>(evt =>
            {
                var enumNames = Enum.GetNames(typeof(BoundsCorner));

                for (var index = 0; index < enumNames.Length; index++)
                {
                    var enumName = enumNames[index];
                    if (enumName == evt.newValue.Replace(" ", ""))
                    {
                        if (tileGoSo != null)
                        {
                            tileGoSo.FindProperty("cornerId").enumValueIndex = index;
                            tileGoSo.ApplyModifiedProperties();
                        }

                        PrefabUtility.SavePrefabAsset(tileGo);
                        break;
                    }
                }
            });

            Label imageLbl = element.Q<Label>("image");
            imageLbl.style.backgroundImage = AssetPreview.GetAssetPreview(tileGo);
        }

        private static bool GetPlaneHit(Plane p, out Vector3 hitPoint)
        {
            var mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            var didHit = p.Raycast(mouseRay, out float hitDistance);
            hitPoint = mouseRay.origin + mouseRay.direction * hitDistance;
            return didHit;
        }
    }
}