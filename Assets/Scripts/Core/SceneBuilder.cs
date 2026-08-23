using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor 工具：一鍵建立場景所有物件（直接在 Editor 中建立，非 Runtime）。
/// </summary>
public class SceneBuilder
{
    const float CELL_SIZE = 6f;
    const int GRID_COUNT = 4;
    const float LINE_WIDTH = 0.05f;
    static readonly Color LINE_COLOR = new Color(1f, 1f, 1f, 0.4f);

    [MenuItem("FFXIV/Build Scene")]
    public static void BuildScene()
    {
        // 清除舊物件
        foreach (var name in new[] { "GameManager", "Arena", "Player", "Boss" })
        {
            var old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        BuildGameManager();
        BuildArena();
        BuildPlayer();
        BuildBoss();
        SetupCamera();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[SceneBuilder] 場景建立完成！");
    }

    static void BuildGameManager()
    {
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
    }

    // ── Arena ──────────────────────────────────────────────
    static void BuildArena()
    {
        var arena = new GameObject("Arena");

        BuildFloor(arena.transform);
        BuildGridLines(arena.transform);
        BuildBoundaries(arena.transform);
    }

    static void BuildFloor(Transform parent)
    {
        float total = CELL_SIZE * GRID_COUNT; // 24
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "ArenaFloor";
        floor.transform.SetParent(parent);
        floor.transform.localPosition = Vector3.zero;
        // Plane 預設 10x10，scale 讓它變成 total x total
        float s = total / 10f;
        floor.transform.localScale = new Vector3(s, 1f, s);
        Object.DestroyImmediate(floor.GetComponent<MeshCollider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.12f, 0.12f, 0.18f);
        AssetDatabase.CreateAsset(mat, "Assets/Materials/ArenaFloor.mat");
        floor.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void BuildGridLines(Transform parent)
    {
        float total = CELL_SIZE * GRID_COUNT;
        float half = total * 0.5f;
        var linesParent = new GameObject("GridLines");
        linesParent.transform.SetParent(parent);

        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.white;

        for (int i = 0; i <= GRID_COUNT; i++)
        {
            float pos = -half + i * CELL_SIZE;
            CreateLine(linesParent.transform, $"V{i}", mat,
                new Vector3(pos, 0.02f, -half),
                new Vector3(pos, 0.02f, half));
            CreateLine(linesParent.transform, $"H{i}", mat,
                new Vector3(-half, 0.02f, pos),
                new Vector3(half, 0.02f, pos));
        }
    }

    static void CreateLine(Transform parent, string lineName, Material mat, Vector3 start, Vector3 end)
    {
        var go = new GameObject(lineName);
        go.transform.SetParent(parent);
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.SetPositions(new[] { start, end });
        lr.startWidth = LINE_WIDTH;
        lr.endWidth = LINE_WIDTH;
        lr.useWorldSpace = true;
        lr.sharedMaterial = mat;
        lr.startColor = LINE_COLOR;
        lr.endColor = LINE_COLOR;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }

    static void BuildBoundaries(Transform parent)
    {
        float half = CELL_SIZE * GRID_COUNT * 0.5f; // 12
        float total = CELL_SIZE * GRID_COUNT;        // 24
        float h = 5f;
        var boundParent = new GameObject("Boundaries");
        boundParent.transform.SetParent(parent);

        CreateBoundary(boundParent.transform, "North", new Vector3(0, h * 0.5f, half + 0.5f),  new Vector3(total + 1f, h, 1f));
        CreateBoundary(boundParent.transform, "South", new Vector3(0, h * 0.5f, -half - 0.5f), new Vector3(total + 1f, h, 1f));
        CreateBoundary(boundParent.transform, "East",  new Vector3(half + 0.5f, h * 0.5f, 0),  new Vector3(1f, h, total + 1f));
        CreateBoundary(boundParent.transform, "West",  new Vector3(-half - 0.5f, h * 0.5f, 0), new Vector3(1f, h, total + 1f));
    }

    static void CreateBoundary(Transform parent, string wallName, Vector3 pos, Vector3 size)
    {
        var go = new GameObject($"Boundary_{wallName}");
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.tag = "Boundary";
        var col = go.AddComponent<BoxCollider>();
        col.size = size;
        col.isTrigger = true;
    }

    // ── Player ─────────────────────────────────────────────
    static void BuildPlayer()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Player";
        go.transform.position = new Vector3(0, 1f, -8f);
        Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());

        // CharacterController 用於移動（PlayerController 需要）
        var cc = go.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.5f;
        cc.center = new Vector3(0, 0, 0);
        go.AddComponent<PlayerController>();

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.2f, 0.5f, 1f);
        AssetDatabase.CreateAsset(mat, "Assets/Materials/Player.mat");
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // ── Boss ───────────────────────────────────────────────
    static void BuildBoss()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Boss";
        go.transform.position = new Vector3(0, 1.5f, 8f);
        go.transform.localScale = new Vector3(3f, 3f, 3f);
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0.8f, 0.1f, 0.1f);
        AssetDatabase.CreateAsset(mat, "Assets/Materials/Boss.mat");
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // ── Camera ─────────────────────────────────────────────
    static void SetupCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var ctrl = cam.GetComponent<CameraController>() ?? cam.gameObject.AddComponent<CameraController>();
        var player = GameObject.Find("Player");
        if (player != null) ctrl.target = player.transform;

        // 設定初始位置讓 Scene View 看得到全場
        cam.transform.position = new Vector3(0, 25f, -18f);
        cam.transform.rotation = Quaternion.Euler(50f, 0f, 0f);
    }
}
#endif
