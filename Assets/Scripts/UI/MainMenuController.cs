using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 主選單控制器。
/// 按鈕在 Start() 中自動尋找並連接。
/// Settings 按鈕開啟場景內的 SettingsMenu（若不存在則動態建立）。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public GameObject mainPanel;
    public GameObject recordPanel;
    public TextMeshProUGUI recordText;

    SettingsMenu _settings;
    ScrollRect _recordScroll;
    RectTransform _recordContent;
    readonly List<GameObject> _recordRows = new();

    void Start()
    {
        ConfigureRecordScroll();

        // 找或建立 SettingsMenu
        _settings = FindFirstObjectByType<SettingsMenu>();

        // 連接主面板按鈕
        ConnectButton(mainPanel, "Start Game", () => StartGame());
        ConnectButton(mainPanel, "Records",    () => ShowRecords());
        ConnectButton(mainPanel, "Settings",   () => OpenSettings());
        ConnectButton(mainPanel, "Quit",       () => QuitGame());

        // 連接紀錄面板 Back 按鈕
        ConnectButton(recordPanel, "Back", () => HideRecords());

        if (recordPanel != null) recordPanel.SetActive(false);
    }

    void ConnectButton(GameObject parent, string btnName, UnityEngine.Events.UnityAction action)
    {
        if (parent == null || action == null) return;
        var tf = parent.transform.Find(btnName);
        if (tf == null) return;
        var btn = tf.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    public void StartGame()  => SceneManager.LoadScene(SceneNames.Battle);

    public void OpenSettings()
    {
        if (_settings == null) _settings = FindFirstObjectByType<SettingsMenu>();
        _settings?.Open();
    }

    public void ShowRecords()
    {
        if (mainPanel   != null) mainPanel.SetActive(false);
        if (recordPanel != null) recordPanel.SetActive(true);
        RefreshRecords();
        if (_recordScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            _recordScroll.verticalNormalizedPosition = 1f;
        }
    }

    public void HideRecords()
    {
        if (recordPanel != null) recordPanel.SetActive(false);
        if (mainPanel   != null) mainPanel.SetActive(true);
    }

    public void QuitGame() => Application.Quit();

    void RefreshRecords()
    {
        if (recordText == null || _recordContent == null) return;
        ClearRecordRows();

        var records = GameRecordStore.Load();

        if (records.Count == 0)
        {
            recordText.text = $"No records yet.\n\n<size=11><color=#888888>Records folder:\n{GameRecordStore.GetRecordsPath()}</color></size>";
            return;
        }

        recordText.text =
            $"<size=11><color=#888888>Records folder: {GameRecordStore.GetRecordsPath()}</color></size>";

        for (int i = 0; i < records.Count; i++)
            CreateRecordRow(records[i], i);
    }

    void CreateRecordRow(GameRecord record, int index)
    {
        var row = new GameObject(
            $"RecordRow_{index + 1}",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement));
        row.transform.SetParent(_recordContent, false);
        row.GetComponent<Image>().color = new Color(0.07f, 0.07f, 0.11f, 0.96f);

        var layoutElement = row.GetComponent<LayoutElement>();
        layoutElement.minHeight = CalculateRecordRowHeight(record);
        layoutElement.preferredHeight = layoutElement.minHeight;
        layoutElement.flexibleWidth = 1f;

        var detailsGo = new GameObject("Details", typeof(RectTransform), typeof(TextMeshProUGUI));
        detailsGo.transform.SetParent(row.transform, false);
        var detailsRt = detailsGo.GetComponent<RectTransform>();
        detailsRt.anchorMin = Vector2.zero;
        detailsRt.anchorMax = Vector2.one;
        detailsRt.offsetMin = new Vector2(12f, 8f);
        detailsRt.offsetMax = new Vector2(-108f, -8f);

        var details = detailsGo.GetComponent<TextMeshProUGUI>();
        details.font = recordText.font;
        details.fontSize = recordText.fontSize;
        details.color = recordText.color;
        details.alignment = TextAlignmentOptions.TopLeft;
        details.textWrappingMode = TextWrappingModes.Normal;
        details.overflowMode = TextOverflowModes.Overflow;
        details.raycastTarget = false;
        details.text = BuildRecordDetails(record, index);

        var deleteGo = new GameObject("Delete", typeof(RectTransform), typeof(Image), typeof(Button));
        deleteGo.transform.SetParent(row.transform, false);
        var deleteRt = deleteGo.GetComponent<RectTransform>();
        deleteRt.anchorMin = new Vector2(1f, 1f);
        deleteRt.anchorMax = new Vector2(1f, 1f);
        deleteRt.pivot = new Vector2(1f, 1f);
        deleteRt.anchoredPosition = new Vector2(-10f, -10f);
        deleteRt.sizeDelta = new Vector2(86f, 32f);

        var deleteImage = deleteGo.GetComponent<Image>();
        deleteImage.color = new Color(0.58f, 0.13f, 0.15f, 1f);
        var deleteButton = deleteGo.GetComponent<Button>();
        deleteButton.targetGraphic = deleteImage;
        deleteButton.onClick.AddListener(() => DeleteRecord(record));

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(deleteGo.transform, false);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        var label = labelGo.GetComponent<TextMeshProUGUI>();
        label.font = recordText.font;
        label.fontSize = Mathf.Max(13f, recordText.fontSize - 1f);
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = "Delete";

        _recordRows.Add(row);
    }

    static string BuildRecordDetails(GameRecord record, int index)
    {
        var sb = new System.Text.StringBuilder();
        int minutes = (int)(record.BattleTime / 60);
        int seconds = (int)(record.BattleTime % 60);
        string outcome = record.IsVictory
            ? "<color=#FFD700>Victory</color>"
            : "<color=#FF6666>Defeat</color>";
        int total = record.TotalDamage;
        string s1Pct = total > 0 ? $"{Mathf.RoundToInt(record.Skill1Dmg * 100f / total)}%" : "0%";
        string s2Pct = total > 0 ? $"{Mathf.RoundToInt(record.Skill2Dmg * 100f / total)}%" : "0%";
        string s3Pct = total > 0 ? $"{Mathf.RoundToInt(record.Skill3Dmg * 100f / total)}%" : "0%";

        sb.AppendLine($"#{index + 1}  [{record.DateStr}]  {outcome}");
        sb.AppendLine($"  Time: {minutes:00}:{seconds:00}   Total DMG: {total}   DPS: {record.Dps:F1}");
        sb.AppendLine($"  S1: {record.Skill1Uses}x  {record.Skill1Dmg} dmg ({s1Pct})");
        sb.AppendLine($"  S2: {record.Skill2Uses}x  {record.Skill2Dmg} dmg ({s2Pct})");
        sb.AppendLine(
            $"  S3: {record.Skill3Uses}x  {record.Skill3Dmg} dmg ({s3Pct})   S4: {record.Skill4Uses}x");
        AppendDamageTakenDetails(sb, record);
        return sb.ToString();
    }

    void DeleteRecord(GameRecord record)
    {
        float scrollPosition = _recordScroll != null
            ? _recordScroll.verticalNormalizedPosition
            : 1f;

        if (!GameRecordStore.Delete(record))
        {
            Debug.LogWarning("[MainMenuController] Could not delete the selected record.");
            return;
        }

        RefreshRecords();
        Canvas.ForceUpdateCanvases();
        if (_recordScroll != null)
            _recordScroll.verticalNormalizedPosition = scrollPosition;
    }

    void ClearRecordRows()
    {
        foreach (var row in _recordRows)
        {
            if (row == null) continue;
            row.SetActive(false);
            Destroy(row);
        }
        _recordRows.Clear();
    }

    static float CalculateRecordRowHeight(GameRecord record)
    {
        int hitCount = record.DamageTaken?.Count ?? 0;
        return Mathf.Max(132f, 128f + hitCount * 20f);
    }

    static void AppendDamageTakenDetails(System.Text.StringBuilder sb, GameRecord record)
    {
        var damageTaken = record.DamageTaken;
        if (damageTaken == null || damageTaken.Count == 0)
        {
            sb.AppendLine("  Damage Taken: No details (older record or no hits)");
            return;
        }

        int totalTaken = 0;
        foreach (var entry in damageTaken)
            totalTaken += entry.Amount;

        sb.AppendLine($"  Damage Taken: {totalTaken} total / {damageTaken.Count} hits");
        foreach (var entry in damageTaken)
        {
            sb.AppendLine(
                $"    <color=#AAAAAA>[{entry.FormatTimestamp()}]</color> " +
                $"{entry.Source}  <color=#FF7777>-{entry.Amount} HP</color>");
        }
    }

    void ConfigureRecordScroll()
    {
        if (recordPanel == null) return;

        var scrollTransform = recordPanel.transform.Find("RecordScroll");
        if (scrollTransform == null) return;

        var scrollRoot = scrollTransform.gameObject;
        _recordScroll = GetOrAdd<ScrollRect>(scrollRoot);
        _recordScroll.horizontal = false;
        _recordScroll.vertical = true;
        _recordScroll.movementType = ScrollRect.MovementType.Clamped;
        _recordScroll.inertia = true;
        _recordScroll.decelerationRate = 0.12f;
        _recordScroll.scrollSensitivity = 36f;

        var frame = GetOrAdd<Image>(scrollRoot);
        frame.color = new Color(0.48f, 0.5f, 0.58f, 0.9f);

        RectTransform content = _recordScroll.content;
        if (content == null)
            content = scrollTransform.Find("Content") as RectTransform;

        var viewportTransform = scrollTransform.Find("Viewport");
        GameObject viewportGo;
        if (viewportTransform == null)
        {
            viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollTransform, false);
        }
        else
        {
            viewportGo = viewportTransform.gameObject;
            GetOrAdd<Image>(viewportGo);
            GetOrAdd<RectMask2D>(viewportGo);
        }

        var viewportRt = viewportGo.GetComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = new Vector2(8f, 8f);
        viewportRt.offsetMax = new Vector2(-28f, -8f);
        viewportGo.GetComponent<Image>().color = new Color(0.025f, 0.025f, 0.045f, 0.98f);
        _recordScroll.viewport = viewportRt;

        if (content != null)
        {
            content.SetParent(viewportRt, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            var layout = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _recordScroll.content = content;
            _recordContent = content;
        }

        if (recordText != null)
        {
            recordText.textWrappingMode = TextWrappingModes.Normal;
            recordText.overflowMode = TextOverflowModes.Overflow;
            recordText.raycastTarget = false;
            recordText.rectTransform.anchorMin = new Vector2(0f, 1f);
            recordText.rectTransform.anchorMax = new Vector2(1f, 1f);
            recordText.rectTransform.pivot = new Vector2(0.5f, 1f);
            recordText.rectTransform.sizeDelta = Vector2.zero;
        }

        _recordScroll.verticalScrollbar = EnsureRecordScrollbar(scrollTransform);
        _recordScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        _recordScroll.verticalScrollbarSpacing = 4f;
    }

    static Scrollbar EnsureRecordScrollbar(Transform parent)
    {
        var existing = parent.Find("VerticalScrollbar");
        GameObject scrollbarGo;
        if (existing == null)
        {
            scrollbarGo = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            scrollbarGo.transform.SetParent(parent, false);
        }
        else
        {
            scrollbarGo = existing.gameObject;
            GetOrAdd<Image>(scrollbarGo);
            GetOrAdd<Scrollbar>(scrollbarGo);
        }

        var rt = scrollbarGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(-6f, 0f);
        rt.sizeDelta = new Vector2(14f, -16f);
        scrollbarGo.GetComponent<Image>().color = new Color(0.12f, 0.12f, 0.16f, 0.95f);

        var sliding = scrollbarGo.transform.Find("Sliding Area");
        if (sliding == null)
        {
            var slidingGo = new GameObject("Sliding Area", typeof(RectTransform));
            slidingGo.transform.SetParent(scrollbarGo.transform, false);
            sliding = slidingGo.transform;
        }
        var slidingRt = sliding.GetComponent<RectTransform>();
        slidingRt.anchorMin = Vector2.zero;
        slidingRt.anchorMax = Vector2.one;
        slidingRt.offsetMin = new Vector2(2f, 2f);
        slidingRt.offsetMax = new Vector2(-2f, -2f);

        var handle = sliding.Find("Handle");
        if (handle == null)
        {
            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(sliding, false);
            handle = handleGo.transform;
        }
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.anchorMin = Vector2.zero;
        handleRt.anchorMax = Vector2.one;
        handleRt.offsetMin = Vector2.zero;
        handleRt.offsetMax = Vector2.zero;
        var handleImage = GetOrAdd<Image>(handle.gameObject);
        handleImage.color = new Color(0.72f, 0.74f, 0.82f, 0.9f);

        var scrollbar = scrollbarGo.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRt;
        scrollbar.targetGraphic = handleImage;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        return scrollbar;
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }
}
