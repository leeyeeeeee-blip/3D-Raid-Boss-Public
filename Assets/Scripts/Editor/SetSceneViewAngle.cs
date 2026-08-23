using UnityEngine;
using UnityEditor;

public class SetSceneViewAngle
{
    [MenuItem("FFXIV/Set Scene View Top-Down")]
    public static void SetTopDown()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null && SceneView.sceneViews.Count > 0)
            sv = (SceneView)SceneView.sceneViews[0];
        if (sv == null) return;

        sv.in2DMode = false;
        sv.pivot = new Vector3(0f, 0f, 0f);
        sv.rotation = Quaternion.Euler(55f, 0f, 0f);
        sv.size = 20f;
        sv.Repaint();
    }

    [MenuItem("FFXIV/Set Scene View Wide")]
    public static void SetWide()
    {
        var sv = SceneView.lastActiveSceneView;
        if (sv == null && SceneView.sceneViews.Count > 0)
            sv = (SceneView)SceneView.sceneViews[0];
        if (sv == null) return;

        sv.in2DMode = false;
        // 從高處俯視整個 Arena
        sv.pivot = new Vector3(0f, 0f, 0f);
        sv.rotation = Quaternion.Euler(60f, 15f, 0f);
        sv.size = 25f;  // 拉遠
        sv.Repaint();
        Debug.Log("[SetSceneViewAngle] Wide view set.");
    }
}
