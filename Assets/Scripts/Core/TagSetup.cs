using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class TagSetup
{
    [MenuItem("FFXIV/Setup Tags")]
    public static void SetupTags()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tagsProp = tagManager.FindProperty("tags");

        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Boundary")
            { found = true; break; }
        }

        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Boundary";
            tagManager.ApplyModifiedProperties();
            Debug.Log("[TagSetup] Tag 'Boundary' 已建立");
        }
        else
        {
            Debug.Log("[TagSetup] Tag 'Boundary' 已存在");
        }
    }
}
#endif
