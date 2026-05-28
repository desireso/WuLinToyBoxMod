using GameData;
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
    private TMP_InputField _kungfuBattleExpInput;

    private InputKeyUGUI _toggleKeyUI;
    private InputKeyUGUI _speedUpKeyUI;
    private InputKeyUGUI _speedDownKeyUI;
    private InputKeyUGUI _recoverKeyUI;

    public int ExpMultiple = 1;
    public int KungfuBattleExpMultiple = 1;
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
        FitSwitchColumn();
        FitInputColumn();

        var expInput = transform.Find("Content/InputFunc/SkillExp/NumInput").GetComponent<TMP_InputField>();
        SetInputFuncLabel(expInput.transform.parent, "기술 EXP");
        MoveInputRow(expInput.transform.parent.GetComponent<RectTransform>(), 1);
        FitInputRow(expInput.transform.parent, expInput);
        expInput.onValueChanged.RemoveAllListeners();
        expInput.onValueChanged.AddListener((string input) => {
            int.TryParse(input, out ExpMultiple);
            ExpMultiple = Mathf.Clamp(ExpMultiple, 1, 1000);
        });

        _coinInput = transform.Find("Content/InputFunc/Gold/NumInput").GetComponent<TMP_InputField>();
        SetInputFuncLabel(_coinInput.transform.parent, "돈");
        MoveInputRow(_coinInput.transform.parent.GetComponent<RectTransform>(), 0);
        FitInputRow(_coinInput.transform.parent, _coinInput);
        _coinInput.onValueChanged.RemoveAllListeners();
        _coinInput.onValueChanged.AddListener((string input) => {
            if (!long.TryParse(input, out long value))
                _coinInput.text = _coinInput.m_OriginalText;
            else {
                var inventory = MonoSingleton<PlayerTeamManager>.Instance.TeamInventory;
                inventory.SetCurrency(CurrencyType.Coin, value * 1000);
            }
        });

        _kungfuBattleExpInput = CreateKungfuBattleExpInput(expInput.transform.parent);
        _kungfuBattleExpInput.onValueChanged.RemoveAllListeners();
        _kungfuBattleExpInput.onEndEdit.RemoveAllListeners();
        _kungfuBattleExpInput.onValueChanged.AddListener(SetKungfuBattleExpMultiple);
        _kungfuBattleExpInput.onEndEdit.AddListener(SetKungfuBattleExpMultiple);

        var walkspeedSlider = transform.Find("Content/SliderFunc/WalkSpeed/Slider");
        walkspeedSlider.Find("Text").gameObject.AddComponent<SliderAmountText>();
        walkspeedSlider.GetComponent<Slider>().onValueChanged.AddListener((float value) => {
            WalkSpeed = (int)value;
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
        _battleSpeedSlider.onValueChanged.AddListener((float value) => {
            GameTimer.Instance.AddOrSetTimeScale(this, value);
            BattleSpeed = (int)value;
        });

        var buttonAchievements = transform.Find("Content/ButtonFunc/Achievement").gameObject;
        buttonAchievements.AddComponent<FadeButtonWrapper>();
        FitButtonText(buttonAchievements, 22);
        buttonAchievements.GetComponent<Button>().onClick.AddListener(() => {
            var achievementDB = BaseDataClass.GetGameData<AchievementDataScriptObject>().data;
            foreach (var id in achievementDB.Keys) {
                MonoSingleton<AchievementManager>.Instance.Complate(id);
            }
        });

        var buttonRecover = transform.Find("Content/ButtonFunc/Recover").gameObject;
        buttonRecover.AddComponent<FadeButtonWrapper>();
        FitButtonText(buttonRecover, 24);
        buttonRecover.GetComponent<Button>().onClick.AddListener(RecoverAll);

        _toggleKeyUI = transform.Find("Content/ConfigFunc/PanelToggle").gameObject.AddComponent<InputKeyUGUI>();
        _speedUpKeyUI = transform.Find("Content/ConfigFunc/SpeedupToggle").gameObject.AddComponent<InputKeyUGUI>();
        _speedDownKeyUI = transform.Find("Content/ConfigFunc/SpeeddownToggle").gameObject.AddComponent<InputKeyUGUI>();
        _recoverKeyUI = transform.Find("Content/ConfigFunc/Recover").gameObject.AddComponent<InputKeyUGUI>();

        BindInputKey(_toggleKeyUI, ConfigManager.Canvas_Toggle);
        BindInputKey(_speedUpKeyUI, ConfigManager.SpeedUp_Toggle);
        BindInputKey(_speedDownKeyUI, ConfigManager.SpeedDown_Toggle);
        BindInputKey(_recoverKeyUI, ConfigManager.Recover_Toggle);
    }

    private TMP_InputField CreateKungfuBattleExpInput(Transform skillExpRow)
    {
        var parent = skillExpRow.parent;
        var row = Instantiate(skillExpRow.gameObject, parent, false);
        row.name = "KungfuBattleExp";
        row.transform.SetSiblingIndex(skillExpRow.GetSiblingIndex() + 1);
        SetInputFuncLabel(row.transform, "무공 EXP");
        MoveInputRow(row.GetComponent<RectTransform>(), 2);

        var input = row.transform.Find("NumInput").GetComponent<TMP_InputField>();
        FitInputRow(row.transform, input);
        input.SetTextWithoutNotify(KungfuBattleExpMultiple.ToString());
        return input;
    }

    private static void MoveInputRow(RectTransform row, int index)
    {
        if (row == null) return;

        row.anchoredPosition = new Vector2(297.1f, -50f - index * 110f);
        row.sizeDelta = new Vector2(row.sizeDelta.x, 100f);
    }

    private void FitSwitchColumn()
    {
        var group = transform.Find("Content/SwitchFunc")?.GetComponent<RectTransform>();
        if (group != null) {
            group.sizeDelta = new Vector2(590f, group.sizeDelta.y);
        }

        FitSwitchRow("TimeFreeze");
        FitSwitchRow("Recover");
        FitSwitchRow("NoCombat");
        FitSwitchRow("Friendship");
        FitSwitchRow("EnableAchievement");
        FitSwitchRow("UltimateMartial");
    }

    private void FitSwitchRow(string rowName)
    {
        var row = transform.Find($"Content/SwitchFunc/{rowName}")?.GetComponent<RectTransform>();
        if (row == null) return;

        row.sizeDelta = new Vector2(590f, row.sizeDelta.y);

        var label = GetLabel(row.transform);
        var labelRect = label?.GetComponent<RectTransform>();
        if (label != null) {
            label.fontSize = 32f;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }

        if (labelRect != null) {
            labelRect.sizeDelta = new Vector2(360f, labelRect.sizeDelta.y);
            labelRect.anchoredPosition = new Vector2(180f, labelRect.anchoredPosition.y);
        }

        var switchRect = row.transform.Find("Switch")?.GetComponent<RectTransform>();
        if (switchRect != null) {
            switchRect.anchoredPosition = new Vector2(-205f, switchRect.anchoredPosition.y);
        }
    }

    private void FitInputColumn()
    {
        var group = transform.Find("Content/InputFunc")?.GetComponent<RectTransform>();
        if (group == null) return;

        group.anchoredPosition = new Vector2(720f, 342f);
        group.sizeDelta = new Vector2(590f, 300f);
    }

    private static void SetInputFuncLabel(Transform row, string label)
    {
        foreach (var text in row.GetComponentsInChildren<TextMeshProUGUI>(true)) {
            if (text.GetComponentInParent<TMP_InputField>() != null) continue;

            text.text = label;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return;
        }
    }

    private static void FitInputRow(Transform row, TMP_InputField input)
    {
        if (row == null || input == null) return;

        foreach (var text in row.GetComponentsInChildren<TextMeshProUGUI>(true)) {
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;

            bool isInputText = text.GetComponentInParent<TMP_InputField>() != null;
            text.fontSize = isInputText ? 24f : 32f;
            text.alignment = isInputText ? TextAlignmentOptions.Center : TextAlignmentOptions.MidlineLeft;

            var textRect = text.GetComponent<RectTransform>();
            if (textRect != null) {
                textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x, 0f);
                textRect.sizeDelta = new Vector2(textRect.sizeDelta.x, 100f);
            }
        }

        var inputRect = input.GetComponent<RectTransform>();
        if (inputRect != null) {
            inputRect.sizeDelta = new Vector2(240f, 56f);
            inputRect.anchoredPosition = new Vector2(inputRect.anchoredPosition.x, 50f);
        }

        if (input.textViewport != null) {
            input.textViewport.offsetMin = new Vector2(input.textViewport.offsetMin.x, 0f);
            input.textViewport.offsetMax = new Vector2(input.textViewport.offsetMax.x, 0f);
        }

        if (input.textComponent != null) {
            input.textComponent.alignment = TextAlignmentOptions.Center;
            input.textComponent.fontSize = 24f;

            var textRect = input.textComponent.GetComponent<RectTransform>();
            if (textRect != null) {
                textRect.offsetMin = new Vector2(textRect.offsetMin.x, 0f);
                textRect.offsetMax = new Vector2(textRect.offsetMax.x, 0f);
                textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x, 0f);
            }
        }
    }

    private static TextMeshProUGUI GetLabel(Transform row)
    {
        if (row == null) return null;

        foreach (var text in row.GetComponentsInChildren<TextMeshProUGUI>(true)) {
            if (text.GetComponentInParent<TMP_InputField>() != null) continue;
            return text;
        }

        return null;
    }

    private static void FitButtonText(GameObject button, float fontSize)
    {
        var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text == null) return;

        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.fontSize = fontSize;
    }

    private void SetKungfuBattleExpMultiple(string input)
    {
        if (!int.TryParse(input, out var value)) {
            value = 1;
        }

        KungfuBattleExpMultiple = Mathf.Clamp(value, 1, 1000);
        _kungfuBattleExpInput.SetTextWithoutNotify(KungfuBattleExpMultiple.ToString());
        ToyBox.LogMessage($"Kungfu battle exp multiple: {KungfuBattleExpMultiple}");
    }
    private void BindInputKey(InputKeyUGUI obj, ConfigElement config)
    {
        obj.Key = config.Value;
        obj.AllowAbortWithCancelButton = true;
        obj.OnChanged += (KeyCode key, KeyCode modifierKey) => config.Value = key;
    }

    private void OnEnable()
    {
        var inventory = PlayerTeamManager.Instance?.TeamInventory;
        if (inventory != null) {
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
        for (int i = 0; i < teamManager.TeamSize; i++) {
            teamManager.GetTeamMemberByIndex(i).FullyRecover();
        }
    }

    public static void SpeedDown()
    {
        if (Instance == null) return;
        int min = (int)Instance._battleSpeedSlider.minValue;
        Instance.BattleSpeed = Math.Max(Instance.BattleSpeed-1, min);

        GameTimer.Instance.AddOrSetTimeScale(Instance, Instance.BattleSpeed);
    }

    public static void SpeedUp()
    {
        if (Instance == null) return;
        int max = (int)Instance._battleSpeedSlider.maxValue;
        Instance.BattleSpeed = Math.Min(Instance.BattleSpeed+1, max);

        GameTimer.Instance.AddOrSetTimeScale(Instance, Instance.BattleSpeed);
    }

}


