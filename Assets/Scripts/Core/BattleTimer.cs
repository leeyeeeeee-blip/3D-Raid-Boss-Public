using UnityEngine;

/// <summary>
/// 戰鬥計時器。Play Mode 開始時自動啟動。
/// </summary>
public class BattleTimer : MonoBehaviour
{
    public float ElapsedSeconds { get; private set; }
    public bool Running { get; private set; }

    void Awake()
    {
        // Play Mode 開始時自動啟動計時
        StartTimer();
    }

    public void StartTimer() { Running = true; ElapsedSeconds = 0f; }
    public void StopTimer()  { Running = false; }
    public void ResetTimer() { ElapsedSeconds = 0f; }

    void Update()
    {
        if (Running && GameManager.Instance != null && GameManager.Instance.IsPlaying)
            ElapsedSeconds += Time.deltaTime;
    }

    public string FormatTime()
    {
        int m = (int)(ElapsedSeconds / 60);
        int s = (int)(ElapsedSeconds % 60);
        return $"{m:00}:{s:00}";
    }
}
