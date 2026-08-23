using UnityEngine;

/// <summary>
/// 按鍵綁定設定，可在設定選單中修改。
/// ponytail: 靜態欄位，不需要 ScriptableObject，設定選單直接寫入即可。
/// </summary>
public static class KeyBindings
{
    public static KeyCode Skill1 = KeyCode.Alpha1;
    public static KeyCode Skill2 = KeyCode.Alpha2;
    public static KeyCode Skill3 = KeyCode.Alpha3; // 保留，不可主動施放
    public static KeyCode Skill4 = KeyCode.R;

    public static void Reset()
    {
        Skill1 = KeyCode.Alpha1;
        Skill2 = KeyCode.Alpha2;
        Skill3 = KeyCode.Alpha3;
        Skill4 = KeyCode.R;
    }
}
