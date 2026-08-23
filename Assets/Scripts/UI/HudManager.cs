using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// HUD 總管。
/// </summary>
public class HudManager : MonoBehaviour
{
    [Header("Boss 血條")]
    public Slider bossHpSlider;
    public TextMeshProUGUI bossHpText;

    [Header("玩家血條（跟隨玩家頭上）")]
    public RectTransform playerHpRoot;   // 整個玩家血條容器（Screen Space）
    public Slider playerHpSlider;
    public TextMeshProUGUI playerHpText; // 黑色數字

    [Header("技能欄")]
    public SkillSlotUI[] skillSlots;     // 0=技能1, 1=技能2, 2=技能3, 3=技能4

    [Header("技能1疊層圓圈（血條下方，橘紅，共5個）")]
    public Image[] skill1StackDots;      // 5個圓圈 Image，由 HudBuilder 建立

    [Header("技能3觸發儲存（技能1疊層下方，共3格）")]
    public Image[] skill3ProcChargeBars;

    [Header("技能1讀條與GCD（血條上方）")]
    public GameObject skill1CastBarRoot;
    public Image skill1CastFill;
    public Image gcdIndicator;

    [Header("技能2疊層（血條左側，黃色）")]
    public TextMeshProUGUI skill2StackText;
    public Image skill2StackBg;          // 技能2層數灰底

    [Header("右上角")]
    public TextMeshProUGUI timerText;
    public Button finishBtn;               // 結算按鈕（測試用）
    public TextMeshProUGUI dpsText;
    public TextMeshProUGUI skillStatsText; // 技能次數/占比列表

    [Header("左上角系統提示")]
    public ScrollRect systemLogScroll;
    public TextMeshProUGUI systemLogText;

    [Header("Boss 技能提示")]
    public TextMeshProUGUI bossSkillAlertText;
    public GameObject bossCastBarRoot;
    public Image bossCastFill;

    [Header("玩家受傷紀錄")]
    public GameObject damageTakenPanel;
    public TextMeshProUGUI damageTakenText;

    [Header("傷害跳字")]
    public GameObject damageNumberPrefab;
    public Canvas worldCanvas;

    // ── 內部參考 ──────────────────────────────────────────
    SkillSystem _skills;
    PlayerStats _playerStats;
    BattleTimer _timer;
    Transform _bossTransform;
    Transform _playerTransform;
    Camera _cam;

    int _bossFakeMaxHp = 10000;
    readonly List<string> _systemLogs = new();
    Coroutine _bossCastRoutine;

    float _refreshTimer;
    const float REFRESH_INTERVAL = 0.1f;
    bool _needsRefresh;

    // 技能1圓圈顏色
    static readonly Color DOT_FILLED  = new Color(1f, 0.35f, 0.1f, 1f);   // 橘紅（技能2同色）
    static readonly Color DOT_EMPTY   = new Color(0.25f, 0.25f, 0.25f, 0.7f); // 暗灰空圓
    static readonly Color PROC_CHARGE_FILLED = new Color(1f, 0.78f, 0.12f, 1f);
    static readonly Color PROC_CHARGE_EMPTY = new Color(0.22f, 0.18f, 0.1f, 0.75f);
    // 灰底顏色（50% 透明度）
    static readonly Color STACK_BG_COLOR = new Color(0.15f, 0.15f, 0.15f, 0.5f);

