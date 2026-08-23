using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class RebuildHudNow
{
    public static void Execute()
    {
        // 1. 完整重建 HUD
        HudBuilder.BuildHud();

        // 2. 套用圓形 Sprite 到 Skill1StackDots
        var hudGo = GameObject.Find("HUD");
        if (hudGo == null) { Debug.LogError("[RebuildHudNow] HUD not found"); return; }

        var dotsTf = hudGo.transform.Find("PlayerHpRoot/Skill1StackDots");
        if (dotsTf == null) { Debug.LogError("[RebuildHudNow] Skill1StackDots not found"); return; }

        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        if (circleSprite == null)
            circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        var hud = hudGo.GetComponent<HudManager>();
        if (hud != null) hud.skill1StackDots = new Image[dotsTf.childCount];

        for (int i = 0; i < dotsTf.childCount; i++)
        {
            var img = dotsTf.GetChild(i).GetComponent<Image>();
            if (img != null && circleSprite != null)
            {
                img.sprite = circleSprite;
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
            }
            if (hud != null) hud.skill1StackDots[i] = img;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[RebuildHudNow] HUD rebuilt with circle dots and finish button.");
    }
}
