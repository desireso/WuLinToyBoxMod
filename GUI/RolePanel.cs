using TMPro;
using WuLin;

namespace HaxxToyBox.GUI;

[RegisterInIl2Cpp]
internal class RolePanel : MonoBehaviour
{
    private ToggleGroup _roleList;
    
    public GameCharacterInstance Character = null;

    public GameObject LeftInfoGroup;
    public GameObject RightInfoGroup;
    public GameObject BottomInfoGroup;
    public GameObject AdditionInfoGroup;

    public Transform TraitList;
    public GameObject TraitEntryPrefab;

    readonly string[] leftRoleInfoKeys = {
        "gongji", "qinggong", "quanzhang", "shuadao", "duanbing", "mingzhong", "baoji", "yishu", "anqi", "wuxuechangshi"
    };

    readonly string[] rightRoleInfoKeys = {
        "fangyu", "jiqi", "yujian", "changbing", "yinlv", "shanbi", "gedang", "dushu", "hubo", "shizhannengli"
    };

    readonly string[] additionRoleInfoKeys = {
        "bili", "tizhi", "minjie", "wuxing", "fuyuan"
    };

    readonly string[] bottomRoleInfoKeys = {
        "rende", "yiqi", "lijie", "xinyong", "zhihui","yongqi"
    };

    readonly string[] percentageKeys = {
        "mingzhong", "baoji", "shanbi", "gedang", "hubo"
    };

    readonly Dictionary<string, string> keyToLabelMap = new() {
        {"gongji", "攻击"}, {"qinggong", "轻功"}, {"quanzhang", "拳掌"}, {"shuadao", "耍刀"},
        {"duanbing", "短兵"}, {"mingzhong", "命中"}, {"baoji", "暴击"}, {"yishu", "医术"},
        {"anqi", "暗器"}, {"wuxuechangshi", "武学常识"}, {"hp", "生命"}, {"mp", "内力"},
        {"point", "冲穴点数"}, {"exp", "经验"}, {"lv", "等级"}, {"fangyu", "防御"}, {"jiqi", "集气速度"},
        {"yujian", "御剑"}, {"changbing", "长兵"}, {"yinlv", "乐器"}, {"shanbi", "闪避"},
        {"gedang", "格挡"}, {"dushu", "毒术"}, {"hubo", "互搏"}, {"shizhannengli", "实战能力"},
        {"bili", "臂力"}, {"tizhi", "体质"}, {"minjie", "敏捷"}, {"wuxing", "悟性"},
        {"fuyuan", "福缘"}, {"rende", "仁德"}, {"yiqi", "义气"}, {"lijie", "礼节"},
        {"xinyong", "信用"}, {"zhihui", "智慧"}, {"yongqi", "勇气"}, {"replv", "名声级别"},
        {"repexp", "名声经验"}, {"coin", "金币"}
     };

    readonly Dictionary<string, string> keyToKoreanLabelMap = new() {
        {"gongji", "공격"}, {"qinggong", "경공"}, {"quanzhang", "권장"}, {"shuadao", "도법"},
        {"duanbing", "단병"}, {"mingzhong", "명중"}, {"baoji", "치명"}, {"yishu", "의술"},
        {"anqi", "암기"}, {"wuxuechangshi", "무학 상식"}, {"hp", "생명"}, {"mp", "내력"},
        {"point", "혈도 점수"}, {"exp", "경험"}, {"lv", "등급"}, {"fangyu", "방어"}, {"jiqi", "집기 속도"},
        {"yujian", "어검"}, {"changbing", "장병"}, {"yinlv", "음률"}, {"shanbi", "회피"},
        {"gedang", "막기"}, {"dushu", "독술"}, {"hubo", "연격"}, {"shizhannengli", "실전 능력"},
        {"bili", "근력"}, {"tizhi", "체질"}, {"minjie", "민첩"}, {"wuxing", "오성"},
        {"fuyuan", "행운"}, {"rende", "인덕"}, {"yiqi", "의리"}, {"lijie", "예절"},
        {"xinyong", "신용"}, {"zhihui", "지혜"}, {"yongqi", "용기"}, {"replv", "명성 등급"},
        {"repexp", "명성 경험"}, {"coin", "돈"}
    };

    public static RolePanel Instance { get; private set; }

    public RolePanel(IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        Instance = this;

        _roleList = transform.Find("RoleList/ScrollView/Viewport/Content").GetComponent<ToggleGroup>();
        LeftInfoGroup = transform.Find("RoleInfo/LeftInfo").gameObject;
        RightInfoGroup = transform.Find("RoleInfo/RightInfo").gameObject;
        BottomInfoGroup = transform.Find("RoleInfo/BottomInfo").gameObject;
        AdditionInfoGroup = transform.Find("RoleInfo/AdditionInfo").gameObject;

        TraitList = transform.Find("Traits/Viewport/Content");
        TraitEntryPrefab = transform.Find("Traits/Viewport/EntryPrefab").gameObject;
        TraitEntryPrefab.AddComponent<TraitDelEntry>();

        UpdateInfoLabels();
    }

