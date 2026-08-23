using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 3 分鐘技能時間軸控制器。
/// 依照時間觸發一次性事件，支援暫停、重置、Debug 倍速。
/// </summary>
public class BossTimelineController : MonoBehaviour
{
    // ── Inspector 設定 ────────────────────────────────────
    [Header("時間軸設定")]
    [Tooltip("時間軸速度倍率（1=正常，5=Debug 快速）")]
    public float timelineSpeed = 1f;
    [Tooltip("是否自動開始")]
    public bool autoStart = true;
    [Tooltip("Debug：從指定秒數開始（0=正常開始）")]
    public float debugStartTime = 0f;

    [Header("Debug 顯示（唯讀）")]
    [SerializeField] float _currentTime;
    [SerializeField] string _currentPhase = "Idle";
    [SerializeField] string _currentMechanic = "None";

    // ── 內部狀態 ──────────────────────────────────────────
    bool _running;
    int _nextEventIndex;
    int _checkerboardVariant = 0;
    int _rollingExplosionCount = 0;

    // ── 外部參考 ──────────────────────────────────────────
    BossMechanicController _mechanic;
    BossArenaGrid _grid;
    HudManager _hud;
    Transform _playerTransform;
    Transform _bossTransform;
    BattleTimer _battleTimer;

    // ── 時間軸事件列表 ────────────────────────────────────
    struct TimelineEvent
    {
        public float time;
        public string name;
        public System.Action action;
        public TimelineEvent(float t, string n, System.Action a) { time = t; name = n; action = a; }
    }

    List<TimelineEvent> _events = new();
    List<Coroutine> _activeCoroutines = new();

    // ══════════════════════════════════════════════════════
    void Awake()
    {
        _mechanic = GetComponent<BossMechanicController>();
        if (_mechanic == null) _mechanic = gameObject.AddComponent<BossMechanicController>();
    }

    void Start()
    {
        _grid = BossArenaGrid.Instance;
        _hud = FindFirstObjectByType<HudManager>();
        _battleTimer = FindFirstObjectByType<BattleTimer>();

        var player = GameObject.Find("Player");
        if (player != null) _playerTransform = player.transform;

        var boss = GameObject.Find("Boss");
        if (boss != null) _bossTransform = boss.transform;

        if (autoStart) StartTimeline();
    }

    void Update()
    {
        if (!_running) return;
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        _currentTime += Time.deltaTime * timelineSpeed;

        // 觸發到期事件
        while (_nextEventIndex < _events.Count &&
               _currentTime >= _events[_nextEventIndex].time)
        {
            var ev = _events[_nextEventIndex];
            _nextEventIndex++;
            _currentMechanic = ev.name;
            Debug.Log($"[BossTimeline] t={ev.time:F2}s → {ev.name}");
            ev.action?.Invoke();
        }

        // 3:00 勝利判定
        if (_currentTime >= 180f && _running)
        {
            _running = false;
            _currentPhase = "Victory";
            _currentMechanic = "Done";
            OnTimelineComplete();
        }
    }

    // ══════════════════════════════════════════════════════
    // 公開 API
    // ══════════════════════════════════════════════════════
    public void StartTimeline()
    {
        ResetTimeline();
        BuildEvents();
        _running = true;
        _currentTime = debugStartTime;

        // Debug 從 Phase 3 開始時，也要維持正式時間軸的王位置。
        if (_currentTime >= 120f)
            MoveBossImmediatelyToCenter();

        // 跳過已過期事件
        while (_nextEventIndex < _events.Count &&
               _events[_nextEventIndex].time <= _currentTime)
            _nextEventIndex++;

        Debug.Log($"[BossTimeline] 時間軸啟動，起始時間={_currentTime:F1}s，速度={timelineSpeed}x");
    }

    public void ResetTimeline()
    {
        _running = false;
        _currentTime = 0f;
        _nextEventIndex = 0;
        _currentPhase = "Idle";
        _currentMechanic = "None";
        _checkerboardVariant = 0;
        _rollingExplosionCount = 0;

        // 停止所有 Coroutine
        foreach (var c in _activeCoroutines)
            if (c != null) StopCoroutine(c);
        _activeCoroutines.Clear();

        // 清除所有危險區
        if (_mechanic != null) _mechanic.ClearAll();

        _events.Clear();
    }

