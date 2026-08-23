using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 單個技能格 UI。
/// - 不可用：灰色50%遮罩
/// - GCD轉動：只顯示倒數數字，不顯示遮罩
/// - 可用（技能2）：橘紅色圖示 + 黃色邊框發亮
/// - 按下：黑色邊框閃爍
/// - 技能4：粉色
/// - 技能3 proc：黃色邊框持續發亮
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    [Header("元件")]
    public Image iconImage;
    public Image cooldownMask;
    public Image unavailableMask;   // 不可用時的灰色遮罩（純色，非 Filled）
    public Image borderFlash;       // 按下時黑色邊框
    public TextMeshProUGUI abbrevText;
    public TextMeshProUGUI cdText;
    public Image glowImage;

    [Header("技能類型")]
    public bool isSkill2;   // 技能2：可用時橘紅 + 邊框發亮
    public bool isSkill4;   // 技能4：粉色

    // 顏色常數
    static readonly Color SKILL2_READY   = new Color(1f, 0.35f, 0.1f, 1f);   // 橘紅
    static readonly Color SKILL4_COLOR   = new Color(1f, 0.55f, 0.75f, 1f);  // 粉色
    static readonly Color UNAVAIL_MASK   = new Color(0.2f, 0.2f, 0.2f, 0.5f);// 灰50%
    static readonly Color GLOW_YELLOW    = new Color(1f, 0.85f, 0.2f, 1f);
    static readonly Color GLOW_ORANGE    = new Color(1f, 0.5f, 0f, 1f);      // 技能2可用時橘色邊框
    static readonly Color BURST_ORANGE   = new Color(1f, 0.5f, 0f, 1f);
    static readonly Color BORDER_FLASH   = new Color(0f, 0f, 0f, 1f);

    bool _procGlow;         // 技能3 proc 待施放發光
    bool _skill2Ready;      // 技能2可用狀態

    void Awake()
    {
        // 初始化圖示顏色
        if (iconImage != null)
            iconImage.color = isSkill4 ? SKILL4_COLOR : Color.white;
    }

    /// <summary>
    /// 一般技能刷新（技能1/2/4）
    /// </summary>
    /// <param name="ready">是否可施放</param>
    /// <param name="cdRemaining">剩餘冷卻/GCD</param>
    /// <param name="cdTotal">總冷卻/GCD 時長</param>
    /// <param name="casting">是否讀條中</param>
    /// <param name="castProgress">讀條進度 0~1</param>
    /// <param name="burstActive">技能4爆發中</param>
    public void Refresh(bool ready, float cdRemaining, float cdTotal,
        bool casting = false, float castProgress = 0f, bool burstActive = false)
    {
        bool onGcd = !ready && !casting && cdRemaining > 0f && cdTotal <= 1.5f;
        // onGcd：GCD 轉動（短冷卻）；否則為技能本身冷卻

        // ── 不可用遮罩（灰色50%）──────────────────────────
        if (unavailableMask != null)
        {
            // 技能2：不可用且不在GCD時顯示灰色遮罩
            bool showUnavail = isSkill2 && !ready && !onGcd;
            unavailableMask.color = UNAVAIL_MASK;
            unavailableMask.gameObject.SetActive(showUnavail);
        }

        // ── 讀條 / 技能自身冷卻 Radial 遮罩 ───────────────
        if (cooldownMask != null)
        {
            // GCD 期間只顯示數字，不顯示 Radial 遮罩
            bool showMask = !onGcd && (!ready || casting);
            cooldownMask.gameObject.SetActive(showMask);
            if (showMask)
            {
                cooldownMask.color = new Color(0f, 0f, 0f, 0.7f);
                if (casting)
                    cooldownMask.fillAmount = 1f - castProgress;
                else if (cdTotal > 0f)
                    cooldownMask.fillAmount = cdRemaining / cdTotal;
            }
        }

        // ── 圖示顏色 ──────────────────────────────────────
        // GCD 期間保持原有圖示顏色，畫面上只增加倒數數字
        if (iconImage != null && !onGcd)
        {
            if (isSkill4)
                iconImage.color = SKILL4_COLOR;
            else if (isSkill2 && ready)
                iconImage.color = SKILL2_READY;
            else if (!ready)
                iconImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            else
                iconImage.color = Color.white;
        }

        // ── 冷卻秒數文字 ──────────────────────────────────
        if (cdText != null)
        {
            if (casting)
                cdText.text = $"{(1.5f * (1f - castProgress)):F1}";
            else if (!ready && cdRemaining > 0.1f)
                cdText.text = onGcd ? $"{cdRemaining:F1}" : $"{cdRemaining:F0}";
            else
                cdText.text = "";
        }

        // ── 技能2可用時邊框發亮（橘色）────────────────────
        if (isSkill2)
        {
            bool wasReady = _skill2Ready;
            _skill2Ready = ready;
            if (glowImage != null && !_procGlow)
            {
                if (ready)
                    glowImage.color = burstActive ? BURST_ORANGE : GLOW_ORANGE;
                else
                    glowImage.color = Color.clear;
            }
        }

        // ── 爆發中邊框（非技能2、非proc發光時）────────────
        if (!isSkill2 && glowImage != null && !_procGlow)
            glowImage.color = burstActive ? BURST_ORANGE : Color.clear;
    }

    /// <summary>
    /// 按下技能時呼叫：黑色邊框閃爍
    /// </summary>
    public void TriggerPressFlash()
    {
        StopCoroutine(nameof(PressFlashCoroutine));
        StartCoroutine(nameof(PressFlashCoroutine));
    }

    IEnumerator PressFlashCoroutine()
    {
        if (borderFlash == null) yield break;
        borderFlash.color = BORDER_FLASH;
        borderFlash.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.12f);
        borderFlash.gameObject.SetActive(false);
    }

    // ── 技能3 proc 發光 ───────────────────────────────────
    /// <summary>
    /// 設置技能3 proc 待施放發光狀態
    /// </summary>
    public void SetProcGlow(bool active)
    {
        _procGlow = active;
        if (glowImage != null)
            glowImage.color = active ? GLOW_YELLOW : Color.clear;
    }

    /// <summary>
    /// 技能3 proc 觸發時閃爍動畫
    /// </summary>
    public void TriggerProcFlash()
    {
        StopCoroutine(nameof(FlashCoroutine));
        StartCoroutine(nameof(FlashCoroutine));
    }

    IEnumerator FlashCoroutine()
    {
        if (glowImage == null) yield break;
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            float alpha = Mathf.PingPong(t * 8f, 1f);
            glowImage.color = new Color(GLOW_YELLOW.r, GLOW_YELLOW.g, GLOW_YELLOW.b, alpha);
            yield return null;
        }
        // 閃爍結束後保持 proc 發光狀態
        glowImage.color = _procGlow ? GLOW_YELLOW : Color.clear;
    }
}