    void Start()
    {
        EnsureBossCastUi();
        EnsureDamageTakenUi();

        var player = GameObject.Find("Player");
        _skills       = player?.GetComponent<SkillSystem>();
        _playerStats  = player?.GetComponent<PlayerStats>();
        _playerTransform = player?.transform;

        _bossTransform = GameObject.Find("Boss")?.transform;
        _timer = FindFirstObjectByType<BattleTimer>();
        _cam   = Camera.main;

        if (_skills != null)
        {
            _skills.OnStateChanged += ScheduleRefresh;
            _skills.OnDamageDealt  += OnDamageDealt;
            _skills.OnSkill3Proc   += OnSkill3Proc;
        }
        if (_playerStats != null)
        {
            _playerStats.OnHpChanged += RefreshPlayerHp;
            _playerStats.OnDamageTaken += OnPlayerDamageTaken;
            RefreshDamageTakenLog();
        }

        if (bossHpSlider != null) bossHpSlider.maxValue = _bossFakeMaxHp;

        // 結算按鈕連接
        if (finishBtn != null)
        {
            finishBtn.onClick.RemoveAllListeners();
            finishBtn.onClick.AddListener(() =>
            {
                GameManager.Instance.SetState(GameManager.GameState.Victory);
            });
        }

        RefreshHud();
        AddSystemLog("Battle Start!");
        // Ensure cursor is visible on scene start
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnDestroy()
    {
        if (_skills != null)
        {
            _skills.OnStateChanged -= ScheduleRefresh;
            _skills.OnDamageDealt -= OnDamageDealt;
            _skills.OnSkill3Proc -= OnSkill3Proc;
        }
        if (_playerStats != null)
        {
            _playerStats.OnHpChanged -= RefreshPlayerHp;
            _playerStats.OnDamageTaken -= OnPlayerDamageTaken;
        }
    }

    void ScheduleRefresh() => _needsRefresh = true;

    void Update()
    {
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= REFRESH_INTERVAL)
        {
            _refreshTimer = 0f;
            RefreshTimer();
            RefreshDps();
            if (_needsRefresh)
            {
                _needsRefresh = false;
                RefreshSkillSlots();
                RefreshStackTexts();
                RefreshBossHp();
                RefreshPlayerHp();
            }
        }
        UpdatePlayerHpPosition();
        RefreshCastAndGcd();
    }

    // ── 玩家血條跟隨頭上 ──────────────────────────────────
    void UpdatePlayerHpPosition()
    {
        if (playerHpRoot == null || _playerTransform == null || _cam == null) return;
        Vector3 worldPos = _playerTransform.position + Vector3.up * 2.8f;
        Vector3 screenPos = _cam.WorldToScreenPoint(worldPos);
        if (screenPos.z < 0) { playerHpRoot.gameObject.SetActive(false); return; }
        playerHpRoot.gameObject.SetActive(true);
        playerHpRoot.position = screenPos;
    }

    // ── 刷新 ──────────────────────────────────────────────
    void RefreshHud()
    {
        RefreshPlayerHp();
        RefreshBossHp();
        RefreshSkillSlots();
        RefreshStackTexts();
        RefreshCastAndGcd();
    }

    void RefreshPlayerHp()
    {
        if (_playerStats == null) return;
        float ratio = (float)_playerStats.CurrentHp / _playerStats.MaxHp;
        if (playerHpSlider != null) playerHpSlider.value = ratio;
        if (playerHpText != null)
        {
            playerHpText.text = $"{_playerStats.CurrentHp}/{_playerStats.MaxHp}";
            playerHpText.color = Color.black;
        }
    }

    void RefreshBossHp()
    {
        if (_skills == null) return;
        int dmg = _skills.TotalDamage;
        if (bossHpSlider != null) bossHpSlider.value = Mathf.Min(dmg, _bossFakeMaxHp);
        if (bossHpText != null) bossHpText.text = $"Total DMG: {dmg}";
    }

    void RefreshSkillSlots()
    {
        if (_skills == null || skillSlots == null) return;
        if (skillSlots.Length > 0) skillSlots[0].Refresh(
            _skills.GcdReady && !_skills.Skill1Casting,
            _skills.GcdRemaining, _skills.GcdDuration,
            _skills.Skill1Casting, _skills.Skill1CastProgress);
        if (skillSlots.Length > 1) skillSlots[1].Refresh(
            _skills.GcdReady && _skills.Skill2Ready,
            _skills.GcdRemaining, _skills.GcdDuration);
        // 技能3：根據 Skill3ProcReady 顯示發光
        if (skillSlots.Length > 2) skillSlots[2].SetProcGlow(_skills.Skill3ProcReady);
        if (skillSlots.Length > 3) skillSlots[3].Refresh(
            _skills.Skill4Ready,
            _skills.Skill4Active ? _skills.Skill4Remaining : _skills.Skill4Cooldown,
            _skills.Skill4Active ? 20f : 60f,
            false, 0f, _skills.Skill4Active);
    }