    // ══════════════════════════════════════════════════════
    // 建立時間軸事件
    // ══════════════════════════════════════════════════════
    void BuildEvents()
    {
        _events.Clear();

        // ── Phase 1：單獨教學 0:00～1:00 ──────────────────
        Add(0f,   "Phase1開始",    () => { _currentPhase = "Phase 1"; });
        Add(8f,   "震地脈衝",      DoEarthquakePulse);
        Add(16f,  "崩裂直線",      DoLineCleave_Phase1);
        Add(27f,  "追蹤落雷",      DoTrackedThunder_Single);
        Add(39f,  "斷層棋盤",      DoCheckerboard_Phase1);
        Add(52f,  "崩裂+落雷",     DoLinePlusThunder);
        Add(60f,  "地脈升壓",      DoEarthquakePulse2);

        // ── Phase 2：機制連鎖 1:00～2:00 ──────────────────
        Add(60.1f,"Phase2開始",    () => { _currentPhase = "Phase 2"; });
        Add(65f,  "避雷錨點",      DoAnchorPoint_Phase2);
        Add(73f,  "風暴推進",      DoStormKnockback_Phase2);
        Add(87f,  "地脈連爆",      DoRollingExplosion_Phase2);
        Add(100f, "棋盤+直線",     DoCheckerboardPlusLine);
        Add(114f, "雷鳴二連",      DoDoubleThunderPulse);
        Add(120f, "Phase2結束",    DoPhase2End);

        // ── Phase 3：組合考試 2:00～3:00 ──────────────────
        Add(120.1f,"Phase3開始",   () => { _currentPhase = "Phase 3"; });
        Add(125f, "雷環掃掠",      DoSweepBeam_Phase3);
        Add(140f, "雙重追雷",      DoDoubleTrackedThunder);
        Add(152f, "連爆+直線",     DoRollingPlusLine);
        Add(163f, "避雷錨點2",     DoAnchorPoint_Phase3);
        Add(166f, "風暴推進2",     DoStormKnockback_Phase3);
        Add(170f, "天崩地裂讀條",  DoFinalCast);
        Add(172f, "最終波一棋盤",  DoFinalCheckerboard);
        Add(175f, "最終波二十字",  DoFinalCross);
        Add(178f, "最終波三四角",  DoFinalCorners);
    }

    void Add(float time, string name, System.Action action)
    {
        _events.Add(new TimelineEvent(time, name, action));
    }

    // ══════════════════════════════════════════════════════
    // Phase 1 技能
    // ══════════════════════════════════════════════════════

    void DoEarthquakePulse()
    {
        ShowAlert("Earthquake Pulse", 2f);
        _mechanic.DoEarthquakePulse(8, 2f, _bossTransform, "Earthquake Pulse");
    }

    void DoEarthquakePulse2()
    {
        ShowAlert("Ley Line Surge", 2f);
        _mechanic.DoEarthquakePulse(8, 2f, _bossTransform, "Ley Line Surge");
    }

    void DoLineCleave_Phase1()
    {
        ShowAlert("Line Cleave", 2.5f);
        if (_playerTransform == null) return;
        (int row, int col) = _grid.WorldPositionToCell(_playerTransform.position);
        bool useRow = Random.value > 0.5f;
        _mechanic.DoLineCleave(useRow, useRow ? row : col, 2.5f, 25, 8f, "Line Cleave");
    }

    void DoTrackedThunder_Single()
    {
        ShowAlert("Tracked Thunder", 2.5f);
        if (_playerTransform == null) return;
        Vector3 lockedPos = _playerTransform.position;
        _mechanic.DoTrackedThunder(lockedPos, 2.5f, 25, "Tracked Thunder");
    }

    void DoCheckerboard_Phase1()
    {
        ShowAlert("Fault Checkerboard", 3.5f);
        _mechanic.DoCheckerboard(_checkerboardVariant, 3.5f, 25, "Fault Checkerboard");
        _checkerboardVariant = 1 - _checkerboardVariant;
    }

    void DoLinePlusThunder()
    {
        ShowAlert("Line Cleave", 2.5f);
        if (_playerTransform == null) return;
        (int row, int col) = _grid.WorldPositionToCell(_playerTransform.position);
        bool useRow = Random.value > 0.5f;
        _mechanic.DoLineCleave(useRow, useRow ? row : col, 2.5f, 25, 8f, "Line Cleave");

        var c = StartCoroutine(DelayedAction(1.5f, () =>
        {
            ShowAlert("Tracked Thunder", 2.5f);
            if (_playerTransform != null)
                _mechanic.DoTrackedThunder(_playerTransform.position, 2.5f, 25, "Tracked Thunder");
        }));
        _activeCoroutines.Add(c);
    }

