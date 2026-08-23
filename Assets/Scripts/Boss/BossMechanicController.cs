using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 機制控制器：建立、顯示、清除各種地板攻擊危險區。
/// 每個危險區只能對玩家造成一次傷害。
/// </summary>
public class BossMechanicController : MonoBehaviour
{
    public static BossMechanicController Instance { get; private set; }

    // ── 顏色常數 ──────────────────────────────────────────
    static readonly Color DANGER_PREVIEW   = new Color(1f, 0.1f, 0.1f, 0.35f);
    static readonly Color DANGER_FLASH     = new Color(1f, 0.5f, 0.0f, 0.75f);
    static readonly Color DANGER_EXPLODE   = new Color(1f, 1f, 0.8f, 0.95f);
    static readonly Color ANCHOR_COLOR     = new Color(0.0f, 0.9f, 1.0f, 0.45f);
    static readonly Color ANCHOR_SAFE_COLOR= new Color(0.0f, 1.0f, 0.8f, 0.60f);
    static readonly Color TETHER_COLOR     = new Color(0.0f, 0.85f, 1.0f, 0.50f);
    static readonly Color SWEEP_COLOR      = new Color(1.0f, 0.85f, 0.0f, 0.40f);

    // ── 內部狀態 ──────────────────────────────────────────
    readonly List<GameObject> _activeHazards = new();
    readonly List<Coroutine>  _activeCoroutines = new();

    // 固定力場：玩家站在其中時擊退降低 80%
    GameObject _tetherFieldObj;
    Vector3    _tetherFieldCenter;
    float      _tetherFieldRadius;
    float      _tetherFieldExpiry;
    bool       _tetherFieldActive;

    // 玩家參考
    Transform  _playerTransform;
    Transform  _bossTransform;
    PlayerStats _playerStats;
    PlayerController _playerController;

