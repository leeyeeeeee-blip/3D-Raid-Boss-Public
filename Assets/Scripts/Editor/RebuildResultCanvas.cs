using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class RebuildResultCanvas
{
    public static void Execute()
    {
        // 重建 ResultCanvas
        var old = GameObject.Find("ResultCanvas");
        if (old != null) Object.DestroyImmediate(old);

        MenuBuilder.BuildBattleMenus();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[RebuildResultCanvas] ResultCanvas rebuilt with SaveRecord button.");
    }
}
#endif
