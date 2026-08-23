using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 修復 MainMenu 場景：加入 Camera，並確認 Build Settings 場景路徑正確。
/// </summary>
public class FixMainMenuScene
{
    [MenuItem("FFXIV/Fix MainMenu Scene")]
    public static void Fix()
    {
        // ── 1. 確認目前在 MainMenu 場景 ──────────────────
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.name.Contains("MainMenu"))
        {
            Debug.LogWarning("[FixMainMenuScene] 請先開啟 MainMenu 場景再執行此工具。");
            return;
        }

        // ── 2. 加入 Camera（若不存在）────────────────────
        var existingCam = Object.FindFirstObjectByType<Camera>();
        if (existingCam == null)
        {
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();
            Debug.Log("[FixMainMenuScene] Main Camera 已加入 MainMenu 場景。");
        }
        else
        {
            Debug.Log($"[FixMainMenuScene] Camera 已存在：{existingCam.name}");
        }

        // ── 3. 修正 Build Settings：使用根目錄的 SampleScene（有完整遊戲物件）──
        FixBuildSettings();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[FixMainMenuScene] 完成！");
    }

    static void FixBuildSettings()
    {
        // 正確的場景路徑
        string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        string battlePath   = "Assets/SampleScene.unity"; // 根目錄，有完整遊戲物件

        var newScenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(mainMenuPath, true),
            new EditorBuildSettingsScene(battlePath,   true),
        };
        EditorBuildSettings.scenes = newScenes;
        Debug.Log($"[FixMainMenuScene] Build Settings 已更新：\n  [0] {mainMenuPath}\n  [1] {battlePath}");
    }
}
#endif
