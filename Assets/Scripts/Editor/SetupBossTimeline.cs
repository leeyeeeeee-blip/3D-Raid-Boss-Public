using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor 工具：在 SampleScene 中建立 BossTimeline GameObject。
/// </summary>
public class SetupBossTimeline
{
    [MenuItem("FFXIV/Setup Boss Timeline")]
    public static void Setup()
    {
        SetupWithParams(1f, 0f);
    }

    [MenuItem("FFXIV/Setup Boss Timeline (5x Speed)")]
    public static void SetupFast()
    {
        SetupWithParams(5f, 0f);
    }

    [MenuItem("FFXIV/Setup Boss Timeline (Start at 35s)")]
    public static void SetupAt35()
    {
        SetupWithParams(1f, 35f);
    }

    [MenuItem("FFXIV/Setup Boss Timeline (Start at 60s Phase2)")]
    public static void SetupAt60()
    {
        SetupWithParams(1f, 60f);
    }

    [MenuItem("FFXIV/Setup Boss Timeline (Start at 120s Phase3)")]
    public static void SetupAt120()
    {
        SetupWithParams(1f, 120f);
    }

    static void SetupWithParams(float speed, float startTime)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        var old = GameObject.Find("BossTimeline");
        if (old != null) Object.DestroyImmediate(old);

        var gridGo = GameObject.Find("BossArenaGrid");
        if (gridGo == null)
        {
            gridGo = new GameObject("BossArenaGrid");
            gridGo.AddComponent<BossArenaGrid>();
        }
        else if (gridGo.GetComponent<BossArenaGrid>() == null)
        {
            gridGo.AddComponent<BossArenaGrid>();
        }

        var go = new GameObject("BossTimeline");
        var timeline = go.AddComponent<BossTimelineController>();
        go.AddComponent<BossMechanicController>();

        timeline.timelineSpeed = speed;
        timeline.autoStart = true;
        timeline.debugStartTime = startTime;

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[SetupBossTimeline] BossTimeline 建立完成！speed={speed}x, startTime={startTime}s");
        Selection.activeGameObject = go;
    }
}
