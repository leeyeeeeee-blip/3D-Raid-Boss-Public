using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class AddComponents
{
    [MenuItem("FFXIV/Add SkillSystem to Player")]
    public static void AddSkillSystem()
    {
        var player = GameObject.Find("Player");
        if (player == null) { Debug.LogError("Player not found"); return; }

        if (player.GetComponent<SkillSystem>() == null)
            player.AddComponent<SkillSystem>();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[AddComponents] SkillSystem 已加到 Player");
    }
}
#endif
