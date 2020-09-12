using UnityEditor;
using UnityEngine.UIElements;

public class BrushEditor : EditorWindow
{
    [MenuItem("Tools/Brush Editor")]
    private static void ShowWindow()
    {
        GetWindow<BrushEditor>();
    }


    private void OnEnable()
    {
        var root = rootVisualElement;

        ListView lv = new ListView(Selection.gameObjects, 30,
            () => new Label(),
            (element, i) => { ((Label) element).text = Selection.gameObjects[i].name; }){style = {flexGrow = 1}};
        
        root.Add(new Button(() =>
        {
            lv.itemsSource = Selection.gameObjects;
            lv.Refresh();
            
        }) {text = "Capture Selection"});

        root.Add(lv);
    }
}
