using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 在 MainMenu 場景加入 SettingsCanvas（與戰鬥場景共用相同結構）。
/// </summary>
public class AddSettingsToMainMenu
{
    [MenuItem("FFXIV/Add Settings To MainMenu")]
    public static void AddSettings()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.name.Contains("MainMenu"))
        {
            Debug.LogWarning("[AddSettingsToMainMenu] 請先開啟 MainMenu 場景。");
            return;
        }

        // 移除舊的
        var old = GameObject.Find("SettingsCanvas");
        if (old != null) Object.DestroyImmediate(old);

        // 建立 SettingsCanvas
        var canvasGo = new GameObject("SettingsCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var sm = canvasGo.AddComponent<SettingsMenu>();

        // 面板
        var panel = new GameObject("SettingsPanel");
        panel.transform.SetParent(canvasGo.transform, false);
        var panelRt = panel.AddComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = panelRt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.9f);
        sm.panel = panel;

        // 標題
        var titleT = CreateText(panel.transform, "Title", "SETTINGS", 40, TextAlignmentOptions.Center);
        SetAnchored(titleT.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(400, 60));

        // Music Slider
        CreateText(panel.transform, "MusicLabel", "Music Volume", 20, TextAlignmentOptions.Left)
            .rectTransform.anchoredPosition = new Vector2(-200, 100);
        sm.musicSlider = CreateSliderUI(panel.transform, "MusicSlider", new Vector2(50, 100));

        // SFX Slider
        CreateText(panel.transform, "SfxLabel", "SFX Volume", 20, TextAlignmentOptions.Left)
            .rectTransform.anchoredPosition = new Vector2(-200, 30);
        sm.sfxSlider = CreateSliderUI(panel.transform, "SfxSlider", new Vector2(50, 30));

        // Key Bindings
        var bindTitle = CreateText(panel.transform, "BindLabel", "Key Bindings", 22, TextAlignmentOptions.Center);
        SetAnchored(bindTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(400, 35));

        sm.skill1KeyText = BuildKeyBind(panel.transform, "Skill 1",          new Vector2(0, -100), 1);
        sm.skill2KeyText = BuildKeyBind(panel.transform, "Skill 2",          new Vector2(0, -150), 2);
        sm.skill4KeyText = BuildKeyBind(panel.transform, "Skill 4 (Burst)",  new Vector2(0, -200), 4);

        // Close 按鈕
        var closeBtn = CreateButton(panel.transform, "Close", new Vector2(0, -280));

        panel.SetActive(false);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[AddSettingsToMainMenu] SettingsCanvas 已加入 MainMenu 場景。");
    }

    static TextMeshProUGUI BuildKeyBind(Transform parent, string label, Vector2 pos, int skillIndex)
    {
        var container = new GameObject($"Bind_{label}");
        container.transform.SetParent(parent, false);
        var rt = container.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(400, 40);

        var labelTxt = CreateText(container.transform, "Label", label, 18, TextAlignmentOptions.Left);
        SetAnchored(labelTxt.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0), new Vector2(150, 35));

        var keyTxt = CreateText(container.transform, "Key", "1", 18, TextAlignmentOptions.Center);
        SetAnchored(keyTxt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(30, 0), new Vector2(100, 35));
        keyTxt.color = new Color(1f, 0.9f, 0.3f);

        var btn = CreateButton(container.transform, "Rebind", new Vector2(160, 0));
        btn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 14;
        btn.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 35);
        return keyTxt;
    }

    static Button CreateButton(Transform parent, string label, Vector2 pos)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(280, 55);
        go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.25f, 0.95f);
        var btn = go.AddComponent<Button>();
        var txt = CreateText(go.transform, "Label", label, 20, TextAlignmentOptions.Center);
        txt.rectTransform.anchorMin = Vector2.zero;
        txt.rectTransform.anchorMax = Vector2.one;
        txt.rectTransform.sizeDelta = Vector2.zero;
        return btn;
    }

    static Slider CreateSliderUI(Transform parent, string name, Vector2 pos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(200, 20);
        var slider = go.AddComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 1; slider.value = 1;

        var bg = new GameObject("BG"); bg.transform.SetParent(go.transform, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f);

        var fillArea = new GameObject("FillArea"); fillArea.transform.SetParent(go.transform, false);
        var faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one; faRt.sizeDelta = Vector2.zero;

        var fill = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(1, 1); fillRt.sizeDelta = Vector2.zero;
        fill.AddComponent<Image>().color = new Color(0.3f, 0.6f, 1f);
        slider.fillRect = fillRt;

        var handle = new GameObject("Handle"); handle.transform.SetParent(go.transform, false);
        var handleRt = handle.AddComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(20, 20);
        handle.AddComponent<Image>().color = Color.white;
        slider.handleRect = handleRt;
        return slider;
    }

    static TextMeshProUGUI CreateText(Transform parent, string name, string text, int fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize;
        tmp.alignment = align; tmp.color = Color.white;
        return tmp;
    }

    static void SetAnchored(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = anchor; rt.anchoredPosition = pos; rt.sizeDelta = size;
    }
}
#endif
