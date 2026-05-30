using GameData;
using BepInEx.Configuration;
using TMPro;
using WuLin;
using HaxxToyBox.Config;

namespace HaxxToyBox.GUI;

[RegisterInIl2Cpp]
internal class MiscPanel : MonoBehaviour
{
    public static MiscPanel Instance { get; private set; }

    private Switch _timeFreezeSwitch;
    private Switch _recoverSwitch;
    private Switch _noCombatSwitch;
    private Switch _relationSwitch;
    private Switch _enableAchieveSwitch;
    private Switch _ultimateMartialSwitch;

    private Slider _battleSpeedSlider;
    private TMP_InputField _coinInput;
    private TMP_InputField _skillExpInput;

    public int ExpMultiple = 1;
    public int WalkSpeed = 1;
    public int BattleSpeed = 1;

    public bool TimeFreezed => _timeFreezeSwitch.IsToggled();
    public bool RecoverEnabled => _recoverSwitch.IsToggled();
    public bool NoCombat => _noCombatSwitch.IsToggled();
    public bool RelationEnabled => _relationSwitch.IsToggled();
    public bool EnableAchieve => _enableAchieveSwitch.IsToggled();
    public bool UltimateMartial => _ultimateMartialSwitch.IsToggled();

    public MiscPanel(IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        Instance = this;

        _timeFreezeSwitch = transform.Find("Content/SwitchFunc/TimeFreeze/Switch").gameObject.AddComponent<Switch>();
        _recoverSwitch = transform.Find("Content/SwitchFunc/Recover/Switch").gameObject.AddComponent<Switch>();
        _noCombatSwitch = transform.Find("Content/SwitchFunc/NoCombat/Switch").gameObject.AddComponent<Switch>();
        _relationSwitch = transform.Find("Content/SwitchFunc/Friendship/Switch").gameObject.AddComponent<Switch>();
        _enableAchieveSwitch = transform.Find("Content/SwitchFunc/EnableAchievement/Switch").gameObject.AddComponent<Switch>();
        _ultimateMartialSwitch = transform.Find("Content/SwitchFunc/UltimateMartial/Switch").gameObject.AddComponent<Switch>();
        BindSwitch(_timeFreezeSwitch, ConfigManager.TimeFreezeEnabled);
        BindSwitch(_recoverSwitch, ConfigManager.RecoverEnabled);
        BindSwitch(_noCombatSwitch, ConfigManager.NoCombatEnabled);
        BindSwitch(_relationSwitch, ConfigManager.RelationEnabled);
        BindSwitch(_enableAchieveSwitch, ConfigManager.EnableAchievement);
        BindSwitch(_ultimateMartialSwitch, ConfigManager.UltimateMartial);

        _skillExpInput = transform.Find("Content/InputFunc/SkillExp/NumInput").GetComponent<TMP_InputField>();
        ExpMultiple = Mathf.Clamp(ConfigManager.SkillExpMultiple.Value, 1, 1000);
        _skillExpInput.SetTextWithoutNotify(ExpMultiple.ToString());
        _skillExpInput.onValueChanged.RemoveAllListeners();
        _skillExpInput.onEndEdit.RemoveAllListeners();
        _skillExpInput.onValueChanged.AddListener(SetSkillExpMultiple);
        _skillExpInput.onEndEdit.AddListener(SetSkillExpMultiple);

        _coinInput = transform.Find("Content/InputFunc/Gold/NumInput").GetComponent<TMP_InputField>();
        _coinInput.onValueChanged.RemoveAllListeners();
        _coinInput.onValueChanged.AddListener((string input) =>
        {
            if (!long.TryParse(input, out long value))
                _coinInput.text = _coinInput.m_OriginalText;
            else
            {
                var inventory = MonoSingleton<PlayerTeamManager>.Instance.TeamInventory;
                inventory.SetCurrency(CurrencyType.Coin, value * 1000);
            }
        });

        var walkspeedSlider = transform.Find("Content/SliderFunc/WalkSpeed/Slider");
        walkspeedSlider.Find("Text").gameObject.AddComponent<SliderAmountText>();
        var walkSlider = walkspeedSlider.GetComponent<Slider>();
        WalkSpeed = Mathf.Clamp(ConfigManager.WalkSpeed.Value, (int)walkSlider.minValue, (int)walkSlider.maxValue);
        walkSlider.value = WalkSpeed;
        walkSlider.onValueChanged.AddListener((float value) =>
        {
            WalkSpeed = (int)value;
            SaveConfig(ConfigManager.WalkSpeed, WalkSpeed);
            //var player = RoamingManager.Instance?.player;
            //if (player == null) return;

            //if (!player.SpeedKey.ContainsKey("toybox")) {
            //    player.SpeedKey.Add("toybox", value);
            //}
            //else {
            //    player.SpeedKey["toybox"] = value;
            //}
        });

        _battleSpeedSlider = transform.Find("Content/SliderFunc/BattleSpeed/Slider").GetComponent<Slider>();
        _battleSpeedSlider.transform.Find("Text").gameObject.AddComponent<SliderAmountText>();
        BattleSpeed = Mathf.Clamp(ConfigManager.BattleSpeed.Value, (int)_battleSpeedSlider.minValue, (int)_battleSpeedSlider.maxValue);
        _battleSpeedSlider.value = BattleSpeed;
        _battleSpeedSlider.onValueChanged.AddListener((float value) =>
        {
            GameTimer.Instance.AddOrSetTimeScale(this, value);
            BattleSpeed = (int)value;
            SaveConfig(ConfigManager.BattleSpeed, BattleSpeed);
        });

        var buttonAchievements = transform.Find("Content/ButtonFunc/Achievement").gameObject;
        buttonAchievements.AddComponent<FadeButtonWrapper>();
        buttonAchievements.GetComponent<Button>().onClick.AddListener(() =>
        {
            var achievementDB = BaseDataClass.GetGameData<AchievementDataScriptObject>().data;
            foreach (var id in achievementDB.Keys)
            {
                MonoSingleton<AchievementManager>.Instance.Complate(id);
            }
        });

        var buttonRecover = transform.Find("Content/ButtonFunc/Recover").gameObject;
        buttonRecover.AddComponent<FadeButtonWrapper>();
        buttonRecover.GetComponent<Button>().onClick.AddListener(RecoverAll);

        var toggleKeyUI = transform.Find("Content/ConfigFunc/PanelToggle").gameObject.AddComponent<InputKeyUGUI>();
        var speedUpKeyUI = transform.Find("Content/ConfigFunc/SpeedupToggle").gameObject.AddComponent<InputKeyUGUI>();
        var speedDownKeyUI = transform.Find("Content/ConfigFunc/SpeeddownToggle").gameObject.AddComponent<InputKeyUGUI>();
        var recoverKeyUI = transform.Find("Content/ConfigFunc/Recover").gameObject.AddComponent<InputKeyUGUI>();

        BindInputKey(toggleKeyUI, ConfigManager.Canvas_Toggle);
        BindInputKey(speedUpKeyUI, ConfigManager.SpeedUp_Toggle);
        BindInputKey(speedDownKeyUI, ConfigManager.SpeedDown_Toggle);
        BindInputKey(recoverKeyUI, ConfigManager.Recover_Toggle);

        ApplyLabels();
    }

