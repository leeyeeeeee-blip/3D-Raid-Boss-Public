using UnityEngine;
using UnityEditor;

/// <summary>
/// 測試危險區視覺效果：在 Editor 中直接建立危險區物件（不需要 Play Mode）
/// </summary>
public class TestHazardVisual
{
    [MenuItem("FFXIV/Test Hazard Visual (Editor)")]
    public static void CreateTestHazards()
    {
        // 清除舊的測試物件
        var old = GameObject.Find("TestHazards");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("TestHazards");

        float cellSize = 6f;
        float arenaHalf = 12f;

        // 建立棋盤格危險區（patternVariant = 0）
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                if ((r + c) % 2 == 0)
                {
                    float z = arenaHalf - cellSize * 0.5f - r * cellSize;
                    float x = -arenaHalf + cellSize * 0.5f + c * cellSize;
                    Vector3 center = new Vector3(x, 0.06f, z);

                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = $"TestHazard_r{r}c{c}";
                    go.transform.SetParent(root.transform);
                    go.transform.position = center;
                    go.transform.localScale = new Vector3(cellSize * 0.98f, 0.03f, cellSize * 0.98f);

                    var col = go.GetComponent<Collider>();
                    if (col != null) Object.DestroyImmediate(col);

                    var mat = new Material(Shader.Find("Sprites/Default"));
                    mat.color = new Color(1f, 0.1f, 0.1f, 0.5f);
                    go.GetComponent<Renderer>().sharedMaterial = mat;
                }
            }
        }

        // 建立一個青色錨點格
        {
            float z = arenaHalf - cellSize * 0.5f - 1 * cellSize;
            float x = -arenaHalf + cellSize * 0.5f + 1 * cellSize;
            var anchor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            anchor.name = "TestAnchor";
            anchor.transform.SetParent(root.transform);
            anchor.transform.position = new Vector3(x, 0.08f, z);
            anchor.transform.localScale = new Vector3(cellSize * 0.98f, 0.04f, cellSize * 0.98f);
            var col = anchor.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(0f, 0.9f, 1f, 0.5f);
            anchor.GetComponent<Renderer>().sharedMaterial = mat;
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[TestHazardVisual] 測試危險區已建立！");
        Selection.activeGameObject = root;

        // 調整 Scene View
        var sv = SceneView.lastActiveSceneView;
        if (sv != null)
        {
            sv.in2DMode = false;
            sv.pivot = new Vector3(0f, 0f, 0f);
            sv.rotation = Quaternion.Euler(50f, 20f, 0f);
            sv.size = 20f;
            sv.Repaint();
        }
    }

    [MenuItem("FFXIV/Remove Test Hazards")]
    public static void RemoveTestHazards()
    {
        var old = GameObject.Find("TestHazards");
        if (old != null)
        {
            Object.DestroyImmediate(old);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[TestHazardVisual] 測試危險區已移除。");
        }
    }
}
