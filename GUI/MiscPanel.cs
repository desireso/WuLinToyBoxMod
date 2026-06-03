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
    private Slider _walkSpeedSlider;
    private TMP_InputField _coinInput;
    private TMP_InputField _skillExpInput;
    private TMP_InputField _fixedItemCountInput;
    private InputKeyUGUI _toggleKeyUI;
    private InputKeyUGUI _speedUpKeyUI;
    private InputKeyUGUI _speedDownKeyUI;
    private InputKeyUGUI _recoverKeyUI;

    public int ExpMultiple = 1;
    public int WalkSpeed = 1;
    public int BattleSpeed = 1;
    public int FixedItemCount = 1;

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

        EnsureFixedItemCountControls();

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

        _fixedItemCountInput = transform.Find("Content/InputFunc/FixedItemCount/NumInput").GetComponent<TMP_InputField>();
        FixedItemCount = Mathf.Clamp(ConfigManager.FixedItemCount.Value, 0, 9999);
        _fixedItemCountInput.SetTextWithoutNotify(FixedItemCount.ToString());
        _fixedItemCountInput.onValueChanged.RemoveAllListeners();
        _fixedItemCountInput.onEndEdit.RemoveAllListeners();
        _fixedItemCountInput.onValueChanged.AddListener(SetFixedItemCount);
        _fixedItemCountInput.onEndEdit.AddListener(SetFixedItemCount);

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
        _walkSpeedSlider = walkspeedSlider.GetComponent<Slider>();
        WalkSpeed = Mathf.Clamp(ConfigManager.WalkSpeed.Value, (int)_walkSpeedSlider.minValue, (int)_walkSpeedSlider.maxValue);
        _walkSpeedSlider.SetValueWithoutNotify(WalkSpeed);
        _walkSpeedSlider.onValueChanged.AddListener((float value) =>
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
        _battleSpeedSlider.SetValueWithoutNotify(BattleSpeed);
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

        ArrangeMiscPanelLayout();

        _toggleKeyUI = transform.Find("Content/ConfigFunc/PanelToggle").gameObject.AddComponent<InputKeyUGUI>();
        _speedUpKeyUI = transform.Find("Content/ConfigFunc/SpeedupToggle").gameObject.AddComponent<InputKeyUGUI>();
        _speedDownKeyUI = transform.Find("Content/ConfigFunc/SpeeddownToggle").gameObject.AddComponent<InputKeyUGUI>();
        _recoverKeyUI = transform.Find("Content/ConfigFunc/Recover").gameObject.AddComponent<InputKeyUGUI>();

        BindInputKey(_toggleKeyUI, ConfigManager.Canvas_Toggle);
        BindInputKey(_speedUpKeyUI, ConfigManager.SpeedUp_Toggle);
        BindInputKey(_speedDownKeyUI, ConfigManager.SpeedDown_Toggle);
        BindInputKey(_recoverKeyUI, ConfigManager.Recover_Toggle);

        RefreshFromConfig(false);

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

        SetLabel("Content/SwitchFunc/UltimateMartial/LowerLeftPanel/Gold", "금전");
        SetLabel("Content/SwitchFunc/UltimateMartial/LowerLeftPanel/SkillExp", "능력 경험치 배율");
        SetLabel("Content/SwitchFunc/UltimateMartial/LowerLeftPanel/FixedItemCount", "아이템 개수 고정");

        SetLabel("Content/SwitchFunc/UltimateMartial/LowerRightPanel/ActionAchievement", "업적 해제");
        SetLabel("Content/SwitchFunc/UltimateMartial/LowerRightPanel/ActionRecover", "상태 회복");

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

    private void EnsureFixedItemCountControls()
    {
        var inputParent = transform.Find("Content/InputFunc");
        var skillExpRow = inputParent?.Find("SkillExp");
        if (inputParent != null && skillExpRow != null && inputParent.Find("FixedItemCount") == null)
        {
            var row = UnityEngine.Object.Instantiate(skillExpRow.gameObject, inputParent);
            row.name = "FixedItemCount";
            row.transform.SetSiblingIndex(skillExpRow.GetSiblingIndex() + 1);
        }
    }

    private void ArrangeMiscPanelLayout()
    {
        ArrangeInputRows();
        ArrangeActionButtons();
    }

    private void ArrangeInputRows()
    {
        var ultimateMartialRow = FindRect("Content/SwitchFunc/UltimateMartial");
        var goldRow = FindRect("Content/InputFunc/Gold");
        var skillExpRow = FindRect("Content/InputFunc/SkillExp");
        var fixedRow = FindRect("Content/InputFunc/FixedItemCount");
        if (ultimateMartialRow == null || goldRow == null || skillExpRow == null || fixedRow == null) return;

        var leftPanel = EnsurePanel(ultimateMartialRow, "LowerLeftPanel");
        var rightPanel = EnsurePanel(ultimateMartialRow, "LowerRightPanel");
        ConfigurePanel(leftPanel, new Vector2(0, -82), new Vector2(650, 300), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 1));
        ConfigurePanel(rightPanel, new Vector2(665, -82), new Vector2(520, 140), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 1));

        goldRow.SetParent(leftPanel, false);
        goldRow.name = "Gold";
        goldRow.SetSiblingIndex(0);
        PlacePanelRow(goldRow, 0);
        ArrangeInputRow(goldRow, "금전", new Vector2(0, 0), new Vector2(470, 0), new Vector2(180, 64));

        skillExpRow.SetParent(leftPanel, false);
        skillExpRow.name = "SkillExp";
        skillExpRow.SetSiblingIndex(goldRow.GetSiblingIndex() + 1);
        PlacePanelRow(skillExpRow, 82);
        ArrangeInputRow(skillExpRow, "능력 경험치 배율", new Vector2(0, 0), new Vector2(470, 0), new Vector2(180, 64));

        fixedRow.SetParent(leftPanel, false);
        fixedRow.name = "FixedItemCount";
        fixedRow.SetSiblingIndex(skillExpRow.GetSiblingIndex() + 1);
        PlacePanelRow(fixedRow, 164);
        ArrangeInputRow(fixedRow, "아이템 개수 고정", new Vector2(0, 0), new Vector2(470, 0), new Vector2(180, 64));
    }

    private void ArrangeInputRow(RectTransform row, string label, Vector2 labelPosition, Vector2 inputPosition, Vector2 inputSize)
    {
        row.sizeDelta = new Vector2(650, row.sizeDelta.y);

        var rowLabel = FirstLabel(row);
        if (rowLabel != null)
        {
            rowLabel.text = label;
            rowLabel.enableWordWrapping = false;
            rowLabel.overflowMode = TextOverflowModes.Overflow;
            rowLabel.alignment = TextAlignmentOptions.Left;
            PlaceRowLabel(rowLabel.GetComponent<RectTransform>(), labelPosition, new Vector2(360, 64));
        }

        var input = FindChildRect(row, "NumInput");
        PlaceRowInput(input, inputPosition, inputSize);
    }

    private void ArrangeActionButtons()
    {
        var rightPanel = FindRect("Content/SwitchFunc/UltimateMartial/LowerRightPanel");
        var achievementButton = FindRect("Content/ButtonFunc/Achievement");
        var recoverButton = FindRect("Content/ButtonFunc/Recover");
        if (rightPanel == null || achievementButton == null || recoverButton == null) return;

        MoveButtonToPanel(achievementButton, rightPanel, "ActionAchievement", new Vector2(125, 20));
        MoveButtonToPanel(recoverButton, rightPanel, "ActionRecover", new Vector2(375, 20));
    }

    private RectTransform FindRect(string path)
    {
        return transform.Find(path)?.GetComponent<RectTransform>();
    }

    private static RectTransform FindChildRect(Transform parent, string path)
    {
        return parent?.Find(path)?.GetComponent<RectTransform>();
    }

    private static RectTransform EnsurePanel(Transform parent, string name)
    {
        var existing = parent.Find(name)?.GetComponent<RectTransform>();
        if (existing != null) return existing;

        var panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        return panel.AddComponent<RectTransform>();
    }

    private static void ConfigurePanel(RectTransform panel, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;
        panel.pivot = pivot;
        panel.anchoredPosition = anchoredPosition;
        panel.sizeDelta = size;
        panel.localScale = Vector3.one;
    }

    private static void PlacePanelRow(RectTransform row, float y)
    {
        row.anchorMin = new Vector2(0, 1);
        row.anchorMax = new Vector2(0, 1);
        row.pivot = new Vector2(0, 0.5f);
        row.anchoredPosition = new Vector2(0, -50 - y);
        row.sizeDelta = new Vector2(650, 100);
        row.localScale = Vector3.one;
    }

    private static void MoveButtonToPanel(RectTransform button, Transform parent, string name, Vector2 anchoredPosition)
    {
        button.SetParent(parent, false);
        button.name = name;
        button.anchorMin = new Vector2(0, 0.5f);
        button.anchorMax = new Vector2(0, 0.5f);
        button.pivot = new Vector2(0.5f, 0.5f);
        button.anchoredPosition = anchoredPosition;
        button.sizeDelta = new Vector2(220, 54);
        button.localScale = Vector3.one;
    }

    private static void PlaceRowLabel(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null) return;

        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static void PlaceRowInput(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        if (rect == null) return;

        rect.anchorMin = new Vector2(0, 0.5f);
        rect.anchorMax = new Vector2(0, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private static TextMeshProUGUI FirstLabel(Transform target)
    {
        if (target == null) return null;

        foreach (var text in target.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.GetComponentInParent<TMP_InputField>() != null) continue;

            return text;
        }

        return null;
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

    private void SetFixedItemCount(string input)
    {
        if (!int.TryParse(input, out var value))
        {
            value = 1;
        }

        FixedItemCount = Mathf.Clamp(value, 0, 9999);
        _fixedItemCountInput.SetTextWithoutNotify(FixedItemCount.ToString());
        SaveConfig(ConfigManager.FixedItemCount, FixedItemCount);
    }

    private static void BindInputKey(InputKeyUGUI obj, ConfigElement config)
    {
        obj.Key = config.Value;
        obj.AllowAbortWithCancelButton = true;
        obj.OnChanged += (key, _) => config.Value = key;
    }

    private void OnEnable()
    {
        RefreshFromConfig(true);

        var inventory = PlayerTeamManager.Instance?.TeamInventory;
        if (inventory != null)
        {
            _coinInput.SetTextWithoutNotify((inventory.GetCurrency(CurrencyType.Coin) / 1000).ToString());
        }
    }

    private void RefreshFromConfig(bool reloadFile)
    {
        if (reloadFile) {
            ConfigManager.Handler.ReloadConfig();
        }

        _timeFreezeSwitch?.SetToggled(ConfigManager.TimeFreezeEnabled.Value, false);
        _recoverSwitch?.SetToggled(ConfigManager.RecoverEnabled.Value, false);
        _noCombatSwitch?.SetToggled(ConfigManager.NoCombatEnabled.Value, false);
        _relationSwitch?.SetToggled(ConfigManager.RelationEnabled.Value, false);
        _enableAchieveSwitch?.SetToggled(ConfigManager.EnableAchievement.Value, false);
        _ultimateMartialSwitch?.SetToggled(ConfigManager.UltimateMartial.Value, false);
        ExpMultiple = Mathf.Clamp(ConfigManager.SkillExpMultiple.Value, 1, 1000);
        _skillExpInput?.SetTextWithoutNotify(ExpMultiple.ToString());
        FixedItemCount = Mathf.Clamp(ConfigManager.FixedItemCount.Value, 0, 9999);
        _fixedItemCountInput?.SetTextWithoutNotify(FixedItemCount.ToString());

        if (_walkSpeedSlider != null) {
            WalkSpeed = Mathf.Clamp(ConfigManager.WalkSpeed.Value, (int)_walkSpeedSlider.minValue, (int)_walkSpeedSlider.maxValue);
            _walkSpeedSlider.SetValueWithoutNotify(WalkSpeed);
        }

        if (_battleSpeedSlider != null) {
            BattleSpeed = Mathf.Clamp(ConfigManager.BattleSpeed.Value, (int)_battleSpeedSlider.minValue, (int)_battleSpeedSlider.maxValue);
            _battleSpeedSlider.SetValueWithoutNotify(BattleSpeed);
        }

        RefreshInputKey(_toggleKeyUI, ConfigManager.Canvas_Toggle);
        RefreshInputKey(_speedUpKeyUI, ConfigManager.SpeedUp_Toggle);
        RefreshInputKey(_speedDownKeyUI, ConfigManager.SpeedDown_Toggle);
        RefreshInputKey(_recoverKeyUI, ConfigManager.Recover_Toggle);
    }

    private static void RefreshInputKey(InputKeyUGUI obj, ConfigElement config)
    {
        if (obj == null || config == null) return;

        obj.Key = config.Value;
        obj.ModifierKey = KeyCode.None;
        obj.Refresh();
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


