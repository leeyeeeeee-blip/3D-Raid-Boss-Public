using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 設定選單：音量 + 按鍵綁定。
/// Slider 和按鈕在 Start() 中自動連接。
/// </summary>
public class SettingsMenu : MonoBehaviour
{
    public GameObject panel;
    public Slider musicSlider;
    public Slider sfxSlider;

    public TextMeshProUGUI skill1KeyText;
    public TextMeshProUGUI skill2KeyText;
    public TextMeshProUGUI skill4KeyText;

    bool _waitingForKey;
    int _bindingSkill;

    const string KEY_MUSIC = "MusicVol";
    const string KEY_SFX   = "SfxVol";

    void Start()
    {
        if (panel != null) panel.SetActive(false);

        // 重新連接 Slider 事件（AddListener 不序列化）
        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        }
        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        }

        // 重新連接 Close 按鈕
        if (panel != null)
        {
            var closeTf = panel.transform.Find("Close");
            if (closeTf != null)
            {
                var btn = closeTf.GetComponent<Button>();
                if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(Close); }
            }

            // 重新連接 Rebind 按鈕
            ConnectRebindButton("Bind_Skill 1/Rebind",          1);
            ConnectRebindButton("Bind_Skill 2/Rebind",          2);
            ConnectRebindButton("Bind_Skill 4 (Burst)/Rebind",  4);
        }

        LoadSettings();
    }

    void ConnectRebindButton(string path, int skillIndex)
    {
        if (panel == null) return;
        var tf = panel.transform.Find(path);
        if (tf == null) return;
        var btn = tf.GetComponent<Button>();
        if (btn == null) return;
        int idx = skillIndex;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => StartRebind(idx));
    }

    public void Open()  => panel?.SetActive(true);
    public void Close() => panel?.SetActive(false);

    void LoadSettings()
    {
        if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat(KEY_MUSIC, 1f);
        if (sfxSlider   != null) sfxSlider.value   = PlayerPrefs.GetFloat(KEY_SFX, 1f);
        RefreshKeyTexts();
    }

    public void OnMusicChanged(float v)
    {
        PlayerPrefs.SetFloat(KEY_MUSIC, v);
        AudioListener.volume = v;
    }

    public void OnSfxChanged(float v) => PlayerPrefs.SetFloat(KEY_SFX, v);

    public void StartRebind(int skillIndex)
    {
        _waitingForKey = true;
        _bindingSkill  = skillIndex;
    }

    void Update()
    {
        if (!_waitingForKey) return;
        foreach (KeyCode kc in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(kc))
            {
                switch (_bindingSkill)
                {
                    case 1: KeyBindings.Skill1 = kc; break;
                    case 2: KeyBindings.Skill2 = kc; break;
                    case 4: KeyBindings.Skill4 = kc; break;
                }
                _waitingForKey = false;
                RefreshKeyTexts();
                break;
            }
        }
    }

    void RefreshKeyTexts()
    {
        if (skill1KeyText != null) skill1KeyText.text = KeyBindings.Skill1.ToString();
        if (skill2KeyText != null) skill2KeyText.text = KeyBindings.Skill2.ToString();
        if (skill4KeyText != null) skill4KeyText.text = KeyBindings.Skill4.ToString();
    }
}