    public void ApplyLabels()
    {
        SetLabel("Content/SwitchFunc/TimeFreeze", "시간 일시정지");
        SetLabel("Content/SwitchFunc/Recover", "전투 후 상태 회복");
        SetLabel("Content/SwitchFunc/NoCombat", "인카운터 전투 미발생");
        SetLabel("Content/SwitchFunc/Friendship", "선물 페이지 호감도 최대 버튼 추가");
        SetLabel("Content/SwitchFunc/EnableAchievement", "모드 업적 해제 영향 없음");
        SetLabel("Content/SwitchFunc/UltimateMartial", "무공 수량 무제한");

        SetLabel("Content/SliderFunc/WalkSpeed", "이동 속도");
        SetLabel("Content/SliderFunc/BattleSpeed", "전투 속도");

        SetLabel("Content/InputFunc/Gold", "금전");
        SetLabel("Content/InputFunc/SkillExp", "능력 경험치 배율");

        SetLabel("Content/ButtonFunc/Achievement", "업적 해제");
        SetLabel("Content/ButtonFunc/Recover", "상태 회복");

        SetLabel("Content/ConfigFunc/PanelToggle", "패널 표시/숨기기");
        SetLabel("Content/ConfigFunc/SpeedupToggle", "게임 속도 증가");
        SetLabel("Content/ConfigFunc/SpeeddownToggle", "게임 속도 감소");
        SetLabel("Content/ConfigFunc/Recover", "상태 회복");
    }

