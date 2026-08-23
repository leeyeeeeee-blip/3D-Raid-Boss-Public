using UnityEngine;

/// <summary>
/// 場地資料：提供 ArenaHalfSize 給其他系統查詢。
/// 實際物件由 SceneBuilder 在 Editor 中建立。
/// </summary>
public class ArenaSetup : MonoBehaviour
{
    public static float CellSize = 6f;
    public static int GridCount = 4;
    public static float ArenaHalfSize => CellSize * GridCount * 0.5f; // 12
}
