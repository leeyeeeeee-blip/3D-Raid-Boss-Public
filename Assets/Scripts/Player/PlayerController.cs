using UnityEngine;

/// <summary>
/// 玩家移動：WASD 相對攝影機方向，無 Rigidbody，純 Transform 移動。
/// 邊界死亡由 OnTriggerEnter 處理。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 7f;

    [Header("狀態")]
    public bool IsDead { get; private set; }

    // ponytail: CharacterController 處理移動，不用 Rigidbody
    private CharacterController _cc;
    private Transform _camTransform;
    private PlayerStats _playerStats;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        BindPlayerStats();
        // 移除 CharacterController 的碰撞影響（只用它做移動，不做物理碰撞）
    }

    void Start()
    {
        _camTransform = Camera.main != null ? Camera.main.transform : null;
        BindPlayerStats();
    }

    void OnDestroy()
    {
        if (_playerStats != null)
            _playerStats.OnDied -= OnHpDepleted;
    }

    void Update()
    {
        if (IsDead) return;
        if (IsOutsideArena(transform.position, ArenaSetup.ArenaHalfSize))
        {
            Die("Out of bounds");
            return;
        }
        if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
        Move();
    }

    void Move()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h == 0 && v == 0) return;

        // 相對攝影機的水平方向
        Vector3 camForward = _camTransform != null ? _camTransform.forward : Vector3.forward;
        Vector3 camRight = _camTransform != null ? _camTransform.right : Vector3.right;
        camForward.y = 0; camForward.Normalize();
        camRight.y = 0; camRight.Normalize();

        Vector3 dir = (camForward * v + camRight * h).normalized;
        _cc.Move(dir * moveSpeed * Time.deltaTime);

        // 面向移動方向
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dir);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Boundary"))
            Die("Boundary trigger");
    }

    public void Die() => Die("Unknown");

    public void Die(string reason)
    {
        if (IsDead) return;
        IsDead = true;
        if (GameManager.Instance != null)
            GameManager.Instance.SetState(GameManager.GameState.Dead);
        Debug.Log($"[Player] 死亡：{reason}");
    }

    void BindPlayerStats()
    {
        var stats = GetComponent<PlayerStats>();
        if (stats == _playerStats) return;
        if (_playerStats != null) _playerStats.OnDied -= OnHpDepleted;
        _playerStats = stats;
        if (_playerStats != null) _playerStats.OnDied += OnHpDepleted;
    }

    void OnHpDepleted() => Die("HP reached zero");

    public static bool IsOutsideArena(Vector3 position, float arenaHalfSize, float fallDeathY = -1f)
    {
        return Mathf.Abs(position.x) > arenaHalfSize ||
               Mathf.Abs(position.z) > arenaHalfSize ||
               position.y < fallDeathY;
    }

    public void ResetPlayer(Vector3 spawnPos)
    {
        IsDead = false;
        // CharacterController 需先停用再移動
        _cc.enabled = false;
        transform.position = spawnPos;
        _cc.enabled = true;
    }
}
