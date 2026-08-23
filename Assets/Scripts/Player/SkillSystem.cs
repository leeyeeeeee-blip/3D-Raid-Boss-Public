using UnityEngine;
using System;

/// <summary>
/// 技能系統：GCD、技能1-4、層數、保底、傷害計算。
/// 所有狀態集中在此，HUD 直接讀取屬性。
/// </summary>
public class SkillSystem : MonoBehaviour
{
    // ── 事件（HUD 訂閱）──────────────────────────────────
    public event Action<int, float> OnDamageDealt;          // (damage, worldY) 傷害跳字
    public event Action OnSkill3Proc;                        // 技能3觸發發光（有待施放的proc）
    public event Action OnSkill4Activated;
    public event Action OnSkill4Expired;
    public event Action OnStateChanged;                      // 任何狀態變化通知 HUD 刷新

    // ── GCD ──────────────────────────────────────────────
    public float GcdDuration => _skill4Active ? 0.5f : 1f;
    public float GcdRemaining { get; private set; }
    public bool GcdReady => GcdRemaining <= 0f;

    // ── 技能1 ─────────────────────────────────────────────
    public bool Skill1Casting { get; private set; }
    public float Skill1CastProgress { get; private set; }   // 0~1
    const float CAST1_TIME = 1.5f;
    float _cast1Timer;

    // ── 技能2 ─────────────────────────────────────────────
    public int Skill1Stack { get; private set; }            // 0-5（技能1累積次數，上限5）
    public const int SKILL1_STACK_MAX = 5;
    public bool Skill2Ready => Skill1Stack >= SKILL2_CONSUME;

    // ── 技能3 ─────────────────────────────────────────────
    public int Skill3Stacks { get; private set; }           // 0-3（傷害加成層數，由技能2消耗）
    public const int SKILL3_PROC_CHARGES_MAX = 3;
    public int Skill3ProcCharges { get; private set; }      // 技能3可施放次數，最多儲存3次
    public bool Skill3ProcReady => Skill3ProcCharges > 0;
    int _skill3MissCount;                                   // 連續未觸發次數
    int Skill3GuaranteeThreshold => _skill4Active ? 3 : 5;
    const float SKILL3_PROC_RATE_NORMAL = 0.20f;
    const float SKILL3_PROC_RATE_BURST  = 0.50f;
    float Skill3ProcRate => _skill4Active ? SKILL3_PROC_RATE_BURST : SKILL3_PROC_RATE_NORMAL;

    // ── 技能4 ─────────────────────────────────────────────
    public bool Skill4Active => _skill4Active;
    public float Skill4Remaining { get; private set; }
    public float Skill4Cooldown { get; private set; }
    public bool Skill4Ready => Skill4Cooldown <= 0f && !_skill4Active;
    bool _skill4Active;
    const float SKILL4_DURATION = 20f;
    const float SKILL4_COOLDOWN = 60f;

    // ── 傷害基礎值 ────────────────────────────────────────
    const int DMG_SKILL1 = 10;
    const int DMG_SKILL2 = 20;
    const int DMG_SKILL3 = 15;
    const float SKILL4_DMG_BONUS = 0.20f;
    const float SKILL3_STACK_BONUS = 0.10f; // 每層 10%，只加在技能2

    // ── Boss 參考 ─────────────────────────────────────────
    Transform _bossTransform;

    // ── DPS 統計（供 DpsTracker 讀取）────────────────────
    public int TotalDamage { get; private set; }
    public int Skill1UseCount { get; private set; }
    public int Skill2UseCount { get; private set; }
    public int Skill3UseCount { get; private set; }
    public int Skill4UseCount { get; private set; }
    public int Skill1TotalDmg { get; private set; }
    public int Skill2TotalDmg { get; private set; }
    public int Skill3TotalDmg { get; private set; }

    void Start()
    {
        var boss = GameObject.Find("Boss");
        if (boss != null) _bossTransform = boss.transform;
    }

    void Update()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;

