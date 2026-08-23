using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 失敗 / 勝利結算畫面。
/// - 顯示本場統計數據
/// - 「儲存紀錄」按鈕：手動按下才寫入 JSON 檔案
/// - 按鈕在 Start() 中自動連接
/// </summary>
public class ResultScreen : MonoBehaviour
{
    public GameObject panel;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI statsText;
    public TextMeshProUGUI saveStatusText; // 顯示「已儲存」或「儲存失敗」
    public Button saveBtn;
    public Button restartBtn;
    public Button mainMenuBtn;

    // 暫存本場資料，等待手動儲存
    GameRecord _pendingRecord;
    bool _saved;

    void Start()
    {
        if (panel != null) panel.SetActive(false);

        // 自動連接按鈕
        ConnectBtn("SaveRecord", () => SaveRecord());
        ConnectBtn("Restart",    () => Restart());
        ConnectBtn("Main Menu",  () => MainMenu());

        StartCoroutine(WatchState());
    }

    void ConnectBtn(string btnName, UnityEngine.Events.UnityAction action)
    {
        if (panel == null) return;
        // 先找 ResultPanel 子層，再找直接子層
        var tf = panel.transform.Find($"ResultPanel/{btnName}")
              ?? panel.transform.Find(btnName);
        if (tf == null) return;
        var btn = tf.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(action);

        // 記錄引用
        if (btnName == "SaveRecord") saveBtn    = btn;
        if (btnName == "Restart")    restartBtn  = btn;
        if (btnName == "Main Menu")  mainMenuBtn = btn;
    }

    System.Collections.IEnumerator WatchState()
    {
        while (true)
        {
            yield return null;
            if (GameManager.Instance == null) continue;
            var state = GameManager.Instance.State;
            if (state == GameManager.GameState.Dead)    { Show(false); yield break; }
            if (state == GameManager.GameState.Victory) { Show(true);  yield break; }
        }
    }

    public void Show(bool victory)
    {
        _saved = false;
        if (panel != null) panel.SetActive(true);

        if (titleText != null)
        {
            titleText.text  = victory ? "VICTORY!" : "DEFEATED";
            titleText.color = victory ? new Color(1f, 0.85f, 0.2f) : new Color(1f, 0.3f, 0.3f);
        }

        // 清除儲存狀態文字
        if (saveStatusText != null) saveStatusText.text = "";

        var skills = GameObject.Find("Player")?.GetComponent<SkillSystem>();
        var playerStats = GameObject.Find("Player")?.GetComponent<PlayerStats>();
        var timer  = FindFirstObjectByType<BattleTimer>();

        if (skills != null && timer != null)
        {
            float t   = timer.ElapsedSeconds;
            float dps = t > 0 ? skills.TotalDamage / t : 0f;

            if (statsText != null)
                statsText.text =
                    $"Time: {timer.FormatTime()}\n" +
                    $"Total DMG: {skills.TotalDamage}\n" +
                    $"DPS: {dps:F1}\n\n" +
                    $"Skill1: {skills.Skill1UseCount}x / {skills.Skill1TotalDmg} dmg\n" +
                    $"Skill2: {skills.Skill2UseCount}x / {skills.Skill2TotalDmg} dmg\n" +
                    $"Skill3: {skills.Skill3UseCount}x / {skills.Skill3TotalDmg} dmg\n" +
                    $"Skill4: {skills.Skill4UseCount}x";

            // 暫存紀錄，等待手動儲存
            _pendingRecord = new GameRecord
            {
                BattleTime = t, TotalDamage = skills.TotalDamage, Dps = dps,
                Skill1Uses = skills.Skill1UseCount, Skill2Uses = skills.Skill2UseCount,
                Skill3Uses = skills.Skill3UseCount, Skill4Uses = skills.Skill4UseCount,
                Skill1Dmg  = skills.Skill1TotalDmg, Skill2Dmg = skills.Skill2TotalDmg,
                Skill3Dmg  = skills.Skill3TotalDmg,
                IsVictory  = victory,
                DateStr    = System.DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
                DamageTaken = playerStats != null
                    ? new System.Collections.Generic.List<DamageTakenRecord>(playerStats.DamageHistory)
                    : new System.Collections.Generic.List<DamageTakenRecord>()
            };
        }

        // 失敗時隱藏 Restart（可依需求調整）
        if (restartBtn != null) restartBtn.gameObject.SetActive(!victory);
        // 重置儲存按鈕狀態
        if (saveBtn != null)
        {
            saveBtn.interactable = true;
            var label = saveBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = "Save Record";
        }
    }

    public void SaveRecord()
    {
        if (_saved)
        {
            if (saveStatusText != null) saveStatusText.text = "Already saved!";
            return;
        }
        if (_pendingRecord == null)
        {
            if (saveStatusText != null) saveStatusText.text = "No data to save.";
            return;
        }

        try
        {
            GameRecordStore.Save(_pendingRecord);
            _saved = true;

            // 更新按鈕與狀態文字
            if (saveBtn != null)
            {
                saveBtn.interactable = false;
                var label = saveBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null) label.text = "Saved ✓";
            }
            if (saveStatusText != null)
                saveStatusText.text = $"Saved to:\n{GameRecordStore.GetRecordsPath()}";
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ResultScreen] 儲存失敗：{e.Message}");
            if (saveStatusText != null) saveStatusText.text = "Save failed!";
        }
    }

    void Restart()  => SceneManager.LoadScene(SceneNames.Battle);
    void MainMenu() => SceneManager.LoadScene(SceneNames.MainMenu);
}