    // ══════════════════════════════════════════════════════
    // Phase 2 技能
    // ══════════════════════════════════════════════════════

    void DoAnchorPoint_Phase2()
    {
        ShowAlert("Anchor Point", 5f);
        var centerCells = new[] { (1, 1), (1, 2), (2, 1), (2, 2) };
        var chosen = centerCells[Random.Range(0, centerCells.Length)];
        _mechanic.DoAnchorPoint(chosen.Item1, chosen.Item2, 5f, 40, 12f, "Anchor Point");
    }

    void DoStormKnockback_Phase2()
    {
        ShowAlert("Storm Surge", 4f);
        _mechanic.DoStormKnockback(4f, 10, 18f, "Storm Surge");
    }

    void DoRollingExplosion_Phase2()
    {
        ShowAlert("Rolling Explosion", 3f);
        bool northToSouth = (_rollingExplosionCount % 2 == 0);
        _mechanic.DoRollingExplosion(northToSouth, 0.8f, 25, "Rolling Explosion");
        _rollingExplosionCount++;
    }

    void DoCheckerboardPlusLine()
    {
        ShowAlert("Fault Checkerboard", 3.5f);
        _mechanic.DoCheckerboard(_checkerboardVariant, 3.5f, 25, "Fault Checkerboard");
        _checkerboardVariant = 1 - _checkerboardVariant;

        var c = StartCoroutine(DelayedAction(1.5f, () =>
        {
            ShowAlert("Line Cleave", 2.5f);
            if (_playerTransform != null)
            {
                (int row, int col) = _grid.WorldPositionToCell(_playerTransform.position);
                bool useRow = Random.value > 0.5f;
                _mechanic.DoLineCleave(useRow, useRow ? row : col, 2.5f, 25, 8f, "Line Cleave");
            }
        }));
        _activeCoroutines.Add(c);
    }

    void DoDoubleThunderPulse()
    {
        ShowAlert("Double Thunder", 1.5f);
        _mechanic.DoEarthquakePulse(6, 0.5f, _bossTransform, "Double Thunder");
        var c = StartCoroutine(DelayedAction(1f, () =>
        {
            _mechanic.DoEarthquakePulse(6, 0.5f, _bossTransform, "Double Thunder");
        }));
        _activeCoroutines.Add(c);
    }

    void DoPhase2End()
    {
        ShowAlert("OVERLOAD", 4f);
        _currentPhase = "Phase 2 -> 3";
        var c = StartCoroutine(BossOverloadFx());
        _activeCoroutines.Add(c);

        var move = StartCoroutine(MoveBossToCenter(2f));
        _activeCoroutines.Add(move);
    }

    IEnumerator BossOverloadFx()
    {
        if (_bossTransform == null) yield break;

        var fxGo = new GameObject("OverloadFx");
        fxGo.transform.SetParent(_bossTransform, false);
        fxGo.transform.localPosition = Vector3.zero;

        var ps = fxGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 4f;
        main.loop = false;
        main.startLifetime = 2f;
        main.startSpeed = 5f;
        main.startSize = 0.8f;
        main.startColor = new Color(1f, 0.3f, 0f, 0.9f);
        main.maxParticles = 200;

        var emission = ps.emission;
        emission.rateOverTime = 50f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.5f;

        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        ps.Play();

        yield return new WaitForSeconds(4f);
        Destroy(fxGo);
    }

    IEnumerator MoveBossToCenter(float duration)
    {
        if (_bossTransform == null) yield break;

        Vector3 start = _bossTransform.position;
        Vector3 destination = GetArenaCenterAtCurrentHeight(start);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying)
            {
                yield return null;
                continue;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            _bossTransform.position = Vector3.LerpUnclamped(start, destination, t);
            yield return null;
        }

