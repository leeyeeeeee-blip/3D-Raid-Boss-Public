using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class TestTransparency
{
    [MenuItem("FFXIV/Test Transparency Visual")]
    public static void Test()
    {
        var old = GameObject.Find("TransparencyTest");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("TransparencyTest");

        // 測試不同 shader 的透明度
        string[] shaders = {
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Lit",
            "Sprites/Default",
            "Unlit/Transparent",
            "Unlit/Color"
        };

        float cellSize = 6f;
        float arenaHalf = 12f;

        // 建立棋盤格（使用 URP Unlit）
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4; c++)
            {
                if ((r + c) % 2 == 0)
                {
                    float z = arenaHalf - cellSize * 0.5f - r * cellSize;
                    float x = -arenaHalf + cellSize * 0.5f + c * cellSize;

                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = $"Checker_r{r}c{c}";
                    go.transform.SetParent(root.transform);
                    go.transform.position = new Vector3(x, 0.06f, z);
                    go.transform.localScale = new Vector3(cellSize * 0.98f, 0.03f, cellSize * 0.98f);

                    var col = go.GetComponent<Collider>();
                    if (col != null) Object.DestroyImmediate(col);

                    // 嘗試 URP Unlit transparent
                    var shader = Shader.Find("Universal Render Pipeline/Unlit");
                    if (shader == null) shader = Shader.Find("Sprites/Default");

                    var mat = new Material(shader);

                    if (shader.name.Contains("Universal Render Pipeline"))
                    {
                        mat.SetFloat("_Surface", 1f);
                        mat.SetFloat("_Blend", 0f);
                        mat.SetFloat("_ZWrite", 0f);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.renderQueue = 3000;
                        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        mat.SetShaderPassEnabled("ShadowCaster", false);
                    }

                    mat.color = new Color(1f, 0.1f, 0.1f, 0.4f);
                    go.GetComponent<Renderer>().sharedMaterial = mat;
                }
            }
        }

        // 青色錨點
        {
            float z = arenaHalf - cellSize * 0.5f - 1 * cellSize;
            float x = -arenaHalf + cellSize * 0.5f + 1 * cellSize;
            var anchor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            anchor.name = "AnchorTest";
            anchor.transform.SetParent(root.transform);
            anchor.transform.position = new Vector3(x, 0.08f, z);
            anchor.transform.localScale = new Vector3(cellSize * 0.98f, 0.04f, cellSize * 0.98f);
            var col = anchor.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (shader.name.Contains("Universal Render Pipeline"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetFloat("_ZWrite", 0f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.SetShaderPassEnabled("ShadowCaster", false);
            }
            mat.color = new Color(0f, 0.9f, 1f, 0.45f);
            anchor.GetComponent<Renderer>().sharedMaterial = mat;
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

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

        Debug.Log("[TestTransparency] 透明度測試物件已建立！");
        Selection.activeGameObject = root;
    }

    [MenuItem("FFXIV/Remove Transparency Test")]
    public static void Remove()
    {
        var old = GameObject.Find("TransparencyTest");
        if (old != null)
        {
            Object.DestroyImmediate(old);
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }
    }
}
