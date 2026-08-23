using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// HOME 鍵測試選單（測試階段用，正式版移除）。
/// </summary>
public class DebugMenu : MonoBehaviour
{
    public GameObject panel;

    SkillSystem _skills;
    PlayerController _player;
    BattleTimer _timer;

    void Start()
    {
        var playerGo = GameObject.Find("Player");
        _skills = playerGo?.GetComponent<SkillSystem>();
        _player = playerGo?.GetComponent<PlayerController>();
        _timer  = FindFirstObjectByType<BattleTimer>();
        panel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Home))
            panel.SetActive(!panel.activeSelf);
    }

    public void TriggerPlayerDeath()
    {
        panel.SetActive(false);
        _player?.Die();
    }

    public void TriggerVictory()
    {
        panel.SetActive(false);
        GameManager.Instance.SetState(GameManager.GameState.Victory);
    }

    public void ResetCooldowns()  => _skills?.ResetCooldowns();
    public void ResetDpsStats()   => _skills?.ResetDpsStats();

    public void RestartBattle()
    {
        panel.SetActive(false);
        SceneManager.LoadScene(SceneNames.Battle);
    }
}
