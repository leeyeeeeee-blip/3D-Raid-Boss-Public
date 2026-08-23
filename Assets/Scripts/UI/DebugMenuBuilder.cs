using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;

public class DebugMenuBuilder
{
    [MenuItem("FFXIV/Build Debug Menu")]
    public static void BuildDebugMenu()
    {
        var old = GameObject.Find("DebugMenuCanvas");
        if (old != null) Object.DestroyImmediate(old);

        var canvasGo = new GameObject("DebugMenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var dm = canvasGo.AddComponent<DebugMenu>();

        // 面板（右側）
        var panel = new GameObject("DebugPanel");
        panel.transform.SetParent(canvasGo.transform, false);
        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-10, 0);
        rt.sizeDelta = new Vector2(220, 380);
        var bg = panel.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.85f);
        dm.panel = panel;

        // 標題
        var title = CreateText(panel.transform, "Title", "DEBUG MENU (HOME)", 14, TextAlignmentOptions.Center);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -15), new Vector2(200, 25));
        title.color = new Color(1f, 0.8f, 0.2f);

        // 按鈕
        string[] labels = { "Player Die", "Boss Die (Victory)", "Reset Cooldowns", "Reset DPS Stats", "Restart" };
        System.Action[] actions = {
            dm.TriggerPlayerDeath,
            dm.TriggerVictory,
            dm.ResetCooldowns,
            dm.ResetDpsStats,
            dm.RestartBattle
        };

        for (int i = 0; i < labels.Length; i++)
        {
            var btn = CreateButton(panel.transform, labels[i], new Vector2(0, -60 - i * 55));
            int idx = i;
            btn.onClick.AddListener(() => actions[idx]());
        }

        panel.SetActive(false);

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[DebugMenuBuilder] 測試選單建立完成！");
    }

    static Button CreateButton(Transform parent, string label, Vector2 pos)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(190, 45);
        go.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.35f, 0.95f);
        var btn = go.AddComponent<Button>();
        var txt = CreateText(go.transform, "Label", label, 14, TextAlignmentOptions.Center);
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.sizeDelta = Vector2.zero;
        return btn;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, int size, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size; tmp.alignment = align; tmp.color = Color.white;
        return tmp;
    }

    static void SetAnchored(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
    }
}
#endif
