using UnityEngine;

/// <summary>
/// 俯視斜角第三人稱攝影機。
/// 右鍵按住 → 360度旋轉（yaw + pitch）+ 隱藏鼠標。
/// 滾輪 → 縮放距離（最大為初始距離，最小約能看到自身）。
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("跟隨目標")]
    public Transform target;

    [Header("攝影機參數")]
    public float distance = 20f;
    public float pitch = 50f;
    public float yaw = 0f;
    public float rotateSpeed = 3f;

    [Header("縮放")]
    public float zoomSpeed = 3f;
    public float minDistance = 2f;   // 最近：約只看到自身
    // maxDistance 由初始 distance 決定（搬移後保持原始值）
    float _maxDistance;

    [Header("平滑")]
    public float followSmooth = 10f;

    bool _rotating;

    void Start()
    {
        _maxDistance = distance; // 以初始值作為最大距離上限
        // 確保進入遊戲時 Cursor 是解鎖可見狀態
        _rotating = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // ── 滾輪縮放 ──────────────────────────────────────
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            distance -= scroll * zoomSpeed * distance * 0.3f; // 相對縮放，遠時快近時慢
            distance = Mathf.Clamp(distance, minDistance, _maxDistance);
        }

        // ── 右鍵按住 → 旋轉 + 隱藏鼠標 ──────────────────
        if (Input.GetMouseButtonDown(1))
        {
            _rotating = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (Input.GetMouseButtonUp(1))
        {
            _rotating = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (_rotating)
        {
            yaw   += Input.GetAxis("Mouse X") * rotateSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed; // 上下360度，不限制
        }

        // ── 計算位置 ──────────────────────────────────────
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = target.position + rot * new Vector3(0, 0, -distance);

        transform.position = Vector3.Lerp(transform.position, desiredPos, followSmooth * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1f);
    }
}