    BossArenaGrid _grid;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _grid = BossArenaGrid.Instance;
        _bossTransform = GameObject.Find("Boss")?.transform;
        var player = GameObject.Find("Player");
        if (player != null)
        {
            _playerTransform  = player.transform;
            _playerStats      = player.GetComponent<PlayerStats>();
            _playerController = player.GetComponent<PlayerController>();
        }
    }

    void Update()
    {
        // 固定力場計時
        if (_tetherFieldActive && Time.time > _tetherFieldExpiry)
        {
            ClearTetherField();
        }
    }

    // ══════════════════════════════════════════════════════
    // 公開 API：清除所有危險區
    // ══════════════════════════════════════════════════════
    public void ClearAll()
    {
        // 停止所有 Coroutine
        foreach (var c in _activeCoroutines)
            if (c != null) StopCoroutine(c);
        _activeCoroutines.Clear();

        // 銷毀所有危險區物件
        foreach (var go in _activeHazards)
            if (go != null) Destroy(go);
        _activeHazards.Clear();

        ClearTetherField();
    }

    // ══════════════════════════════════════════════════════
    // 1. 震地脈衝（全場傷害，無地板預告）
    // ══════════════════════════════════════════════════════
    public void DoEarthquakePulse(int damage, float castTime, Transform bossTransform, string damageSource = "Earthquake Pulse")
    {
        var c = StartCoroutine(EarthquakePulseRoutine(damage, castTime, bossTransform, damageSource));
        _activeCoroutines.Add(c);
    }

    IEnumerator EarthquakePulseRoutine(int damage, float castTime, Transform bossTransform, string damageSource)
    {
        // 在 Boss 身上建立擴散環粒子
        GameObject ringFx = CreateBossRingFx(bossTransform);

        yield return new WaitForSeconds(castTime);

        // 造成全場傷害
        DealDamageToPlayer(damage, damageSource);

        if (ringFx != null) Destroy(ringFx, 0.5f);
    }

    // ══════════════════════════════════════════════════════
    // 2. 崩裂直線（鎖定 row 或 col）
    // ══════════════════════════════════════════════════════
    public void DoLineCleave(bool isRow, int index, float warnTime, int damage, float knockback, string damageSource = "Line Cleave")
    {
        var c = StartCoroutine(LineCleaveRoutine(isRow, index, warnTime, damage, knockback, damageSource));
        _activeCoroutines.Add(c);
    }

    IEnumerator LineCleaveRoutine(bool isRow, int index, float warnTime, int damage, float knockback, string damageSource)
    {
        (Vector3 center, Vector3 size) bounds = isRow
            ? _grid.GetRowBounds(index)
            : _grid.GetColBounds(index);

        var hazard = CreateFlatQuad(bounds.center, bounds.size, DANGER_PREVIEW, "LineCleave");
        _activeHazards.Add(hazard);

        // 閃爍警告
        yield return FlashWarning(hazard, warnTime, 0.6f);

        // 爆炸閃光
        yield return ExplodeFlash(hazard);

        // 傷害判定
        bool hit = IsPlayerInBounds(bounds.center, bounds.size);
        if (hit)
        {
            DealDamageToPlayer(damage, damageSource);
            ApplyKnockback(knockback);
        }

        DestroyHazard(hazard);
    }

    // ══════════════════════════════════════════════════════
    // 3. 追蹤落雷（鎖定格子，不追蹤）
    // ══════════════════════════════════════════════════════
    public void DoTrackedThunder(Vector3 lockedWorldPos, float warnTime, int damage, string damageSource = "Tracked Thunder")
    {
        var c = StartCoroutine(TrackedThunderRoutine(lockedWorldPos, warnTime, damage, damageSource));
        _activeCoroutines.Add(c);
    }

    IEnumerator TrackedThunderRoutine(Vector3 lockedWorldPos, float warnTime, int damage, string damageSource)
    {
        (int row, int col) = _grid.WorldPositionToCell(lockedWorldPos);
        Vector3 cellCenter = _grid.GetCellCenter(row, col);
        float cs = _grid.CellSize;
        Vector3 size = new Vector3(cs, 0.01f, cs);

        var hazard = CreateFlatQuad(cellCenter, size, DANGER_PREVIEW, "TrackedThunder");
        _activeHazards.Add(hazard);

        yield return FlashWarning(hazard, warnTime, 0.6f);
        yield return ExplodeFlash(hazard);

        bool hit = IsPlayerInCell(row, col);
        if (hit) DealDamageToPlayer(damage, damageSource);

        DestroyHazard(hazard);
    }

    // ══════════════════════════════════════════════════════
    // 4. 斷層棋盤
    // ══════════════════════════════════════════════════════
    public void DoCheckerboard(int patternVariant, float warnTime, int damage, string damageSource = "Fault Checkerboard")
    {
        var c = StartCoroutine(CheckerboardRoutine(patternVariant, warnTime, damage, damageSource));
        _activeCoroutines.Add(c);
    }

    IEnumerator CheckerboardRoutine(int patternVariant, float warnTime, int damage, string damageSource)
    {
        float cs = _grid.CellSize;
        Vector3 cellSize = new Vector3(cs, 0.01f, cs);
        var hazards = new List<GameObject>();
        var dangerCells = new List<(int row, int col)>();

        for (int r = 0; r < 4; r++)
        {
            for (int c2 = 0; c2 < 4; c2++)
            {
                if ((r + c2) % 2 == patternVariant)
                {
                    Vector3 center = _grid.GetCellCenter(r, c2);
                    var h = CreateFlatQuad(center, cellSize, DANGER_PREVIEW, "Checkerboard");
                    hazards.Add(h);
                    _activeHazards.Add(h);
                    dangerCells.Add((r, c2));
                }
            }
        }

        // 閃爍
        float elapsed = 0f;
        float flashInterval = 0.3f;
        bool bright = false;
        while (elapsed < warnTime)
        {
            if (!GameManager.Instance.IsPlaying) { yield return null; elapsed += 0f; continue; }
            float dt = Time.deltaTime;
            elapsed += dt;
            flashInterval -= dt;
            if (flashInterval <= 0f && elapsed > warnTime * 0.6f)
            {
                bright = !bright;
                flashInterval = 0.15f;
                Color fc = bright ? DANGER_FLASH : DANGER_PREVIEW;
                foreach (var h in hazards) SetQuadColor(h, fc);
            }
            yield return null;
        }

        // 爆炸
        foreach (var h in hazards) SetQuadColor(h, DANGER_EXPLODE);
        yield return new WaitForSeconds(0.15f);

        // 傷害判定
        bool playerHit = false;
        foreach (var (row, col) in dangerCells)
        {
            if (!playerHit && IsPlayerInCell(row, col))
            {
                playerHit = true;
                DealDamageToPlayer(damage, damageSource);
            }
        }

        foreach (var h in hazards) DestroyHazard(h);
    }

    // ══════════════════════════════════════════════════════
    // 5. 避雷錨點
    // ══════════════════════════════════════════════════════
    public void DoAnchorPoint(int row, int col, float warnTime, int failDamage, float tetherDuration, string damageSource = "Anchor Point")
    {
        var c = StartCoroutine(AnchorPointRoutine(row, col, warnTime, failDamage, tetherDuration, damageSource));
        _activeCoroutines.Add(c);
    }

    IEnumerator AnchorPointRoutine(int row, int col, float warnTime, int failDamage, float tetherDuration, string damageSource)
    {
        float cs = _grid.CellSize;
        Vector3 center = _grid.GetCellCenter(row, col);
        Vector3 size = new Vector3(cs, 0.01f, cs);

        var hazard = CreateFlatQuad(center, size, ANCHOR_COLOR, "AnchorPoint");
        _activeHazards.Add(hazard);

        // 等待預告時間（不閃爍，保持青色）
        float elapsed = 0f;
        while (elapsed < warnTime)
        {
            if (!GameManager.Instance.IsPlaying) { yield return null; continue; }
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 結算：玩家是否在格子內
        bool success = IsPlayerInCell(row, col);

        DestroyHazard(hazard);

        if (success)
        {
            // 成功：建立固定力場
            SpawnTetherField(center, cs * 0.5f, tetherDuration);
        }
        else
        {
            // 失敗：造成傷害
            DealDamageToPlayer(failDamage, damageSource);
        }
    }

    // ══════════════════════════════════════════════════════
    // 6. 固定力場（Tether Field）
    // ══════════════════════════════════════════════════════
    void SpawnTetherField(Vector3 center, float radius, float duration)
    {
        ClearTetherField();

        _tetherFieldCenter = center;
        _tetherFieldRadius = radius;
        _tetherFieldExpiry = Time.time + duration;
        _tetherFieldActive = true;

        // 建立視覺：旋轉的青色圓形
        _tetherFieldObj = CreateTetherFieldVisual(center, radius);
        _activeHazards.Add(_tetherFieldObj);
    }

    public void ClearTetherField()
    {
        _tetherFieldActive = false;
        if (_tetherFieldObj != null)
        {
            _activeHazards.Remove(_tetherFieldObj);
            Destroy(_tetherFieldObj);
            _tetherFieldObj = null;
        }
    }

    public bool IsPlayerInTetherField()
    {
        if (!_tetherFieldActive || _playerTransform == null) return false;
        float dist = Vector3.Distance(
            new Vector3(_playerTransform.position.x, 0f, _playerTransform.position.z),
            new Vector3(_tetherFieldCenter.x, 0f, _tetherFieldCenter.z));
        return dist <= _tetherFieldRadius;
    }

    // ══════════════════════════════════════════════════════
    // 7. 風暴推進（擊退）
    // ══════════════════════════════════════════════════════
    public void DoStormKnockback(float warnTime, int damage, float knockbackDist, string damageSource = "Storm Surge")
    {
        var c = StartCoroutine(StormKnockbackRoutine(warnTime, damage, knockbackDist, damageSource));
        _activeCoroutines.Add(c);
    }

    IEnumerator StormKnockbackRoutine(float warnTime, int damage, float knockbackDist, string damageSource)
    {
        // 建立風環視覺
        Vector3 knockbackOrigin = _bossTransform != null
            ? _bossTransform.position
            : Vector3.zero;
        var windRing = CreateWindRingFx(knockbackOrigin);
        _activeHazards.Add(windRing);

        float elapsed = 0f;
        while (elapsed < warnTime)
        {
            if (!GameManager.Instance.IsPlaying) { yield return null; continue; }
            elapsed += Time.deltaTime;
            // 風環向外擴張動畫
            float t = elapsed / warnTime;
            if (windRing != null)
                windRing.transform.localScale = Vector3.one * (1f + t * 3f);
            yield return null;
        }

        DestroyHazard(windRing);

        // 傷害
        DealDamageToPlayer(damage, damageSource);

        // 擊退
        float actualKnockback = CalculateKnockbackDistance(
            knockbackDist,
            IsPlayerInTetherField());

        ApplyKnockbackFromPoint(actualKnockback, knockbackOrigin);
    }

    /// <summary>
    /// 純計算入口，供擊退邏輯與單元測試共用。
    /// 固定力場內只承受原本 20% 的擊退距離。
    /// </summary>
    public static float CalculateKnockbackDistance(float baseDistance, bool anchored)
    {
        return anchored ? baseDistance * 0.2f : baseDistance;
    }

    // ══════════════════════════════════════════════════════
    // 8. 地脈連爆（逐排爆炸）
    // ══════════════════════════════════════════════════════
    public void DoRollingExplosion(bool northToSouth, float rowInterval, int damage, string damageSource = "Rolling Explosion")
    {
        var c = StartCoroutine(RollingExplosionRoutine(northToSouth, rowInterval, damage, damageSource));
        _activeCoroutines.Add(c);
    }

    IEnumerator RollingExplosionRoutine(bool northToSouth, float rowInterval, int damage, string damageSource)
    {
        int[] rowOrder = northToSouth
            ? new[] { 0, 1, 2, 3 }
            : new[] { 3, 2, 1, 0 };

        float cs = _grid.CellSize;
        float half = _grid.ArenaHalfSize;

        // 先顯示所有排的預告
        var rowHazards = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            int row = rowOrder[i];
            (Vector3 center, Vector3 size) bounds = _grid.GetRowBounds(row);
            var h = CreateFlatQuad(bounds.center, bounds.size, DANGER_PREVIEW, $"RollingRow{row}");
            rowHazards[i] = h;
            _activeHazards.Add(h);
        }

        // 逐排爆炸
        for (int i = 0; i < 4; i++)
        {
            int row = rowOrder[i];
            var h = rowHazards[i];

            // 等待間隔（第一排立即爆炸，後續排等待）
            if (i > 0)
            {
                float elapsed = 0f;
                while (elapsed < rowInterval)
                {
                    if (!GameManager.Instance.IsPlaying) { yield return null; continue; }
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            if (h == null) continue;

            // 爆炸閃光
            SetQuadColor(h, DANGER_EXPLODE);
            yield return new WaitForSeconds(0.15f);

            // 傷害判定
            if (IsPlayerInRow(row)) DealDamageToPlayer(damage, damageSource);

            // 爆炸後立即清除（變成安全區）
            DestroyHazard(h);
            rowHazards[i] = null;
        }
    }

    // ══════════════════════════════════════════════════════
    // 9. 雷環掃掠（旋轉扇形）
    // ══════════════════════════════════════════════════════
    public void DoSweepBeam(float startAngle, int sweepCount, float sweepInterval, int damage, string damageSource = "Thunder Sweep")
    {
        var c = StartCoroutine(SweepBeamRoutine(startAngle, sweepCount, sweepInterval, damage, damageSource));
        _activeCoroutines.Add(c);
    }

    IEnumerator SweepBeamRoutine(float startAngle, int sweepCount, float sweepInterval, int damage, string damageSource)
    {
        float sectorAngle = 90f;
        float arenaRadius = CalculateArenaCoveringRadius(_grid.ArenaHalfSize);
        float currentAngle = startAngle;

        for (int i = 0; i < sweepCount; i++)
        {
            // 建立扇形危險區
            var sector = CreateSectorHazard(Vector3.up * 0.05f, arenaRadius, currentAngle, sectorAngle);
            _activeHazards.Add(sector);

            bool playerHitThisSweep = false;
            float elapsed = 0f;

            while (elapsed < sweepInterval)
            {
                if (!GameManager.Instance.IsPlaying) { yield return null; continue; }
                elapsed += Time.deltaTime;

                // 傷害判定（每次掃掠只命中一次）
                if (!playerHitThisSweep && IsPlayerInSector(currentAngle, sectorAngle))
                {
                    playerHitThisSweep = true;
                    DealDamageToPlayer(damage, damageSource);
                }

                yield return null;
            }

            DestroyHazard(sector);

            // 旋轉 90 度
            currentAngle += 90f;
        }
    }

    // ══════════════════════════════════════════════════════
    // 10. 十字崩裂（一行 + 一列）
    // ══════════════════════════════════════════════════════
    public void DoCrossCleave(int row, int col, float warnTime, int damage, string damageSource = "Cross Cleave")
    {
        var c = StartCoroutine(CrossCleaveRoutine(row, col, warnTime, damage, damageSource));
        _activeCoroutines.Add(c);
    }

    IEnumerator CrossCleaveRoutine(int row, int col, float warnTime, int damage, string damageSource)
    {
        var rowBounds = _grid.GetRowBounds(row);
        var colBounds = _grid.GetColBounds(col);

        var hRow = CreateFlatQuad(rowBounds.center, rowBounds.size, DANGER_PREVIEW, "CrossRow");
        var hCol = CreateFlatQuad(colBounds.center, colBounds.size, DANGER_PREVIEW, "CrossCol");
        _activeHazards.Add(hRow);
        _activeHazards.Add(hCol);

        yield return FlashWarning(hRow, warnTime, 0.6f);
        SetQuadColor(hCol, DANGER_FLASH);
        yield return ExplodeFlash(hRow);
        SetQuadColor(hCol, DANGER_EXPLODE);
        yield return new WaitForSeconds(0.15f);

        // 傷害判定（在行或列中）
        bool inRow = IsPlayerInRow(row);
        bool inCol = IsPlayerInCol(col);
        if (inRow || inCol) DealDamageToPlayer(damage, damageSource);

        DestroyHazard(hRow);
        DestroyHazard(hCol);
    }

    // ══════════════════════════════════════════════════════
    // 11. 四角爆炸
    // ══════════════════════════════════════════════════════
    public void DoCornerExplosion(float warnTime, int damage, string damageSource = "Corner Blast")
    {
        var c = StartCoroutine(CornerExplosionRoutine(warnTime, damage, damageSource));
        _activeCoroutines.Add(c);
    }

    IEnumerator CornerExplosionRoutine(float warnTime, int damage, string damageSource)
    {
        // 四個角落：(0,0),(0,3),(3,0),(3,3)
        var corners = new[] { (0, 0), (0, 3), (3, 0), (3, 3) };
        float cs = _grid.CellSize;
        Vector3 cellSize = new Vector3(cs, 0.01f, cs);
        var hazards = new List<GameObject>();
        var dangerCells = new List<(int, int)>();

        foreach (var (r, c2) in corners)
        {
            Vector3 center = _grid.GetCellCenter(r, c2);
            var h = CreateFlatQuad(center, cellSize, DANGER_PREVIEW, "Corner");
            hazards.Add(h);
            _activeHazards.Add(h);
            dangerCells.Add((r, c2));
        }

        yield return FlashWarningMultiple(hazards, warnTime, 0.6f);

        foreach (var h in hazards) SetQuadColor(h, DANGER_EXPLODE);
        yield return new WaitForSeconds(0.15f);

        bool playerHit = false;
        foreach (var (r, c2) in dangerCells)
        {
            if (!playerHit && IsPlayerInCell(r, c2))
            {
                playerHit = true;
                DealDamageToPlayer(damage, damageSource);
            }
        }

        foreach (var h in hazards) DestroyHazard(h);
    }

    // ══════════════════════════════════════════════════════
    // 輔助：傷害與擊退
    // ══════════════════════════════════════════════════════
    void DealDamageToPlayer(int damage, string source)
    {
        if (_playerStats == null || _playerController == null) return;
        if (_playerController.IsDead) return;
        _playerStats.TakeDamage(damage, source);
        if (_playerStats.CurrentHp <= 0)
            _playerController.Die("HP reached zero");
    }

    void ApplyKnockback(float distance)
    {
        if (_playerTransform == null || _playerController == null) return;
        if (_playerController.IsDead) return;

        // 從玩家當前位置向外（遠離 Boss 方向）
        Vector3 dir = _playerTransform.position;
        dir.y = 0f;
        if (dir.magnitude < 0.1f) dir = Vector3.right;
        dir.Normalize();

        var cc = _playerTransform.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            _playerTransform.position += dir * distance;
            cc.enabled = true;
        }
    }

    void ApplyKnockbackFromPoint(float distance, Vector3 origin)
    {
        if (_playerTransform == null || _playerController == null) return;
        if (_playerController.IsDead) return;

        Vector3 dir = CalculateKnockbackDirection(_playerTransform.position, origin);

        var cc = _playerTransform.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            _playerTransform.position += dir * distance;
            cc.enabled = true;
        }
    }

    public static Vector3 CalculateKnockbackDirection(Vector3 playerPosition, Vector3 origin)
    {
        Vector3 dir = playerPosition - origin;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) dir = Vector3.right;
        return dir.normalized;
    }

    public static float CalculateArenaCoveringRadius(float arenaHalfSize)
    {
        return Mathf.Sqrt(2f) * arenaHalfSize + 0.5f;
    }

    // ══════════════════════════════════════════════════════
    // 輔助：位置判定
    // ══════════════════════════════════════════════════════
    bool IsPlayerInBounds(Vector3 center, Vector3 size)
    {
        if (_playerTransform == null) return false;
        Vector3 p = _playerTransform.position;
        return Mathf.Abs(p.x - center.x) <= size.x * 0.5f &&
               Mathf.Abs(p.z - center.z) <= size.z * 0.5f;
    }

    bool IsPlayerInCell(int row, int col)
    {
        if (_playerTransform == null) return false;
        (int pr, int pc) = _grid.WorldPositionToCell(_playerTransform.position);
        return pr == row && pc == col;
    }

    bool IsPlayerInRow(int row)
    {
        if (_playerTransform == null) return false;
        (int pr, int _) = _grid.WorldPositionToCell(_playerTransform.position);
        return pr == row;
    }

    bool IsPlayerInCol(int col)
    {
        if (_playerTransform == null) return false;
        (int _, int pc) = _grid.WorldPositionToCell(_playerTransform.position);
        return pc == col;
    }

    bool IsPlayerInSector(float centerAngleDeg, float sectorAngleDeg)
    {
        if (_playerTransform == null) return false;
        Vector3 toPlayer = _playerTransform.position - Vector3.zero;
        toPlayer.y = 0f;
        if (toPlayer.magnitude < 0.5f) return false;

        float playerAngle = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
        // 正規化到 0~360
        playerAngle = (playerAngle % 360f + 360f) % 360f;
        float center = (centerAngleDeg % 360f + 360f) % 360f;
        float half = sectorAngleDeg * 0.5f;

        float diff = Mathf.Abs(Mathf.DeltaAngle(playerAngle, center));
        return diff <= half;
    }

    // ══════════════════════════════════════════════════════
    // 輔助：視覺物件建立
    // ══════════════════════════════════════════════════════

    /// <summary>建立平面四邊形（危險區地板標記）</summary>
    GameObject CreateFlatQuad(Vector3 center, Vector3 size, Color color, string label)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Hazard_{label}";
        // Y 略高於地板，避免 Z-Fighting
        go.transform.position = new Vector3(center.x, 0.06f, center.z);
        go.transform.localScale = new Vector3(size.x * 0.98f, 0.03f, size.z * 0.98f);

        // 移除碰撞器
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        go.GetComponent<Renderer>().material = MakeTransparentMaterial(color);
        return go;
    }

    Material MakeTransparentMaterial(Color color)
    {
        // 嘗試 URP Unlit，失敗則退回 Sprites/Default
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        var mat = new Material(shader);

        // URP Unlit transparent
        if (shader.name.Contains("Universal Render Pipeline"))
        {
            mat.SetFloat("_Surface", 1f);   // Transparent
            mat.SetFloat("_Blend", 0f);     // Alpha
            mat.SetFloat("_ZWrite", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetShaderPassEnabled("ShadowCaster", false);
        }

        mat.color = color;
        return mat;
    }

    void SetQuadColor(GameObject go, Color color)
    {
        if (go == null) return;
        var r = go.GetComponent<Renderer>();
        if (r != null) r.material.color = color;
    }

    void DestroyHazard(GameObject go)
    {
        if (go == null) return;
        _activeHazards.Remove(go);
        Destroy(go);
    }

    /// <summary>閃爍警告動畫</summary>
    IEnumerator FlashWarning(GameObject hazard, float totalTime, float flashStartRatio)
    {
        float elapsed = 0f;
        float flashInterval = 0.3f;
        bool bright = false;

        while (elapsed < totalTime)
        {
            if (!GameManager.Instance.IsPlaying) { yield return null; continue; }
            float dt = Time.deltaTime;
            elapsed += dt;
            flashInterval -= dt;

            if (flashInterval <= 0f && elapsed > totalTime * flashStartRatio)
            {
                bright = !bright;
                flashInterval = 0.15f;
                SetQuadColor(hazard, bright ? DANGER_FLASH : DANGER_PREVIEW);
            }
            yield return null;
        }
    }

    IEnumerator FlashWarningMultiple(List<GameObject> hazards, float totalTime, float flashStartRatio)
    {
        float elapsed = 0f;
        float flashInterval = 0.3f;
        bool bright = false;

        while (elapsed < totalTime)
        {
            if (!GameManager.Instance.IsPlaying) { yield return null; continue; }
            float dt = Time.deltaTime;
            elapsed += dt;
            flashInterval -= dt;

            if (flashInterval <= 0f && elapsed > totalTime * flashStartRatio)
            {
                bright = !bright;
                flashInterval = 0.15f;
                Color fc = bright ? DANGER_FLASH : DANGER_PREVIEW;
                foreach (var h in hazards) SetQuadColor(h, fc);
            }
            yield return null;
        }
    }

    /// <summary>爆炸閃光</summary>
    IEnumerator ExplodeFlash(GameObject hazard)
    {
        SetQuadColor(hazard, DANGER_EXPLODE);
        yield return new WaitForSeconds(0.15f);
    }

    /// <summary>Boss 身上的擴散環粒子特效</summary>
    GameObject CreateBossRingFx(Transform bossTransform)
    {
        if (bossTransform == null) return null;

        var go = new GameObject("BossRingFx");
        go.transform.position = bossTransform.position;

        // 先取得 ParticleSystem（預設已建立），在 Play 前設定所有屬性
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 2f;
        main.loop = false;
        main.startLifetime = 1.5f;
        main.startSpeed = 8f;
        main.startSize = 0.5f;
        main.startColor = new Color(1f, 0.4f, 0.1f, 0.8f);
        main.maxParticles = 60;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        var burst = new ParticleSystem.Burst(0f, 60);
        emission.SetBursts(new[] { burst });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1f;

        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        ps.Play();
        return go;
    }

    /// <summary>風環特效（從中心向外擴張）</summary>
    GameObject CreateWindRingFx(Vector3 origin)
    {
        var go = new GameObject("WindRingFx");
        go.transform.position = new Vector3(origin.x, 0.1f, origin.z);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 4f;
        main.loop = true;
        main.startLifetime = 2f;
        main.startSpeed = 5f;
        main.startSize = 0.3f;
        main.startColor = new Color(0.5f, 0.9f, 1f, 0.6f);
        main.maxParticles = 100;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;

        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.material = new Material(Shader.Find("Particles/Standard Unlit"));

        ps.Play();
        return go;
    }

    /// <summary>固定力場視覺（旋轉青色圓形）</summary>
    GameObject CreateTetherFieldVisual(Vector3 center, float radius)
    {
        var go = new GameObject("TetherField");
        go.transform.position = center;

        // 抬高成一塊可辨識的平台，不只改變地板顏色。
        var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = "TetherVisual";
        cyl.transform.SetParent(go.transform);
        cyl.transform.localPosition = new Vector3(0f, 0.18f, 0f);
        cyl.transform.localScale = new Vector3(radius * 2f, 0.18f, radius * 2f);

        var col = cyl.GetComponent<Collider>();
        if (col != null) DestroyRuntimeOrImmediate(col);

        cyl.GetComponent<Renderer>().material = MakeTransparentMaterial(TETHER_COLOR);

        var beacon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beacon.name = "TetherBeacon";
        beacon.transform.SetParent(go.transform);
        beacon.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        beacon.transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);
        var beaconCollider = beacon.GetComponent<Collider>();
        if (beaconCollider != null) DestroyRuntimeOrImmediate(beaconCollider);
        beacon.GetComponent<Renderer>().material = MakeTransparentMaterial(ANCHOR_SAFE_COLOR);

        // 加入旋轉動畫腳本
        var rotator = go.AddComponent<TetherFieldRotator>();
        rotator.Init(cyl.transform);

        return go;
    }

    static void DestroyRuntimeOrImmediate(UnityEngine.Object obj)
    {
        if (obj == null) return;

        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    /// <summary>建立由中心延伸至場外的完整扇形 Mesh。</summary>
    GameObject CreateSectorHazard(Vector3 center, float radius, float centerAngleDeg, float sectorAngleDeg)
    {
        var root = new GameObject("SectorHazard");
        root.transform.position = center;

        int segments = Mathf.Max(12, Mathf.CeilToInt(sectorAngleDeg / 5f));
        float halfAngle = sectorAngleDeg * 0.5f;
        float startAngle = centerAngleDeg - halfAngle;

        var vertices = new Vector3[segments + 2];
        var triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (startAngle + sectorAngleDeg * i / segments) * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(
                Mathf.Sin(angle) * radius,
                0f,
                Mathf.Cos(angle) * radius);

            if (i < segments)
            {
                int triangle = i * 3;
                triangles[triangle] = 0;
                triangles[triangle + 1] = i + 1;
                triangles[triangle + 2] = i + 2;
            }
        }

        var mesh = new Mesh { name = "SectorHazardMesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        root.AddComponent<MeshFilter>().sharedMesh = mesh;
        root.AddComponent<MeshRenderer>().material = MakeTransparentMaterial(SWEEP_COLOR);

        return root;
    }
}

/// <summary>固定力場旋轉動畫</summary>
public class TetherFieldRotator : MonoBehaviour
{
    Transform _visual;
    Vector3 _baseScale;
    float _pulseTimer;

    public void Init(Transform visual)
    {
        _visual = visual;
        _baseScale = visual != null ? visual.localScale : Vector3.one;
    }

    void Update()
    {
        if (_visual == null) return;
        transform.Rotate(0f, 45f * Time.deltaTime, 0f);
        _pulseTimer += Time.deltaTime;
        float scale = 1f + Mathf.Sin(_pulseTimer * 3f) * 0.05f;
        _visual.localScale = _baseScale * scale;
    }
}
