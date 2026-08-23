using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;

public class HudBuilder
{
    [MenuItem("FFXIV/Build HUD")]
    public static void BuildHud()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        var oldHud = GameObject.Find("HUD");
        if (oldHud != null) Object.DestroyImmediate(oldHud);

        // 主 Canvas
        var hudGo = new GameObject("HUD");
        var canvas = hudGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = hudGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        hudGo.AddComponent<GraphicRaycaster>();

        var hud = hudGo.AddComponent<HudManager>();
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (circleSprite == null)
            circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Sprite uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        // ── Boss 血條（上方中央）──────────────────────────
        var bossHpPanel = CreatePanel(hudGo.transform, "BossHpPanel",
            new Vector2(0.5f, 1f), new Vector2(0, -10), new Vector2(600, 40));
        hud.bossHpSlider = CreateSlider(bossHpPanel.transform, "BossHpSlider",
            new Color(0.6f, 0.1f, 0.1f), new Color(0.9f, 0.2f, 0.2f));
        hud.bossHpText = CreateText(bossHpPanel.transform, "BossHpText", "Boss HP", 14, TextAlignmentOptions.Center);
        StretchFill(hud.bossHpText.rectTransform);

        var bossName = CreateText(hudGo.transform, "BossName", "Trial Boss", 16, TextAlignmentOptions.Center);
        SetAnchored(bossName.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -55), new Vector2(400, 30));

        // ── 玩家血條（跟隨玩家頭上，Screen Space）────────
        var playerHpRoot = new GameObject("PlayerHpRoot");
        playerHpRoot.transform.SetParent(hudGo.transform, false);
        var playerHpRootRt = playerHpRoot.AddComponent<RectTransform>();
        playerHpRootRt.sizeDelta = new Vector2(200, 22);
        playerHpRootRt.anchorMin = playerHpRootRt.anchorMax = new Vector2(0.5f, 0.5f);
        hud.playerHpRoot = playerHpRootRt;

        hud.playerHpSlider = CreateSlider(playerHpRoot.transform, "PlayerHpSlider",
            new Color(0.1f, 0.4f, 0.1f), new Color(0.2f, 0.8f, 0.2f));
        hud.playerHpText = CreateText(playerHpRoot.transform, "PlayerHpText", "100/100", 11, TextAlignmentOptions.Center);
        StretchFill(hud.playerHpText.rectTransform);
        hud.playerHpText.color = Color.black;

        // 技能1讀條：位於血條上方，由左向右顯示施法進度。
        var castBarRoot = new GameObject("Skill1CastBar");
        castBarRoot.transform.SetParent(playerHpRoot.transform, false);
        var castBarRt = castBarRoot.AddComponent<RectTransform>();
        SetAnchored(castBarRt, new Vector2(0.5f, 1f), new Vector2(15f, 18f), new Vector2(170f, 10f));
        castBarRoot.AddComponent<Image>().color = new Color(0.05f, 0.05f, 0.07f, 0.9f);
        hud.skill1CastBarRoot = castBarRoot;

        var castFillGo = new GameObject("Fill");
        castFillGo.transform.SetParent(castBarRoot.transform, false);
        var castFillRt = castFillGo.AddComponent<RectTransform>();
        StretchFill(castFillRt, 1f);
        hud.skill1CastFill = castFillGo.AddComponent<Image>();
        hud.skill1CastFill.sprite = uiSprite;
        hud.skill1CastFill.color = new Color(0.25f, 0.72f, 1f, 1f);
        hud.skill1CastFill.type = Image.Type.Filled;
        hud.skill1CastFill.fillMethod = Image.FillMethod.Horizontal;
        hud.skill1CastFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        hud.skill1CastFill.fillAmount = 0f;
        castBarRoot.SetActive(false);

        // GCD 指示器：讀條左側的圓形倒數，GCD 期間由滿逐漸歸零。
        var gcdBgGo = new GameObject("GcdIndicatorBackground");
        gcdBgGo.transform.SetParent(playerHpRoot.transform, false);
        var gcdBgRt = gcdBgGo.AddComponent<RectTransform>();
        SetAnchored(gcdBgRt, new Vector2(0.5f, 1f), new Vector2(-83f, 20f), new Vector2(18f, 18f));
        var gcdBg = gcdBgGo.AddComponent<Image>();
        gcdBg.sprite = circleSprite;
        gcdBg.preserveAspect = true;
        gcdBg.color = new Color(0.12f, 0.12f, 0.15f, 0.9f);

        var gcdFillGo = new GameObject("GcdIndicator");
        gcdFillGo.transform.SetParent(gcdBgGo.transform, false);
        var gcdFillRt = gcdFillGo.AddComponent<RectTransform>();
        StretchFill(gcdFillRt, 1f);
        hud.gcdIndicator = gcdFillGo.AddComponent<Image>();
        hud.gcdIndicator.sprite = circleSprite;
        hud.gcdIndicator.preserveAspect = true;
        hud.gcdIndicator.color = new Color(1f, 0.58f, 0.12f, 1f);
        hud.gcdIndicator.type = Image.Type.Filled;
        hud.gcdIndicator.fillMethod = Image.FillMethod.Radial360;
        hud.gcdIndicator.fillOrigin = (int)Image.Origin360.Top;
        hud.gcdIndicator.fillClockwise = false;
        hud.gcdIndicator.fillAmount = 0f;

        // ── 技能1疊層：血條正下方，5個圓圈（橘紅填充）──
        {
            // 容器：置中於血條下方
            const int DOT_COUNT = 5;
            const float DOT_SIZE = 14f;
            const float DOT_GAP  = 4f;
            float totalW = DOT_COUNT * DOT_SIZE + (DOT_COUNT - 1) * DOT_GAP;

            var dotsRoot = new GameObject("Skill1StackDots");
            dotsRoot.transform.SetParent(playerHpRoot.transform, false);
            var dotsRootRt = dotsRoot.AddComponent<RectTransform>();
            SetAnchored(dotsRootRt, new Vector2(0.5f, 0f), new Vector2(0, -20), new Vector2(totalW, DOT_SIZE));

            hud.skill1StackDots = new Image[DOT_COUNT];
            for (int i = 0; i < DOT_COUNT; i++)
            {
                var dotGo = new GameObject($"Dot{i + 1}");
                dotGo.transform.SetParent(dotsRoot.transform, false);
                var dotRt = dotGo.AddComponent<RectTransform>();
                // 從左至右排列
                float xPos = -totalW * 0.5f + DOT_SIZE * 0.5f + i * (DOT_SIZE + DOT_GAP);
                dotRt.anchorMin = dotRt.anchorMax = new Vector2(0.5f, 0.5f);
                dotRt.anchoredPosition = new Vector2(xPos, 0);
                dotRt.sizeDelta = new Vector2(DOT_SIZE, DOT_SIZE);

                var img = dotGo.AddComponent<Image>();
                img.sprite = circleSprite;
                img.preserveAspect = true;
                img.color = new Color(0.25f, 0.25f, 0.25f, 0.7f); // 初始空圓（暗灰）
                hud.skill1StackDots[i] = img;
            }
        }

        // 技能3觸發儲存：三個長方形，總寬80，小於技能1圓點指示器的86。
        {
            const int CHARGE_COUNT = 3;
            const float BAR_WIDTH = 24f;
            const float BAR_HEIGHT = 6f;
            const float BAR_GAP = 4f;
            float totalW = CHARGE_COUNT * BAR_WIDTH + (CHARGE_COUNT - 1) * BAR_GAP;

            var chargeRoot = new GameObject("Skill3ProcCharges");
            chargeRoot.transform.SetParent(playerHpRoot.transform, false);
            var chargeRootRt = chargeRoot.AddComponent<RectTransform>();
            SetAnchored(chargeRootRt, new Vector2(0.5f, 0f), new Vector2(0f, -33f), new Vector2(totalW, BAR_HEIGHT));

            hud.skill3ProcChargeBars = new Image[CHARGE_COUNT];
            for (int i = 0; i < CHARGE_COUNT; i++)
            {
                var barGo = new GameObject($"Charge{i + 1}");
                barGo.transform.SetParent(chargeRoot.transform, false);
                var barRt = barGo.AddComponent<RectTransform>();
                float xPos = -totalW * 0.5f + BAR_WIDTH * 0.5f + i * (BAR_WIDTH + BAR_GAP);
                barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 0.5f);
                barRt.anchoredPosition = new Vector2(xPos, 0f);
                barRt.sizeDelta = new Vector2(BAR_WIDTH, BAR_HEIGHT);
                var image = barGo.AddComponent<Image>();
                image.color = new Color(0.22f, 0.18f, 0.1f, 0.75f);
                hud.skill3ProcChargeBars[i] = image;
            }
        }

        // ── 技能2疊層（Skill3 Stacks）：血條左側，黃色（帶灰底）──
        {
            var stack2BgGo = new GameObject("Skill2StackBg");
            stack2BgGo.transform.SetParent(playerHpRoot.transform, false);
            var stack2BgRt = stack2BgGo.AddComponent<RectTransform>();
            SetAnchored(stack2BgRt, new Vector2(0f, 0.5f), new Vector2(-28, 0), new Vector2(30, 22));
            hud.skill2StackBg = stack2BgGo.AddComponent<Image>();
            hud.skill2StackBg.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
            stack2BgGo.SetActive(false);

            hud.skill2StackText = CreateText(playerHpRoot.transform, "Skill2Stack", "", 16, TextAlignmentOptions.Right);
            SetAnchored(hud.skill2StackText.rectTransform, new Vector2(0f, 0.5f), new Vector2(-28, 0), new Vector2(30, 22));
            hud.skill2StackText.color = new Color(1f, 0.85f, 0.2f);
            hud.skill2StackText.fontStyle = FontStyles.Bold;
        }

        // ── 技能欄（下方中央）────────────────────────────
        var skillBar = new GameObject("SkillBar");
        skillBar.transform.SetParent(hudGo.transform, false);
        var skillBarRt = skillBar.AddComponent<RectTransform>();
        skillBarRt.anchorMin = new Vector2(0.5f, 0f);
        skillBarRt.anchorMax = new Vector2(0.5f, 0f);
        skillBarRt.anchoredPosition = new Vector2(0, 20);
        skillBarRt.sizeDelta = new Vector2(320, 70);

        string[] abbrevs = { "1", "2", "3", "R" };
        hud.skillSlots = new SkillSlotUI[4];
        for (int i = 0; i < 4; i++)
            hud.skillSlots[i] = BuildSkillSlot(skillBar.transform, i, abbrevs[i]);

        // ── 右上：計時 + 結算按鈕 + DPS + 技能統計 ──────
        hud.timerText = CreateText(hudGo.transform, "TimerText", "00:00", 20, TextAlignmentOptions.Right);
        SetAnchored(hud.timerText.rectTransform, new Vector2(1f, 1f), new Vector2(-185, -15), new Vector2(120, 30));

        // 結算按鈕（計時右側）
        var finishBtnGo = new GameObject("FinishBtn");
        finishBtnGo.transform.SetParent(hudGo.transform, false);
        var finishBtnRt = finishBtnGo.AddComponent<RectTransform>();
        finishBtnRt.anchorMin = finishBtnRt.anchorMax = new Vector2(1f, 1f);
        finishBtnRt.pivot = new Vector2(1f, 1f);
        finishBtnRt.anchoredPosition = new Vector2(-15, -10);
        finishBtnRt.sizeDelta = new Vector2(60, 30);
        finishBtnGo.AddComponent<Image>().color = new Color(0.6f, 0.15f, 0.15f, 0.9f);
        var finishBtn = finishBtnGo.AddComponent<Button>();
        var finishColors = finishBtn.colors;
        finishColors.highlightedColor = new Color(0.8f, 0.2f, 0.2f);
        finishColors.pressedColor = new Color(0.4f, 0.1f, 0.1f);
        finishBtn.colors = finishColors;
        var finishLabel = CreateText(finishBtnGo.transform, "Label", "結算", 14, TextAlignmentOptions.Center);
        finishLabel.rectTransform.anchorMin = Vector2.zero;
        finishLabel.rectTransform.anchorMax = Vector2.one;
        finishLabel.rectTransform.sizeDelta = Vector2.zero;
        hud.finishBtn = finishBtn;

        hud.dpsText = CreateText(hudGo.transform, "DpsText", "DPS: 0.0", 15, TextAlignmentOptions.Right);
        SetAnchored(hud.dpsText.rectTransform, new Vector2(1f, 1f), new Vector2(-15, -50), new Vector2(160, 25));

        hud.skillStatsText = CreateText(hudGo.transform, "SkillStatsText", "", 13, TextAlignmentOptions.Right);
        SetAnchored(hud.skillStatsText.rectTransform, new Vector2(1f, 1f), new Vector2(-15, -80), new Vector2(200, 90));
        hud.skillStatsText.color = new Color(0.85f, 0.85f, 0.85f);

        // ── 左上：系統提示（Mask 限制文字在框內）────────
        var logPanel = CreatePanel(hudGo.transform, "SystemLogPanel",
            new Vector2(0f, 1f), new Vector2(10, -10), new Vector2(300, 140));
        logPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.45f);

        var maskGo = new GameObject("LogMask");
        maskGo.transform.SetParent(logPanel.transform, false);
        var maskRt = maskGo.AddComponent<RectTransform>();
        StretchFill(maskRt, 4);
        maskGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        maskGo.AddComponent<Mask>().showMaskGraphic = false;

        var scrollGo = new GameObject("SystemLogScroll");
        scrollGo.transform.SetParent(maskGo.transform, false);
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        StretchFill(scrollRt);
        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.scrollSensitivity = 20f;

        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewportRt = viewportGo.AddComponent<RectTransform>();
        StretchFill(viewportRt);
        viewportGo.AddComponent<Image>().color = Color.clear;
        viewportGo.AddComponent<Mask>().showMaskGraphic = false;
        scroll.viewport = viewportRt;

        var contentGo = new GameObject("Content");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0, 0);
        var csf = contentGo.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        hud.systemLogText = CreateText(contentGo.transform, "LogText", "", 11, TextAlignmentOptions.TopLeft);
        hud.systemLogText.rectTransform.anchorMin = Vector2.zero;
        hud.systemLogText.rectTransform.anchorMax = Vector2.one;
        hud.systemLogText.rectTransform.sizeDelta = Vector2.zero;
        hud.systemLogText.color = new Color(0.9f, 0.9f, 0.9f);
        hud.systemLogText.overflowMode = TextOverflowModes.Overflow;

        scroll.content = contentRt;
        hud.systemLogScroll = scroll;

        // ── Boss 技能讀條與半透明名稱背景 ────────────────
        var bossCastPanel = CreatePanel(hudGo.transform, "BossCastAlert",
            new Vector2(0.5f, 1f), new Vector2(0, -72), new Vector2(620, 58));
        bossCastPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        hud.bossCastBarRoot = bossCastPanel;

        var castBg = new GameObject("CastBarBackground");
        castBg.transform.SetParent(bossCastPanel.transform, false);
        var castBgRt = castBg.AddComponent<RectTransform>();
        castBgRt.anchorMin = new Vector2(0f, 1f);
        castBgRt.anchorMax = new Vector2(1f, 1f);
        castBgRt.pivot = new Vector2(0.5f, 1f);
        castBgRt.anchoredPosition = new Vector2(0f, -4f);
        castBgRt.sizeDelta = new Vector2(-8f, 14f);
        castBg.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

        var bossCastFillGo = new GameObject("CastBarFill");
        bossCastFillGo.transform.SetParent(castBg.transform, false);
        var bossCastFillRt = bossCastFillGo.AddComponent<RectTransform>();
        bossCastFillRt.anchorMin = Vector2.zero;
        bossCastFillRt.anchorMax = Vector2.zero;
        bossCastFillRt.offsetMin = Vector2.zero;
        bossCastFillRt.offsetMax = Vector2.zero;
        hud.bossCastFill = bossCastFillGo.AddComponent<Image>();
        hud.bossCastFill.color = new Color(1f, 0.28f, 0.12f);
        hud.bossCastFill.type = Image.Type.Simple;
        hud.bossCastFill.fillAmount = 0f;

        hud.bossSkillAlertText = CreateText(bossCastPanel.transform, "BossSkillAlert", "", 18, TextAlignmentOptions.Center);
        SetAnchored(hud.bossSkillAlertText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 5), new Vector2(604, 30));
        hud.bossSkillAlertText.color = new Color(1f, 0.45f, 0.35f);
        bossCastPanel.SetActive(false);

        // ── 左上：玩家受傷來源／時間／數值 ────────────────
        var damagePanel = CreatePanel(hudGo.transform, "DamageTakenPanel",
            new Vector2(0f, 1f), new Vector2(10, -160), new Vector2(390, 170));
        damagePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        hud.damageTakenPanel = damagePanel;

        var damageTitle = CreateText(damagePanel.transform, "Title", "DAMAGE TAKEN", 13, TextAlignmentOptions.Left);
        SetAnchored(damageTitle.rectTransform, new Vector2(0f, 1f), new Vector2(8, -7), new Vector2(374, 22));
        damageTitle.fontStyle = FontStyles.Bold;
        damageTitle.color = new Color(1f, 0.55f, 0.5f);

        hud.damageTakenText = CreateText(damagePanel.transform, "DamageTakenText", "No damage taken", 12, TextAlignmentOptions.TopLeft);
        hud.damageTakenText.rectTransform.anchorMin = Vector2.zero;
        hud.damageTakenText.rectTransform.anchorMax = Vector2.one;
        hud.damageTakenText.rectTransform.offsetMin = new Vector2(8f, 8f);
        hud.damageTakenText.rectTransform.offsetMax = new Vector2(-8f, -31f);
        hud.damageTakenText.overflowMode = TextOverflowModes.Truncate;

        // ── 傷害跳字 ─────────────────────────────────────
        hud.damageNumberPrefab = BuildDamageNumberPrefab();
        hud.worldCanvas = canvas;

        // ── BattleTimer ───────────────────────────────────
        var gmGo = GameObject.Find("GameManager");
        if (gmGo != null && gmGo.GetComponent<BattleTimer>() == null)
        {
            var bt = gmGo.AddComponent<BattleTimer>();
            bt.StartTimer();
        }

        // ── PlayerStats ───────────────────────────────────
        var player = GameObject.Find("Player");
        if (player != null && player.GetComponent<PlayerStats>() == null)
            player.AddComponent<PlayerStats>();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[HudBuilder] HUD built.");
    }

    // ── 技能格建立 ────────────────────────────────────────
    static SkillSlotUI BuildSkillSlot(Transform parent, int index, string abbrev)
    {
        float slotSize = 70f;
        float spacing = 10f;
        float totalWidth = 4 * slotSize + 3 * spacing;
        float startX = -totalWidth * 0.5f + slotSize * 0.5f;

        var go = new GameObject($"Skill{index + 1}Slot");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(slotSize, slotSize);
        rt.anchoredPosition = new Vector2(startX + index * (slotSize + spacing), 0);

        // 背景
        go.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

        var slot = go.AddComponent<SkillSlotUI>();
        slot.isSkill2 = (index == 1);
        slot.isSkill4 = (index == 3);

        // 圖示顏色
        Color iconColor = index switch
        {
            0 => new Color(0.3f, 0.6f, 1f),
            1 => new Color(1f, 0.35f, 0.1f),
            2 => new Color(1f, 0.8f, 0.2f),
            3 => new Color(1f, 0.55f, 0.75f),
            _ => Color.white
        };

        // 圖示
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        StretchFill(iconGo.AddComponent<RectTransform>(), 4);
        slot.iconImage = iconGo.AddComponent<Image>();
        slot.iconImage.color = iconColor;

        // 不可用遮罩（灰色50%，純色）— 注意：透明度設為 0.5
        var unavailGo = new GameObject("UnavailMask");
        unavailGo.transform.SetParent(go.transform, false);
        StretchFill(unavailGo.AddComponent<RectTransform>(), 4);
        slot.unavailableMask = unavailGo.AddComponent<Image>();
        slot.unavailableMask.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        unavailGo.SetActive(false); // 預設隱藏，不可用時才顯示

        // GCD/冷卻 Radial 遮罩
        var maskGo = new GameObject("CooldownMask");
        maskGo.transform.SetParent(go.transform, false);
        StretchFill(maskGo.AddComponent<RectTransform>(), 4);
        slot.cooldownMask = maskGo.AddComponent<Image>();
        slot.cooldownMask.color = new Color(0f, 0f, 0f, 0.65f);
        slot.cooldownMask.type = Image.Type.Filled;
        slot.cooldownMask.fillMethod = Image.FillMethod.Radial360;
        slot.cooldownMask.fillOrigin = (int)Image.Origin360.Top;
        slot.cooldownMask.fillClockwise = false;
        maskGo.SetActive(false);

        // 發光邊框（技能3/爆發）
        var glowGo = new GameObject("Glow");
        glowGo.transform.SetParent(go.transform, false);
        StretchFill(glowGo.AddComponent<RectTransform>(), 0);
        slot.glowImage = glowGo.AddComponent<Image>();
        slot.glowImage.color = Color.clear;

        // 按下閃爍邊框（黑色）
        var borderGo = new GameObject("BorderFlash");
        borderGo.transform.SetParent(go.transform, false);
        StretchFill(borderGo.AddComponent<RectTransform>(), 0);
        slot.borderFlash = borderGo.AddComponent<Image>();
        slot.borderFlash.color = Color.black;
        borderGo.SetActive(false);

        // 縮寫（左上）
        var abbrevGo = new GameObject("Abbrev");
        abbrevGo.transform.SetParent(go.transform, false);
        var abbrevRt = abbrevGo.AddComponent<RectTransform>();
        abbrevRt.anchorMin = new Vector2(0, 1);
        abbrevRt.anchorMax = new Vector2(0, 1);
        abbrevRt.anchoredPosition = new Vector2(4, -4);
        abbrevRt.sizeDelta = new Vector2(20, 20);
        slot.abbrevText = abbrevGo.AddComponent<TextMeshProUGUI>();
        slot.abbrevText.text = abbrev;
        slot.abbrevText.fontSize = 11;
        slot.abbrevText.alignment = TextAlignmentOptions.TopLeft;
        slot.abbrevText.color = Color.white;

        // 冷卻秒數（中央）
        var cdGo = new GameObject("CdText");
        cdGo.transform.SetParent(go.transform, false);
        StretchFill(cdGo.AddComponent<RectTransform>(), 0);
        slot.cdText = cdGo.AddComponent<TextMeshProUGUI>();
        slot.cdText.fontSize = 16;
        slot.cdText.alignment = TextAlignmentOptions.Center;
        slot.cdText.color = Color.white;
        slot.cdText.fontStyle = FontStyles.Bold;

        return slot;
    }

    static GameObject BuildDamageNumberPrefab()
    {
        var go = new GameObject("DamageNumber");
        go.AddComponent<RectTransform>().sizeDelta = new Vector2(100, 40);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return go;
    }

    // ── 輔助 ──────────────────────────────────────────────
    static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        return go;
    }

    static Slider CreateSlider(Transform parent, string name, Color bgColor, Color fillColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        StretchFill(rt);
        var slider = go.AddComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 1; slider.value = 1;
        slider.interactable = false;

        var bg = new GameObject("Background"); bg.transform.SetParent(go.transform, false);
        StretchFill(bg.AddComponent<RectTransform>());
        bg.AddComponent<Image>().color = bgColor;

        var fillArea = new GameObject("Fill Area"); fillArea.transform.SetParent(go.transform, false);
        StretchFill(fillArea.AddComponent<RectTransform>());

        var fill = new GameObject("Fill"); fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one; fillRt.sizeDelta = Vector2.zero;
        fill.AddComponent<Image>().color = fillColor;
        slider.fillRect = fillRt;
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

    static void StretchFill(RectTransform rt, float padding = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padding, padding);
        rt.offsetMax = new Vector2(-padding, -padding);
    }
}
#endif
