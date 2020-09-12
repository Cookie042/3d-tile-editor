using System;
using UnityEditor;

namespace TMEditorSimple
{
    [Serializable]
    public class SerializableStringAndGameObjectDict : SerializableDictionary<string, TileGroup>
    {
    }
    
    [Serializable]
    public class SerializableStringAndMaterialDict : SerializableDictionary<string, MaterialGroup>
    {
    }
    

    [CustomPropertyDrawer(typeof(SerializableStringAndGameObjectDict)), 
     CustomPropertyDrawer(typeof(SerializableStringAndMaterialDict))]
    public class AnySerializableDictionaryPropertyDrawer : SerializableDictionaryPropertyDrawer
    {
    }
}