        TickGcd();
        TickCast1();
        TickSkill4();
        HandleInput();
    }

    // ── Tick ──────────────────────────────────────────────
    void TickGcd()
    {
        if (GcdRemaining > 0f)
        {
            GcdRemaining -= Time.deltaTime;
            if (GcdRemaining < 0f) GcdRemaining = 0f;
            OnStateChanged?.Invoke();
        }
    }

    void TickCast1()
    {
        if (!Skill1Casting) return;
        _cast1Timer += Time.deltaTime;
        Skill1CastProgress = Mathf.Clamp01(_cast1Timer / CAST1_TIME);
        OnStateChanged?.Invoke();

        if (_cast1Timer >= CAST1_TIME)
            FinishCast1();
    }

    void TickSkill4()
    {
        if (_skill4Active)
        {
            Skill4Remaining -= Time.deltaTime;
            if (Skill4Remaining <= 0f) DeactivateSkill4();
            else OnStateChanged?.Invoke();
        }
        if (Skill4Cooldown > 0f)
        {
            Skill4Cooldown -= Time.deltaTime;
            if (Skill4Cooldown < 0f) Skill4Cooldown = 0f;
            OnStateChanged?.Invoke();
        }
    }

    // ── Input ─────────────────────────────────────────────
    HudManager _hud;

    void HandleInput()
    {
        if (_hud == null) _hud = FindFirstObjectByType<HudManager>();

        if (Input.GetKeyDown(KeyBindings.Skill1)) { TrySkill1(); _hud?.TriggerSkillPress(0); }
        if (Input.GetKeyDown(KeyBindings.Skill2)) { TrySkill2(); _hud?.TriggerSkillPress(1); }
        if (Input.GetKeyDown(KeyBindings.Skill3)) { TrySkill3(); _hud?.TriggerSkillPress(2); }
        if (Input.GetKeyDown(KeyBindings.Skill4)) { TrySkill4(); _hud?.TriggerSkillPress(3); }
    }

    // ── 技能1 ─────────────────────────────────────────────
    void TrySkill1()
    {
        if (!GcdReady || Skill1Casting) return;

        if (_skill4Active)
        {
            // 爆發期間：瞬發，GCD 在此設定
            GcdRemaining = GcdDuration;
            ExecuteSkill1Instant();
        }
        else
        {
            // 開始讀條，GCD 從按下按鍵時開始計算（1.5秒）
            Skill1Casting = true;
            _cast1Timer = 0f;
            Skill1CastProgress = 0f;
            GcdRemaining = GcdDuration; // GCD 從按下時開始，不是讀條完成後
            OnStateChanged?.Invoke();
        }
    }

    void FinishCast1()
    {
        Skill1Casting = false;
        _cast1Timer = 0f;
        Skill1CastProgress = 0f;
        ExecuteSkill1Instant();
    }

    void ExecuteSkill1Instant()
    {
        int dmg = CalcDmg(DMG_SKILL1, false);
        DealDamage(dmg);
        Skill1UseCount++;
        Skill1TotalDmg += dmg;

        // 技能4期間每次技能1給予3層；一般狀態給予1層，上限皆為5層
        int stacksGained = _skill4Active ? SKILL2_CONSUME : 1;
        Skill1Stack = Mathf.Min(Skill1Stack + stacksGained, SKILL1_STACK_MAX);
        OnStateChanged?.Invoke();

        // 技能3觸發判定（只設置 proc 狀態，不自動施放）
        TryProcSkill3();
    }

    // ── 技能2 ─────────────────────────────────────────────
    // 技能2每次最多消耗3層 Skill1Stack，多餘的保留
    const int SKILL2_CONSUME = 3;

    void TrySkill2()
    {
        if (!GcdReady || !Skill2Ready) return;

        GcdRemaining = GcdDuration;

        // 無論技能4是否生效，技能2都固定消耗3層，多餘層數保留
        Skill1Stack -= SKILL2_CONSUME;

        // 傷害：基礎 + 技能4加成 + 技能3層數加成（消耗 Skill3Stacks）
        float skill3Bonus = Skill3Stacks * SKILL3_STACK_BONUS;
        int dmg = CalcDmg(DMG_SKILL2, true, skill3Bonus);
        DealDamage(dmg);
        Skill2UseCount++;
        Skill2TotalDmg += dmg;

        // 消耗技能3的層數
        Skill3Stacks = 0;

        OnStateChanged?.Invoke();
        TryProcSkill3();
    }

    // ── 技能3（玩家手動施放）─────────────────────────────
    void TrySkill3()
    {
        // 只有在有 proc 待施放時才能手動施放
        if (!Skill3ProcReady) return;

        Skill3ProcCharges--;
        Skill3Stacks = Mathf.Min(Skill3Stacks + 1, 3);
        Skill3UseCount++;

        // 技能3本身也造成傷害
        int dmg = CalcDmg(DMG_SKILL3, false);
        DealDamage(dmg);
        Skill3TotalDmg += dmg;

        OnStateChanged?.Invoke();
    }

    // ── 技能3 觸發判定（只設置 proc 狀態）───────────────
    void TryProcSkill3()
    {
        bool proc = UnityEngine.Random.value < Skill3ProcRate;

        if (!proc)
        {
            _skill3MissCount++;
            if (_skill3MissCount >= Skill3GuaranteeThreshold)
            {
                proc = true; // 保底觸發
            }
        }

        if (proc)
        {
            _skill3MissCount = 0;
            if (Skill3ProcCharges < SKILL3_PROC_CHARGES_MAX)
            {
                Skill3ProcCharges++;
                OnSkill3Proc?.Invoke();
                OnStateChanged?.Invoke();
            }
        }
    }

    // ── 技能4 ─────────────────────────────────────────────
    void TrySkill4()
    {
        if (!Skill4Ready) return;

        _skill4Active = true;
        Skill4Remaining = SKILL4_DURATION;
        Skill4Cooldown = SKILL4_COOLDOWN;
        Skill4UseCount++;

        // 啟動時技能2累積計數立即重置
        Skill1Stack = 0;

        OnSkill4Activated?.Invoke();
        OnStateChanged?.Invoke();
        Debug.Log("[SkillSystem] Skill4 Burst activated!");
    }

    void DeactivateSkill4()
    {
        _skill4Active = false;
        Skill4Remaining = 0f;
        OnSkill4Expired?.Invoke();
        OnStateChanged?.Invoke();
        Debug.Log("[SkillSystem] Skill4 Burst ended.");
    }

    // ── 傷害計算 ──────────────────────────────────────────
    int CalcDmg(int baseDmg, bool applySkill3Bonus, float extraBonus = 0f)
    {
        float mult = 1f;
        if (_skill4Active) mult += SKILL4_DMG_BONUS;
        if (applySkill3Bonus) mult += extraBonus;
        return Mathf.RoundToInt(baseDmg * mult);
    }

    void DealDamage(int dmg)
    {
        TotalDamage += dmg;
        float bossY = _bossTransform != null ? _bossTransform.position.y + 3f : 5f;
        OnDamageDealt?.Invoke(dmg, bossY);
    }

    // ── 公開工具（測試選單用）────────────────────────────
    public void ResetCooldowns()
    {
        GcdRemaining = 0f;
        Skill4Cooldown = 0f;
        OnStateChanged?.Invoke();
    }

    public void ResetDpsStats()
    {
        TotalDamage = 0;
        Skill1UseCount = Skill2UseCount = Skill3UseCount = Skill4UseCount = 0;
        Skill1TotalDmg = Skill2TotalDmg = Skill3TotalDmg = 0;
        OnStateChanged?.Invoke();
    }

    public void FullReset()
    {
        GcdRemaining = 0f;
        Skill1Casting = false;
        _cast1Timer = 0f;
        Skill1Stack = 0;
        Skill3Stacks = 0;
        Skill3ProcCharges = 0;
        _skill3MissCount = 0;
        _skill4Active = false;
        Skill4Remaining = 0f;
        Skill4Cooldown = 0f;
        ResetDpsStats();
        OnStateChanged?.Invoke();
    }
}