    void RefreshStackTexts()
    {
        if (_skills == null) return;

        // ── 技能1疊層：5個圓圈，橘紅填充 ────────────────
        if (skill1StackDots != null)
        {
            int stack = _skills.Skill1Stack;
            for (int i = 0; i < skill1StackDots.Length; i++)
            {
                if (skill1StackDots[i] == null) continue;
                skill1StackDots[i].color = (i < stack) ? DOT_FILLED : DOT_EMPTY;
            }
        }

        if (skill3ProcChargeBars != null)
        {
            int charges = _skills.Skill3ProcCharges;
            for (int i = 0; i < skill3ProcChargeBars.Length; i++)
            {
                if (skill3ProcChargeBars[i] == null) continue;
                skill3ProcChargeBars[i].color = i < charges
                    ? PROC_CHARGE_FILLED
                    : PROC_CHARGE_EMPTY;
            }
        }

        // 技能2疊層（Skill3 Stacks）：黃色，血條左側
        if (skill2StackText != null)
        {
            bool hasStack2 = _skills.Skill3Stacks > 0;
            skill2StackText.text = hasStack2 ? $"{_skills.Skill3Stacks}" : "";
            skill2StackText.color = new Color(1f, 0.85f, 0.2f);
            if (skill2StackBg != null)
            {
                skill2StackBg.color = STACK_BG_COLOR;
                skill2StackBg.gameObject.SetActive(hasStack2);
            }
        }
    }

    void RefreshCastAndGcd()
    {
        if (_skills == null) return;

        bool casting = _skills.Skill1Casting;
        if (skill1CastBarRoot != null)
            skill1CastBarRoot.SetActive(casting);
        if (skill1CastFill != null)
            skill1CastFill.fillAmount = casting ? _skills.Skill1CastProgress : 0f;

        if (gcdIndicator != null)
        {
            bool onGcd = !_skills.GcdReady;
            gcdIndicator.fillAmount = onGcd
                ? Mathf.Clamp01(_skills.GcdRemaining / _skills.GcdDuration)
                : 0f;
        }
    }

    void RefreshTimer()
    {
        if (_timer == null || timerText == null) return;
        timerText.text = _timer.FormatTime();
    }

    void RefreshDps()
    {
        if (_skills == null || _timer == null) return;
        float t = _timer.ElapsedSeconds;
        float dps = t > 0 ? _skills.TotalDamage / t : 0f;

        if (dpsText != null)
            dpsText.text = $"DPS: {dps:F1}";

        if (skillStatsText != null && _skills.TotalDamage > 0)
        {
            int total = _skills.TotalDamage;
            skillStatsText.text =
                $"S1: {_skills.Skill1UseCount}x  {Pct(_skills.Skill1TotalDmg, total)}%\n" +
                $"S2: {_skills.Skill2UseCount}x  {Pct(_skills.Skill2TotalDmg, total)}%\n" +
                $"S3: {_skills.Skill3UseCount}x  {Pct(_skills.Skill3TotalDmg, total)}%\n" +
                $"S4: {_skills.Skill4UseCount}x";
        }
    }

    int Pct(int part, int total) => total > 0 ? Mathf.RoundToInt(part * 100f / total) : 0;

