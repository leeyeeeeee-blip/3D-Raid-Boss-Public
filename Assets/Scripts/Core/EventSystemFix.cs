using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 將 EventSystem 的 StandaloneInputModule 換成 InputSystemUIInputModule（支援 New Input System）。
/// </summary>
public class EventSystemFix
{
    [MenuItem("FFXIV/Fix EventSystem Input Module")]
    public static void FixEventSystem()
    {
        var es = Object.FindFirstObjectByType<EventSystem>();
        if (es == null) { Debug.LogError("EventSystem not found"); return; }

        // 移除舊的 StandaloneInputModule
        var old = es.GetComponent<StandaloneInputModule>();
        if (old != null) Object.DestroyImmediate(old);

        // 加入支援 Both 的模組（Unity 6 用 InputSystemUIInputModule）
        var existing = es.GetComponent("InputSystemUIInputModule");
        if (existing == null)
        {
            // 用 AddComponent by type name（避免硬依賴）
            var t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (t != null)
            {
                es.gameObject.AddComponent(t);
                Debug.Log("[EventSystemFix] InputSystemUIInputModule 已加入");
            }
            else
            {
                // Fallback：保留 StandaloneInputModule（Both 模式下也能用）
                if (es.GetComponent<StandaloneInputModule>() == null)
                    es.gameObject.AddComponent<StandaloneInputModule>();
                Debug.Log("[EventSystemFix] 使用 StandaloneInputModule（Both 模式）");
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
#endif
