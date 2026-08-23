using UnityEngine;
using UnityEditor;

/// <summary>
/// 在 Play Mode 中建立危險區並暫停 Editor，以便截圖。
/// </summary>
public class PauseAndShowHazards
{
    [MenuItem("FFXIV/Pause And Show Checkerboard")]
    public static void ShowCheckerboard()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogWarning("[PauseAndShowHazards] 請先進入 Play Mode！");
            return;
        }

        // 找到 BossMechanicController
        var mechanic = Object.FindFirstObjectByType<BossMechanicController>();
        if (mechanic == null)
        {
            Debug.LogError("[PauseAndShowHazards] 找不到 BossMechanicController！");
            return;
        }

        // 直接呼叫棋盤技能
        mechanic.DoCheckerboard(0, 999f, 25); // 超長預告時間，不會爆炸

        // 暫停 Editor
        EditorApplication.isPaused = true;

        // 調整 Scene View
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.in2DMode = false;
            sv.pivot = new Vector3(0f, 0f, 0f);
            sv.rotation = Quaternion.Euler(50f, 20f, 0f);
            sv.size = 20f;
            sv.Repaint();
        }

        Debug.Log("[PauseAndShowHazards] 棋盤已建立，Editor 已暫停。");
    }

    [MenuItem("FFXIV/Pause And Show Anchor Point")]
    public static void ShowAnchorPoint()
    {
        if (!EditorApplication.isPlaying) return;

        var mechanic = Object.FindFirstObjectByType<BossMechanicController>();
        if (mechanic == null) return;

        mechanic.DoAnchorPoint(1, 1, 999f, 40, 12f);
        EditorApplication.isPaused = true;

        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.in2DMode = false;
            sv.pivot = new Vector3(0f, 0f, 0f);
            sv.rotation = Quaternion.Euler(50f, 20f, 0f);
            sv.size = 20f;
            sv.Repaint();
        }

        Debug.Log("[PauseAndShowHazards] 錨點已建立，Editor 已暫停。");
    }

    [MenuItem("FFXIV/Resume Game")]
    public static void ResumeGame()
    {
        EditorApplication.isPaused = false;
        Debug.Log("[PauseAndShowHazards] 遊戲已繼續。");
    }
}
