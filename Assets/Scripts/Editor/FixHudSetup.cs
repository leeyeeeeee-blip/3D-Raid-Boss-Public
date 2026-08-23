using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// 修復 HUD 設定的 Editor 工具。
/// 已更新：技能1疊層改為5個圓圈，移除舊的 skill1StackBg/skill1StackText 引用。
/// </summary>
public class FixHudSetup
{
    [MenuItem("FFXIV/Fix HUD Setup")]
    public static void Fix()
    {
        var hudGo = GameObject.Find("HUD");
        if (hudGo == null) { Debug.LogError("[FixHudSetup] HUD not found!"); return; }

        var hud = hudGo.GetComponent<HudManager>();
        if (hud == null) { Debug.LogError("[FixHudSetup] HudManager not found!"); return; }

        var playerHpRoot = hudGo.transform.Find("PlayerHpRoot");
        if (playerHpRoot == null) { Debug.LogError("[FixHudSetup] PlayerHpRoot not found!"); return; }

        // ── 1. 確認技能1疊層圓圈（Skill1StackDots）已存在 ──
        var dotsTf = playerHpRoot.Find("Skill1StackDots");
        if (dotsTf != null)
        {
            int dotCount = dotsTf.childCount;
            hud.skill1StackDots = new Image[dotCount];
            for (int i = 0; i < dotCount; i++)
                hud.skill1StackDots[i] = dotsTf.GetChild(i).GetComponent<Image>();
            Debug.Log($"[FixHudSetup] Skill1StackDots connected: {dotCount} dots.");
        }
        else
        {
            Debug.LogWarning("[FixHudSetup] Skill1StackDots not found. Please run 'Build HUD' to rebuild.");
        }

        var chargesTf = playerHpRoot.Find("Skill3ProcCharges");
        if (chargesTf != null)
        {
            hud.skill3ProcChargeBars = new Image[chargesTf.childCount];
            for (int i = 0; i < chargesTf.childCount; i++)
                hud.skill3ProcChargeBars[i] = chargesTf.GetChild(i).GetComponent<Image>();
        }

        var castBarTf = playerHpRoot.Find("Skill1CastBar");
        if (castBarTf != null)
        {
            hud.skill1CastBarRoot = castBarTf.gameObject;
            hud.skill1CastFill = castBarTf.Find("Fill")?.GetComponent<Image>();
        }

        hud.gcdIndicator = playerHpRoot
            .Find("GcdIndicatorBackground/GcdIndicator")
            ?.GetComponent<Image>();

        // ── 2. 確認 Skill2Stack 灰底連接 ──────────────────
        var skill2StackTf = playerHpRoot.Find("Skill2Stack");
        if (skill2StackTf != null)
        {
            var bgTf = skill2StackTf.Find("Bg");
            if (bgTf != null)
            {
                hud.skill2StackBg = bgTf.GetComponent<Image>();
                Debug.Log("[FixHudSetup] Skill2Stack Bg connected.");
            }
        }

        // ── 3. 確認技能2 UnavailMask 透明度正確 ──────────
        var skill2SlotTf = hudGo.transform.Find("SkillBar/Skill2Slot");
        if (skill2SlotTf != null)
        {
            var unavailTf = skill2SlotTf.Find("UnavailMask");
            if (unavailTf != null)
            {
                var img = unavailTf.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
                    Debug.Log("[FixHudSetup] Skill2 UnavailMask color fixed.");
                }
            }
        }

        // ── 4. 確認技能2 Glow 連接 ────────────────────────
        var skill2Slot = skill2SlotTf?.GetComponent<SkillSlotUI>();
        if (skill2Slot != null)
        {
            Debug.Log($"[FixHudSetup] Skill2Slot glowImage: {(skill2Slot.glowImage != null ? skill2Slot.glowImage.name : "NULL")}");
        }

        // ── 5. 確認 HudManager skillSlots 連接正確 ────────
        var skillBarTf = hudGo.transform.Find("SkillBar");
        if (skillBarTf != null && hud.skillSlots != null)
        {
            Debug.Log($"[FixHudSetup] skillSlots count: {hud.skillSlots.Length}");
            for (int i = 0; i < hud.skillSlots.Length; i++)
                Debug.Log($"  Slot[{i}]: {(hud.skillSlots[i] != null ? hud.skillSlots[i].name : "NULL")}");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[FixHudSetup] Done.");
    }
}
#endif
