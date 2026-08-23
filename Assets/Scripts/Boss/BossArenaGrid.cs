using UnityEngine;

/// <summary>
/// 將 Arena 切割成 4×4 格子，提供格子中心座標查詢。
/// row 0 = 北側（+Z），row 3 = 南側（-Z）
/// col 0 = 西側（-X），col 3 = 東側（+X）
/// </summary>
public class BossArenaGrid : MonoBehaviour
{
    public static BossArenaGrid Instance { get; private set; }

    // Arena 半徑（從 ArenaSetup 取得）
    public float ArenaHalfSize => ArenaSetup.ArenaHalfSize; // 12
    public float CellSize => ArenaSetup.CellSize;           // 6
    public int GridCount => ArenaSetup.GridCount;           // 4

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>
    /// 取得格子中心的世界座標（Y 略高於地板，避免 Z-Fighting）
    /// row 0 = 北（+Z），row 3 = 南（-Z）
    /// col 0 = 西（-X），col 3 = 東（+X）
    /// </summary>
    public Vector3 GetCellCenter(int row, int col, float yOffset = 0.05f)
    {
        float half = ArenaHalfSize;
        float cs = CellSize;
        // row 0 → z = half - cs*0.5
        float z = half - cs * 0.5f - row * cs;
        float x = -half + cs * 0.5f + col * cs;
        return new Vector3(x, yOffset, z);
    }

    /// <summary>
    /// 將世界座標轉換為格子索引（row, col）
    /// </summary>
    public (int row, int col) WorldPositionToCell(Vector3 worldPos)
    {
        float half = ArenaHalfSize;
        float cs = CellSize;
        // col: x 從 -half 開始
        int col = Mathf.Clamp(Mathf.FloorToInt((worldPos.x + half) / cs), 0, GridCount - 1);
        // row: z 從 +half 開始往南
        int row = Mathf.Clamp(Mathf.FloorToInt((half - worldPos.z) / cs), 0, GridCount - 1);
        return (row, col);
    }

    /// <summary>
    /// 取得整排（row）的中心世界座標
    /// </summary>
    public Vector3 GetRowCenter(int row, float yOffset = 0.05f)
    {
        float half = ArenaHalfSize;
        float cs = CellSize;
        float z = half - cs * 0.5f - row * cs;
        return new Vector3(0f, yOffset, z);
    }

    /// <summary>
    /// 取得整列（col）的中心世界座標
    /// </summary>
    public Vector3 GetColCenter(int col, float yOffset = 0.05f)
    {
        float half = ArenaHalfSize;
        float cs = CellSize;
        float x = -half + cs * 0.5f + col * cs;
        return new Vector3(x, yOffset, 0f);
    }

    /// <summary>
    /// Arena 中心
    /// </summary>
    public Vector3 ArenaCenter => new Vector3(0f, 0.05f, 0f);

    /// <summary>
    /// 取得整排的 Bounds（用於顯示長方形預告）
    /// </summary>
    public (Vector3 center, Vector3 size) GetRowBounds(int row)
    {
        float half = ArenaHalfSize;
        float cs = CellSize;
        float z = half - cs * 0.5f - row * cs;
        return (new Vector3(0f, 0.05f, z), new Vector3(half * 2f, 0.01f, cs));
    }

    /// <summary>
    /// 取得整列的 Bounds
    /// </summary>
    public (Vector3 center, Vector3 size) GetColBounds(int col)
    {
        float half = ArenaHalfSize;
        float cs = CellSize;
        float x = -half + cs * 0.5f + col * cs;
        return (new Vector3(x, 0.05f, 0f), new Vector3(cs, 0.01f, half * 2f));
    }
}
