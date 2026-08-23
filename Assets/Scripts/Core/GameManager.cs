using UnityEngine;

/// <summary>
/// 遊戲狀態管理，單例。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Paused, Dead, Victory }
    public GameState State { get; private set; } = GameState.Playing;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void SetState(GameState newState)
    {
        State = newState;
        Time.timeScale = (newState == GameState.Paused) ? 0f : 1f;
        Debug.Log($"[GameManager] State → {newState}");
    }

    public bool IsPlaying => State == GameState.Playing;
}