    // ── 傷害跳字 ──────────────────────────────────────────
    void OnDamageDealt(int dmg, float worldY)
    {
        if (damageNumberPrefab == null || worldCanvas == null || _bossTransform == null) return;
        var go = Instantiate(damageNumberPrefab, worldCanvas.transform);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            tmp.text = dmg.ToString();
            tmp.color = _skills.Skill4Active ? Color.yellow : Color.white;
        }
        Vector3 screenPos = _cam.WorldToScreenPoint(
            _bossTransform.position + Vector3.up * 3f + Random.insideUnitSphere * 0.5f);
        go.GetComponent<RectTransform>().position = screenPos;
        StartCoroutine(FloatAndFade(go));
        RefreshBossHp();
    }

    IEnumerator FloatAndFade(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        var tmp = go.GetComponent<TextMeshProUGUI>();
        float t = 0f;
        Vector3 startPos = rt.position;
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f;
            rt.position = startPos + Vector3.up * (60f * t);
            if (tmp != null) tmp.alpha = 1f - t;
            yield return null;
        }
        Destroy(go);
    }

    // ── 技能3發光 ─────────────────────────────────────────
    void OnSkill3Proc()
    {
        if (skillSlots != null && skillSlots.Length > 2)
            skillSlots[2].TriggerProcFlash();
        AddSystemLog($"Skill3 Proc! Stored: {_skills.Skill3ProcCharges}/{SkillSystem.SKILL3_PROC_CHARGES_MAX} (Press 3 to cast)");
    }

    // ── 系統提示（限制在框內）────────────────────────────
    public void AddSystemLog(string msg)
    {
        _systemLogs.Add($"[{System.DateTime.Now:HH:mm:ss}] {msg}");
        if (_systemLogs.Count > 30) _systemLogs.RemoveAt(0);
        if (systemLogText != null)
            systemLogText.text = string.Join("\n", _systemLogs);
        if (systemLogScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            systemLogScroll.verticalNormalizedPosition = 0f;
        }
    }

    public void ShowBossSkillAlert(string msg, float castDuration = 3f)
    {
        if (bossSkillAlertText == null) return;

        EnsureBossCastUi();
        bossSkillAlertText.text = msg;
        if (bossCastBarRoot != null) bossCastBarRoot.SetActive(true);
        SetBossCastProgress(0f);

        if (_bossCastRoutine != null) StopCoroutine(_bossCastRoutine);
        _bossCastRoutine = StartCoroutine(UpdateBossCastBar(Mathf.Max(0.05f, castDuration)));
    }

    IEnumerator UpdateBossCastBar(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (GameManager.Instance == null || GameManager.Instance.IsPlaying)
                elapsed += Time.deltaTime;

            SetBossCastProgress(elapsed / duration);
            yield return null;
        }

        SetBossCastProgress(1f);
        yield return new WaitForSeconds(0.12f);
        if (bossSkillAlertText != null) bossSkillAlertText.text = "";
        if (bossCastBarRoot != null) bossCastBarRoot.SetActive(false);
        _bossCastRoutine = null;
    }

    void OnPlayerDamageTaken(DamageTakenRecord damageRecord)
    {
        RefreshDamageTakenLog();
        AddSystemLog($"[{damageRecord.FormatTimestamp()}] {damageRecord.Source}: -{damageRecord.Amount} HP");
    }

    void SetBossCastProgress(float progress)
    {
        if (bossCastFill == null) return;
        progress = Mathf.Clamp01(progress);
        bossCastFill.fillAmount = progress;
        var fillRect = bossCastFill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(progress, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    void RefreshDamageTakenLog()
    {
        if (damageTakenText == null || _playerStats == null) return;

        var history = _playerStats.DamageHistory;
        if (history.Count == 0)
        {
            damageTakenText.text = "No damage taken";
            return;
        }

        int first = Mathf.Max(0, history.Count - 6);
        var sb = new System.Text.StringBuilder();
        for (int i = first; i < history.Count; i++)
        {
            var entry = history[i];
            sb.Append("<color=#AAAAAA>[")
              .Append(entry.FormatTimestamp())
              .Append("]</color> ")
              .Append(entry.Source)
              .Append("  <color=#FF7777>-")
              .Append(entry.Amount)
              .Append(" HP</color>");
            if (i < history.Count - 1) sb.AppendLine();
        }
        damageTakenText.text = sb.ToString();
    }

    void EnsureBossCastUi()
    {
        if (bossCastBarRoot == null)
        {
            var root = new GameObject("BossCastAlert", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(transform, false);
            var rt = root.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -72f);
            rt.sizeDelta = new Vector2(620f, 58f);
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            bossCastBarRoot = root;
        }

        if (bossSkillAlertText == null)
        {
            bossSkillAlertText = bossCastBarRoot.GetComponentInChildren<TextMeshProUGUI>();
            if (bossSkillAlertText == null)
            {
                var textGo = new GameObject("BossSkillAlert", typeof(RectTransform));
                textGo.transform.SetParent(bossCastBarRoot.transform, false);
                bossSkillAlertText = textGo.AddComponent<TextMeshProUGUI>();
            }
        }

        bossSkillAlertText.transform.SetParent(bossCastBarRoot.transform, false);
        bossSkillAlertText.fontSize = 18f;
        bossSkillAlertText.alignment = TextAlignmentOptions.Center;
        bossSkillAlertText.color = new Color(1f, 0.45f, 0.35f);
        bossSkillAlertText.raycastTarget = false;
        var textRt = bossSkillAlertText.rectTransform;
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 0f);
        textRt.pivot = new Vector2(0.5f, 0f);
        textRt.anchoredPosition = new Vector2(0f, 5f);
        textRt.sizeDelta = new Vector2(-16f, 30f);

        if (bossCastFill == null)
        {
            var barBg = new GameObject("CastBarBackground", typeof(RectTransform), typeof(Image));
            barBg.transform.SetParent(bossCastBarRoot.transform, false);
            var bgRt = barBg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 1f);
            bgRt.anchorMax = new Vector2(1f, 1f);
            bgRt.pivot = new Vector2(0.5f, 1f);
            bgRt.anchoredPosition = new Vector2(0f, -4f);
            bgRt.sizeDelta = new Vector2(-8f, 14f);
            barBg.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

            var fillGo = new GameObject("CastBarFill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(barBg.transform, false);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.zero;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            bossCastFill = fillGo.GetComponent<Image>();
            bossCastFill.color = new Color(1f, 0.28f, 0.12f, 1f);
            bossCastFill.type = Image.Type.Simple;
        }

        bossCastFill.type = Image.Type.Simple;
        SetBossCastProgress(0f);
        bossCastBarRoot.SetActive(false);
    }

    void EnsureDamageTakenUi()
    {
        if (damageTakenPanel == null)
        {
            var panel = new GameObject("DamageTakenPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(10f, -160f);
            rt.sizeDelta = new Vector2(390f, 170f);
            panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            damageTakenPanel = panel;
        }

        if (damageTakenPanel.transform.Find("Title") == null)
        {
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(damageTakenPanel.transform, false);
            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.text = "DAMAGE TAKEN";
            title.fontSize = 13f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Left;
            title.color = new Color(1f, 0.55f, 0.5f);
            title.raycastTarget = false;
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -7f);
            rt.sizeDelta = new Vector2(-16f, 22f);
        }

        if (damageTakenText == null)
        {
            var textGo = new GameObject("DamageTakenText", typeof(RectTransform));
            textGo.transform.SetParent(damageTakenPanel.transform, false);
            damageTakenText = textGo.AddComponent<TextMeshProUGUI>();
        }

        damageTakenText.fontSize = 12f;
        damageTakenText.alignment = TextAlignmentOptions.TopLeft;
        damageTakenText.color = Color.white;
        damageTakenText.textWrappingMode = TextWrappingModes.NoWrap;
        damageTakenText.overflowMode = TextOverflowModes.Truncate;
        damageTakenText.raycastTarget = false;
        var textRect = damageTakenText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(8f, 8f);
        textRect.offsetMax = new Vector2(-8f, -31f);
    }

    // ── 技能按下閃爍（由 SkillSystem 呼叫）──────────────
    public void TriggerSkillPress(int skillIndex)
    {
        if (skillSlots != null && skillIndex < skillSlots.Length)
            skillSlots[skillIndex].TriggerPressFlash();
    }
}
