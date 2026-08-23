using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// ESC 暫停選單。
/// - ESC 關閉時同時關閉 Settings 視窗。
/// - 按鈕在 Start() 中自動連接。
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI timerText;

    BattleTimer _timer;
    SettingsMenu _settings;

    void Start()
    {
        _timer    = FindFirstObjectByType<BattleTimer>();
        _settings = FindFirstObjectByType<SettingsMenu>();
        if (panel != null) panel.SetActive(false);

        ConnectButton("Resume",    () => Resume());
        ConnectButton("Restart",   () => Restart());
        ConnectButton("Settings",  () => Settings());
        ConnectButton("Main Menu", () => MainMenu());
    }

    void ConnectButton(string btnName, UnityEngine.Events.UnityAction action)
    {
        if (panel == null) return;
        var tf = panel.transform.Find(btnName);
        if (tf == null) return;
        var btn = tf.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Toggle();
    }

    void Toggle()
    {
        bool show = !panel.activeSelf;
        panel.SetActive(show);
        GameManager.Instance.SetState(show ? GameManager.GameState.Paused : GameManager.GameState.Playing);

        if (show)
        {
            // 開啟暫停：顯示計時
            if (timerText != null && _timer != null)
                timerText.text = $"Battle Time: {_timer.FormatTime()}";
        }
        else
        {
            // 關閉暫停：同時關閉 Settings 視窗
            if (_settings == null) _settings = FindFirstObjectByType<SettingsMenu>();
            _settings?.Close();
        }
    }

    public void Resume()   => Toggle();
    public void Restart()  => SceneManager.LoadScene(SceneNames.Battle);
    public void MainMenu() => SceneManager.LoadScene(SceneNames.MainMenu);
    public void Settings()
    {
        if (_settings == null) _settings = FindFirstObjectByType<SettingsMenu>();
        _settings?.Open();
    }
}