    private void SetupRoleList()
    {
        var numRole = PlayerTeamManager.Instance.TeamSize;
        
        for (int i = 0; i < _roleList.transform.childCount; i++) {
            var entry = _roleList.transform.GetChild(i);
            entry.gameObject.SetActive(i < numRole);
            if (i >= numRole) continue;

            var role = PlayerTeamManager.Instance.GetTeamMemberByIndex(i);

            entry.Find("Content/Avatar/Avatar").GetComponent<Image>().sprite = role.GetPortrait(GameCharacterInstance.PortraitType.Small);
            entry.Find("Content/NameText").GetComponent<TextMeshProUGUI>().text = role.FullName;

            var toggle = entry.GetComponent<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener(delegate (bool isOn){
                if (isOn) {
                    UpdateRoleInfo(role);
                }
            });
        }
    }

    private void OnEnable()
    {
        SetupRoleList();

        UpdateRoleInfo();
    }

    public void UpdateRoleInfo(GameCharacterInstance charac = null)
    {
        if (charac != null)
            Character = charac;
        //ToyBox.LogMessage($"{character.FullName} Selected.");

        UpdateInfoGroup(LeftInfoGroup, leftRoleInfoKeys);
        UpdateInfoGroup(RightInfoGroup, rightRoleInfoKeys);
        UpdateInfoGroup(BottomInfoGroup, bottomRoleInfoKeys);
        UpdateInfoGroup(AdditionInfoGroup, additionRoleInfoKeys);

        UpdateTraitList();
    }

    private void UpdateInfoGroup(GameObject group, string[] keys)
    {
        int iterations = Mathf.Min(group.transform.childCount, keys.Length);
        for (int i = 0; i < iterations; i++) {
            Transform child = group.transform.GetChild(i);
            SetInfoLabel(child, keyToKoreanLabelMap[keys[i]]);

            if (Character == null) continue;

            var inputObj = child.GetComponentInChildren<TMP_InputField>();
            if (inputObj == null) continue;

            string formatstr = percentageKeys.Contains(keys[i]) ? "F3" : "";

            string propKey = keyToLabelMap[keys[i]];
            var propSource = GameCharacterInstance.FinalPropSource.Origin;
            inputObj.text = Character.GetFinalPropAsDecimal(propKey, propSource).ToString(formatstr);

            inputObj.onValueChanged.RemoveAllListeners();
            inputObj.onValueChanged.AddListener(delegate (string input) {
                bool ret = ChangeProperty(propKey, input);
                if (!ret) inputObj.text = inputObj.m_OriginalText;
            });
        }
    }

    private void UpdateInfoLabels()
    {
        UpdateInfoLabels(LeftInfoGroup, leftRoleInfoKeys);
        UpdateInfoLabels(RightInfoGroup, rightRoleInfoKeys);
        UpdateInfoLabels(BottomInfoGroup, bottomRoleInfoKeys);
        UpdateInfoLabels(AdditionInfoGroup, additionRoleInfoKeys);
    }

    private void UpdateInfoLabels(GameObject group, string[] keys)
    {
        int iterations = Mathf.Min(group.transform.childCount, keys.Length);
        for (int i = 0; i < iterations; i++) {
            SetInfoLabel(group.transform.GetChild(i), keyToKoreanLabelMap[keys[i]]);
        }
    }

    private static void SetInfoLabel(Transform row, string label)
    {
        foreach (var text in row.GetComponentsInChildren<TextMeshProUGUI>(true)) {
            if (text.GetComponentInParent<TMP_InputField>() != null) continue;

            text.text = label;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return;
        }
    }

    private bool ChangeProperty(string propKey, string input)
    {
        if (Character == null || 
            !Il2CppSystem.Decimal.TryParse(input, out Il2CppSystem.Decimal value)) {
            return false;
        }

        if (!Character.m_originProps.ContainsKey(propKey))
            Character.m_originProps.Add(propKey, 0);

        var diff = value - Character.m_originProps[propKey];
        Character.ChangeOriginProp(propKey, diff);

        return true;
    }

    private void UpdateTraitList()
    {
        if (Character == null) return;

        var traits = Character.GetAllTrait();

        for (int i = 0; i < traits.Count; i++) {
            var trait = traits[i];
            GameObject entry;
            if (i >= TraitList.childCount) {
                entry = Instantiate(TraitEntryPrefab, TraitList);
            }
            else {
                entry = TraitList.GetChild(i).gameObject;
            }
            entry.SetActive(true);
            entry.GetComponent<TraitDelEntry>().SetTrait(trait);
        }

        for (int i = traits.Count; i < TraitList.childCount; i++) {
            TraitList.GetChild(i).gameObject.SetActive(false);
        }

    }
}