        _bossTransform.position = destination;
    }

    void MoveBossImmediatelyToCenter()
    {
        if (_bossTransform != null)
            _bossTransform.position = GetArenaCenterAtCurrentHeight(_bossTransform.position);
    }

    public static Vector3 GetArenaCenterAtCurrentHeight(Vector3 currentPosition)
    {
        return new Vector3(0f, currentPosition.y, 0f);
    }

    // ══════════════════════════════════════════════════════
    // Phase 3 技能
    // ══════════════════════════════════════════════════════

    void DoSweepBeam_Phase3()
    {
        ShowAlert("Thunder Sweep", 8f);
        float startAngle = Random.Range(0f, 90f);
        _mechanic.DoSweepBeam(startAngle, 4, 2f, 25, "Thunder Sweep");
    }

    void DoDoubleTrackedThunder()
    {
        ShowAlert("Double Thunder", 4.5f);
        if (_playerTransform == null) return;

        Vector3 pos1 = _playerTransform.position;
        _mechanic.DoTrackedThunder(pos1, 2.5f, 25, "Double Thunder");

        var c = StartCoroutine(DelayedAction(2f, () =>
        {
            if (_playerTransform != null)
            {
                Vector3 pos2 = _playerTransform.position;
                _mechanic.DoTrackedThunder(pos2, 2.5f, 25, "Double Thunder");
            }
        }));
        _activeCoroutines.Add(c);
    }

    void DoRollingPlusLine()
    {
        ShowAlert("Rolling Explosion", 3f);
        bool northToSouth = (_rollingExplosionCount % 2 == 0);
        _mechanic.DoRollingExplosion(northToSouth, 0.8f, 25, "Rolling Explosion");
        _rollingExplosionCount++;

        var c = StartCoroutine(DelayedAction(1.5f, () =>
        {
            ShowAlert("Line Cleave", 2.5f);
            if (_playerTransform != null)
            {
                (int row, int col) = _grid.WorldPositionToCell(_playerTransform.position);
                bool useRow = Random.value > 0.5f;
                _mechanic.DoLineCleave(useRow, useRow ? row : col, 2.5f, 25, 8f, "Line Cleave");
            }
        }));
        _activeCoroutines.Add(c);
    }

    void DoAnchorPoint_Phase3()
    {
        ShowAlert("Anchor Point", 3f);
        var centerCells = new[] { (1, 1), (1, 2), (2, 1), (2, 2) };
        var chosen = centerCells[Random.Range(0, centerCells.Length)];
        _mechanic.DoAnchorPoint(chosen.Item1, chosen.Item2, 3f, 40, 12f, "Anchor Point");
    }

    void DoStormKnockback_Phase3()
    {
        ShowAlert("Storm Surge", 3f);
        _mechanic.DoStormKnockback(3f, 10, 18f, "Storm Surge");
    }

    void DoFinalCast()
    {
        ShowAlert("TERRA BREAK", 10f);
        _currentMechanic = "TERRA BREAK - Casting";
        var c = StartCoroutine(FinalCastFx());
        _activeCoroutines.Add(c);
    }

    IEnumerator FinalCastFx()
    {
        if (_bossTransform == null) yield break;

        var fxGo = new GameObject("FinalCastFx");
        fxGo.transform.position = _bossTransform.position;

        var ps = fxGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 10f;
        main.loop = false;
        main.startLifetime = 3f;
        main.startSpeed = 10f;
        main.startSize = 1f;
        main.startColor = new Color(1f, 0.8f, 0f, 1f);
        main.maxParticles = 300;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 2f;

        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        ps.Play();

        yield return new WaitForSeconds(10f);
        Destroy(fxGo);
    }

    void DoFinalCheckerboard()
    {
        ShowAlert("Fault Checkerboard", 1.5f);
        _mechanic.DoCheckerboard(_checkerboardVariant, 1.5f, 30, "Fault Checkerboard");
        _checkerboardVariant = 1 - _checkerboardVariant;
    }

    void DoFinalCross()
    {
        ShowAlert("Cross Cleave", 1.5f);
        _mechanic.DoCrossCleave(1, 1, 1.5f, 30, "Cross Cleave");
    }

    void DoFinalCorners()
    {
        ShowAlert("Corner Blast", 1.5f);
        _mechanic.DoCornerExplosion(1.5f, 30, "Corner Blast");
    }

    // ══════════════════════════════════════════════════════
    // 時間軸完成
    // ══════════════════════════════════════════════════════
    void OnTimelineComplete()
    {
        Debug.Log("[BossTimeline] 3分鐘時間軸完成！");
        _mechanic.ClearAll();

        if (GameManager.Instance == null) return;
        if (GameManager.Instance.State == GameManager.GameState.Playing)
        {
            GameManager.Instance.SetState(GameManager.GameState.Victory);
        }
    }

    // ══════════════════════════════════════════════════════
    // 輔助方法
    // ══════════════════════════════════════════════════════
    void ShowAlert(string msg, float castDuration)
    {
        if (_hud != null) _hud.ShowBossSkillAlert(msg, castDuration);
        if (_hud != null) _hud.AddSystemLog($"[Boss] {msg}");
    }

    IEnumerator DelayedAction(float delay, System.Action action)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
                elapsed += Time.deltaTime;
            yield return null;
        }
        action?.Invoke();
    }
}
