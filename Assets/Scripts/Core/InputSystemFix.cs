using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 將 Active Input Handling 改為 Both，讓舊 Input API 可正常使用。
/// ponytail: 不重寫所有 Input 呼叫，改設定一行解決。
/// </summary>
public class InputSystemFix
{
    [MenuItem("FFXIV/Fix Input System (Both)")]
    public static void FixInputSystem()
    {
        var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        // activeInputHandler: 0=Input Manager(Old), 1=Input System(New), 2=Both
        var prop = settings.FindProperty("activeInputHandler");
        if (prop != null)
        {
            prop.intValue = 2;
            settings.ApplyModifiedProperties();
            Debug.Log("[InputSystemFix] Active Input Handling → Both");
        }
        else
        {
            Debug.LogError("[InputSystemFix] 找不到 activeInputHandler 屬性");
        }
    }
}
#endif
