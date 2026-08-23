using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 一鍵完整建立戰鬥場景。換電腦後執行這一個即可。
/// </summary>
public class FullSetup
{
    [MenuItem("FFXIV/★ Full Setup (Run This First)")]
    public static void RunFullSetup()
    {
        TagSetup.SetupTags();
        InputSystemFix.FixInputSystem();
        SceneBuilder.BuildScene();
        AddComponents.AddSkillSystem();
        HudBuilder.BuildHud();
        MenuBuilder.BuildBattleMenus();
        DebugMenuBuilder.BuildDebugMenu();
        EventSystemFix.FixEventSystem();

        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[FullSetup] ✓ 完整場景建立完成！可以按 Play 了。");
    }
}
#endif
