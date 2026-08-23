using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 玩家血量。
/// </summary>
public class PlayerStats : MonoBehaviour
{
    public int MaxHp = 100;
    public int CurrentHp { get; private set; }
    public event Action OnDied;
    public event Action OnHpChanged;
    public event Action<DamageTakenRecord> OnDamageTaken;

    readonly List<DamageTakenRecord> _damageHistory = new();
    public IReadOnlyList<DamageTakenRecord> DamageHistory => _damageHistory;

    void Awake() => CurrentHp = MaxHp;

    public void TakeDamage(int dmg)
    {
        TakeDamage(dmg, "Unknown");
    }

    public void TakeDamage(int dmg, string source, float battleTime = -1f)
    {
        if (CurrentHp <= 0 || dmg <= 0) return;

        int previousHp = CurrentHp;
        CurrentHp = Mathf.Max(0, CurrentHp - dmg);
        int actualDamage = previousHp - CurrentHp;

        if (battleTime < 0f)
        {
            var timer = FindAnyObjectByType<BattleTimer>();
            battleTime = timer != null ? timer.ElapsedSeconds : 0f;
        }

        var damageRecord = new DamageTakenRecord(battleTime, source, actualDamage);
        _damageHistory.Add(damageRecord);
        OnDamageTaken?.Invoke(damageRecord);
        OnHpChanged?.Invoke();
        if (CurrentHp <= 0) OnDied?.Invoke();
    }

    public void Heal(int amount)
    {
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        OnHpChanged?.Invoke();
    }

    public void FullReset()
    {
        CurrentHp = MaxHp;
        _damageHistory.Clear();
        OnHpChanged?.Invoke();
    }
}
