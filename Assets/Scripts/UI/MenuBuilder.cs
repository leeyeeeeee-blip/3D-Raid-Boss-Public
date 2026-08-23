using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class MenuBuilder
{
    // ── 戰鬥場景選單 ──────────────────────────────────────
    [MenuItem("FFXIV/Build Battle Menus")]
    public static void BuildBattleMenus()
    {
        foreach (var n in new[] { "PauseMenuCanvas", "ResultCanvas", "SettingsCanvas" })
        {
            var old = GameObject.Find(n);
            if (old != null) Object.DestroyImmediate(old);
        }

        BuildPauseMenu();
        BuildResultScreen();
        BuildSettingsMenu();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[MenuBuilder] Battle menus built.");
    }

    static void BuildPauseMenu()
    {
        var canvas = CreateOverlayCanvas("PauseMenuCanvas", 20);
        var pm = canvas.gameObject.AddComponent<PauseMenu>();

        var panel = CreateFullPanel(canvas.transform, "PausePanel", new Color(0, 0, 0, 0.92f));
        pm.panel = panel;

        var title = CreateText(panel.transform, "Title", "PAUSED", 48, TextAlignmentOptions.Center);
        SetAnchored(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -120), new Vector2(400, 60));

        pm.timerText = CreateText(panel.transform, "TimerText", "00:00", 24, TextAlignmentOptions.Center);
        SetAnchored(pm.timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(300, 40));
        pm.timerText.color = new Color(0.8f, 0.8f, 0.8f);

        float btnY = -280;
        var resumeBtn  = CreateButton(panel.transform, "Resume",       new Vector2(0, btnY));
        var restartBtn = CreateButton(panel.transform, "Restart",      new Vector2(0, btnY - 70));
        var settingBtn = CreateButton(panel.transform, "Settings",     new Vector2(0, btnY - 140));
        var menuBtn    = CreateButton(panel.transform, "Main Menu",    new Vector2(0, btnY - 210));

        resumeBtn.onClick.AddListener(pm.Resume);
        restartBtn.onClick.AddListener(pm.Restart);
        settingBtn.onClick.AddListener(() => pm.Settings());
        menuBtn.onClick.AddListener(pm.MainMenu);

        panel.SetActive(false);
    }

    static void BuildResultScreen()
    {
        var canvas = CreateOverlayCanvas("ResultCanvas", 25);
        var rs = canvas.gameObject.AddComponent<ResultScreen>();

        var panel = CreateFullPanel(canvas.transform, "ResultPanel", new Color(0, 0, 0, 0.88f));
        rs.panel = panel;

        // 標題
        rs.titleText = CreateText(panel.transform, "Title", "RESULT", 52, TextAlignmentOptions.Center);
        SetAnchored(rs.titleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(500, 70));

        // 統計文字
        rs.statsText = CreateText(panel.transform, "Stats", "", 20, TextAlignmentOptions.Center);
        SetAnchored(rs.statsText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(500, 280));
        rs.statsText.color = new Color(0.9f, 0.9f, 0.9f);

        // 儲存紀錄按鈕（中央偏下）
        var saveBtn = CreateButton(panel.transform, "SaveRecord", new Vector2(0, -200));
        saveBtn.GetComponent<UnityEngine.UI.Image>().color = new Color(0.15f, 0.45f, 0.15f, 0.95f);
        var saveBtnColors = saveBtn.colors;
        saveBtnColors.highlightedColor = new Color(0.2f, 0.6f, 0.2f);
        saveBtn.colors = saveBtnColors;
        var saveLbl = saveBtn.GetComponentInChildren<TextMeshProUGUI>();
        if (saveLbl != null) saveLbl.text = "Save Record";
        rs.saveBtn = saveBtn;

        // 儲存狀態文字
        rs.saveStatusText = CreateText(panel.transform, "SaveStatus", "", 13, TextAlignmentOptions.Center);
        SetAnchored(rs.saveStatusText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -255), new Vector2(500, 40));
        rs.saveStatusText.color = new Color(0.6f, 1f, 0.6f);

        // 底部按鈕
        var restartBtn = CreateButton(panel.transform, "Restart",   new Vector2(-120, -310));
        var menuBtn    = CreateButton(panel.transform, "Main Menu", new Vector2(120,  -310));
        rs.restartBtn  = restartBtn;
        rs.mainMenuBtn = menuBtn;

        panel.SetActive(false);
    }

    static void BuildSettingsMenu()
    {
        var canvas = CreateOverlayCanvas("SettingsCanvas", 30);
        var sm = canvas.gameObject.AddComponent<SettingsMenu>();

        var panel = CreateFullPanel(canvas.transform, "SettingsPanel", new Color(0, 0, 0, 0.9f));
        sm.panel = panel;

        var titleT = CreateText(panel.transform, "Title", "SETTINGS", 40, TextAlignmentOptions.Center);
        SetAnchored(titleT.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(400, 60));

        CreateText(panel.transform, "MusicLabel", "Music Volume", 20, TextAlignmentOptions.Left)
            .rectTransform.anchoredPosition = new Vector2(-200, 100);
        sm.musicSlider = CreateSliderUI(panel.transform, "MusicSlider", new Vector2(50, 100));
        sm.musicSlider.onValueChanged.AddListener(sm.OnMusicChanged);

        CreateText(panel.transform, "SfxLabel", "SFX Volume", 20, TextAlignmentOptions.Left)
            .rectTransform.anchoredPosition = new Vector2(-200, 30);
        sm.sfxSlider = CreateSliderUI(panel.transform, "SfxSlider", new Vector2(50, 30));
        sm.sfxSlider.onValueChanged.AddListener(sm.OnSfxChanged);

        var bindTitle = CreateText(panel.transform, "BindLabel", "Key Bindings", 22, TextAlignmentOptions.Center);
        SetAnchored(bindTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(400, 35));

        sm.skill1KeyText = BuildKeyBind(panel.transform, "Skill 1", new Vector2(0, -100), () => sm.StartRebind(1));
        sm.skill2KeyText = BuildKeyBind(panel.transform, "Skill 2", new Vector2(0, -150), () => sm.StartRebind(2));
        sm.skill4KeyText = BuildKeyBind(panel.transform, "Skill 4 (Burst)", new Vector2(0, -200), () => sm.StartRebind(4));

        var closeBtn = CreateButton(panel.transform, "Close", new Vector2(0, -280));
        closeBtn.onClick.AddListener(sm.Close);

        panel.SetActive(false);
    }

    // ── 主選單場景（獨立建立，不混入戰鬥場景）────────────
    [MenuItem("FFXIV/Create MainMenu Scene")]
    public static void CreateMainMenuScene()
    {
        // 先儲存目前場景
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // 建立全新空場景
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // EventSystem
        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        var t = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (t != null) es.AddComponent(t);
        else es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Canvas
        var canvasGo = new GameObject("MainMenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        var mmc = canvasGo.AddComponent<MainMenuController>();

        // 背景
        var bg = new GameObject("Background");
        bg.transform.SetParent(canvasGo.transform, false);
        var bgRt = bg.AddComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f);

        // 主面板
        var mainPanel = new GameObject("MainPanel");
        mainPanel.transform.SetParent(canvasGo.transform, false);
        var mpRt = mainPanel.AddComponent<RectTransform>();
        mpRt.anchorMin = mpRt.anchorMax = new Vector2(0.5f, 0.5f);
        mpRt.sizeDelta = new Vector2(400, 500);
        mmc.mainPanel = mainPanel;

        // 標題
        var titleText = CreateText(mainPanel.transform, "Title", "FFXIV Boss Fight Demo", 32, TextAlignmentOptions.Center);
        SetAnchored(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(500, 60));
        titleText.color = new Color(1f, 0.85f, 0.3f);

        // 按鈕
        string[] btnLabels = { "Start Game", "Records", "Settings", "Quit" };
        for (int i = 0; i < btnLabels.Length; i++)
        {
            var btn = CreateButton(mainPanel.transform, btnLabels[i], new Vector2(0, 80 - i * 70));
            int idx = i;
            switch (idx)
            {
                case 0: btn.onClick.AddListener(mmc.StartGame); break;
                case 1: btn.onClick.AddListener(mmc.ShowRecords); break;
                case 3: btn.onClick.AddListener(mmc.QuitGame); break;
            }
        }

        // 紀錄面板
        var recordPanel = new GameObject("RecordPanel");
        recordPanel.transform.SetParent(canvasGo.transform, false);
        var rpRt = recordPanel.AddComponent<RectTransform>();
        rpRt.anchorMin = rpRt.anchorMax = new Vector2(0.5f, 0.5f);
        rpRt.sizeDelta = new Vector2(700, 600);
        recordPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.85f);
        mmc.recordPanel = recordPanel;

        var recTitle = CreateText(recordPanel.transform, "RecTitle", "RECORDS", 28, TextAlignmentOptions.Center);
        SetAnchored(recTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(600, 40));

        var scrollGo = new GameObject("RecordScroll");
        scrollGo.transform.SetParent(recordPanel.transform, false);
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.05f, 0.15f);
        scrollRt.anchorMax = new Vector2(0.95f, 0.9f);
        scrollRt.offsetMin = scrollRt.offsetMax = Vector2.zero;
        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(scrollGo.transform, false);
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1f); contentRt.sizeDelta = new Vector2(0, 0);
        contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        mmc.recordText = CreateText(contentGo.transform, "RecordText", "", 15, TextAlignmentOptions.TopLeft);
        mmc.recordText.rectTransform.anchorMin = Vector2.zero;
        mmc.recordText.rectTransform.anchorMax = Vector2.one;
        mmc.recordText.rectTransform.sizeDelta = Vector2.zero;
        scroll.content = contentRt;

        var backBtn = CreateButton(recordPanel.transform, "Back", new Vector2(0, -260));
        backBtn.onClick.AddListener(mmc.HideRecords);
        recordPanel.SetActive(false);

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainMenu.unity");
        Debug.Log("[MenuBuilder] MainMenu scene created: Assets/Scenes/MainMenu.unity");

        AddSceneToBuild("Assets/Scenes/MainMenu.unity");
        AddSceneToBuild("Assets/Scenes/SampleScene.unity");
    }

    static void AddSceneToBuild(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == scenePath) return;
        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(newScenes, 0);
        newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
    }

    // ── 輔助方法 ──────────────────────────────────────────

    static Canvas CreateOverlayCanvas(string name, int sortOrder)
    {
        var go = new GameObject(name);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = sortOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        go.AddComponent<GraphicRaycaster>();
        return c;
    }

    static GameObject CreateFullPanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.AddComponent<Image>().color = color;
        return go;
    }

    public static Button CreateButton(Transform parent, string label, Vector2 pos)
    {
        var go = new GameObject(label);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(280, 55);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.15f, 0.25f, 0.95f);
        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.2f);
        btn.colors = colors;
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

    static TextMeshProUGUI BuildKeyBind(Transform parent, string label, Vector2 pos, System.Action onRebind)
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
        btn.onClick.AddListener(() => onRebind());
        return keyTxt;
    }

    public static TextMeshProUGUI CreateText(Transform parent, string name, string text,
        int fontSize, TextAlignmentOptions align)
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