    private void SetLabel(string path, string label)
    {
        var target = transform.Find(path);
        if (target == null) return;

        foreach (var text in target.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.GetComponentInParent<TMP_InputField>() != null) continue;

            text.text = label;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return;
        }
    }

    private static void BindSwitch(Switch toggleSwitch, ConfigEntry<bool> config)
    {
        toggleSwitch.SetToggled(config.Value, false);
        toggleSwitch.OnChanged += value => SaveConfig(config, value);
    }

    private static void SaveConfig<T>(ConfigEntry<T> config, T value)
    {
        if (!EqualityComparer<T>.Default.Equals(config.Value, value))
        {
            config.Value = value;
        }

        ConfigManager.Handler.SaveConfig();
    }

    private void SetSkillExpMultiple(string input)
    {
        if (!int.TryParse(input, out var value))
        {
            value = 1;
        }

        ExpMultiple = Mathf.Clamp(value, 1, 1000);
        _skillExpInput.SetTextWithoutNotify(ExpMultiple.ToString());
        SaveConfig(ConfigManager.SkillExpMultiple, ExpMultiple);
    }

    private static void BindInputKey(InputKeyUGUI obj, ConfigElement config)
    {
        obj.Key = config.Value;
        obj.AllowAbortWithCancelButton = true;
        obj.OnChanged += (key, _) => config.Value = key;
    }

    private void OnEnable()
    {
        var inventory = PlayerTeamManager.Instance?.TeamInventory;
        if (inventory != null)
        {
            _coinInput.SetTextWithoutNotify((inventory.GetCurrency(CurrencyType.Coin) / 1000).ToString());
        }

        _battleSpeedSlider.value = BattleSpeed;
    }

    public static void RecoverAll()
    {
        var teamManager = PlayerTeamManager.Instance;
        if (teamManager == null) return;
        teamManager.ModifyProp("队伍体力", 100);
        teamManager.ModifyProp("队伍心情", 100);
        for (int i = 0; i < teamManager.TeamSize; i++)
        {
            teamManager.GetTeamMemberByIndex(i).FullyRecover();
        }
    }

    public static void SpeedDown()
    {
        if (Instance == null) return;
        int min = (int)Instance._battleSpeedSlider.minValue;
        Instance.BattleSpeed = Math.Max(Instance.BattleSpeed - 1, min);

        GameTimer.Instance.AddOrSetTimeScale(Instance, Instance.BattleSpeed);
        Instance._battleSpeedSlider.SetValueWithoutNotify(Instance.BattleSpeed);
        SaveConfig(ConfigManager.BattleSpeed, Instance.BattleSpeed);
    }

    public static void SpeedUp()
    {
        if (Instance == null) return;
        int max = (int)Instance._battleSpeedSlider.maxValue;
        Instance.BattleSpeed = Math.Min(Instance.BattleSpeed + 1, max);

        GameTimer.Instance.AddOrSetTimeScale(Instance, Instance.BattleSpeed);
        Instance._battleSpeedSlider.SetValueWithoutNotify(Instance.BattleSpeed);
        SaveConfig(ConfigManager.BattleSpeed, Instance.BattleSpeed);
    }

}


