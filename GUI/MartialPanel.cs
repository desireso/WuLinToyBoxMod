using TMPro;
using WuLin;
using System.IO;
using System.Text;
using GameData;

namespace HaxxToyBox.GUI;

internal static class MartialTypes
{
    public const int Internal = 0;
    public const int Fist = 1;
    public const int Sword = 2;
    public const int Blade = 3;
    public const int LongWeapon = 4;
    public const int ShortWeapon = 5;
    public const int Music = 6;
    public const int Other = 7;
}

[RegisterInIl2Cpp]
internal class MartialPanel : MonoBehaviour
{
    private ToggleGroup _roleList;
    private ToggleGroup _typeGroup;

    private InfinityScrollKungfuData _infinityScroll;

    private int _type;
    private Dictionary<int, List<KungfuData>> _classifiedKungfus = new ();

    public GameCharacterInstance Character { get; private set; }

    public MartialPanel(IntPtr ptr) : base(ptr) { }
    public static MartialPanel Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        _roleList = transform.Find("RoleList/ScrollView/Viewport/Content").GetComponent<ToggleGroup>();
        _typeGroup = transform.Find("MartialList/TypeGroup").GetComponent<ToggleGroup>();

        for (int i = 0; i < _typeGroup.transform.childCount; i++) {
            var toggle = _typeGroup.transform.GetChild(i).GetComponent<Toggle>();
            var type = i;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((bool value) => {
                if (value) {
                    UpdateMartialList(type);
                }
            });
        }
        KoreanLocalizer.SetToggleLabels(
            _typeGroup.transform,
            "내공", "권장", "검", "도", "장병", "단병", "악기", "기타"
        );

        var scrollView = transform.Find("MartialList/ScrollView").gameObject;
        var entryPrefab = transform.Find("MartialList/ScrollView/Viewport/EntryPrefab").gameObject;
        entryPrefab.AddComponent<MartialEntry>();
        _infinityScroll = scrollView.AddComponent<InfinityScrollKungfuData>();
        _infinityScroll.ItemPrefab = entryPrefab;
        _infinityScroll.Columns = 2;
        _infinityScroll.SpaceX = 10;
        _infinityScroll.SpaceY = 10;

        LoadMartialData();
    }

    private void OnEnable()
    {
        SetupRoleList();

        UpdateMartialList(MartialTypes.Internal);
    }

    private void LoadMartialData()
    {
        var kungfus = GameConfig.Instance.KungfuDataScriptObject.KungfuData;
        
        foreach(KungfuData kungfu in kungfus) {
            var type = GetMartialType(kungfu);
            if (!_classifiedKungfus.TryGetValue(type, out var kungfuList)) {
                kungfuList = new List<KungfuData>();
                _classifiedKungfus[type] = kungfuList;
            }
            kungfuList.InsertInOrder(kungfu, (a, b) => b.Rarity.CompareTo(a.Rarity));
        }
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
            toggle.onValueChanged.AddListener(delegate (bool isOn) {
                if (isOn) {
                    Character = role;
                }
            });
        }
    }

    private void UpdateMartialList(int type)
    {
        _type = type;

        if (_classifiedKungfus.TryGetValue(type, out var kungfus)) {
            _infinityScroll.Data = kungfus;
            _infinityScroll.RefreshData();
        }
        else {
            _infinityScroll.Data = new List<KungfuData>();
            _infinityScroll.RefreshData();
        }
    }

    public static int GetMartialType(KungfuData kungfu)
    {
        if (kungfu.KungfuType == KungfuType.Internal) {
            return MartialTypes.Internal;
        }

        if (kungfu.KungfuType != KungfuType.Outernal || kungfu.NeedWeaponToCast.Length == 0) {
            return MartialTypes.Other;
        }

        return kungfu.NeedWeaponToCast[0] switch {
            ItemType.Equip_Weapon_None => MartialTypes.Fist,
            ItemType.Equip_Weapon_Sword => MartialTypes.Sword,
            ItemType.Equip_Weapon_Blade => MartialTypes.Blade,
            ItemType.Equip_Weapon_Lance or ItemType.Equip_Weapon_Staff => MartialTypes.LongWeapon,
            ItemType.Equip_Weapon_Fan or ItemType.Equip_Weapon_Dagger or ItemType.Equip_Weapon_Brush => MartialTypes.ShortWeapon,
            ItemType.Equip_Weapon_Guqin or ItemType.Equip_Weapon_Flute or ItemType.Equip_Weapon_Pipa => MartialTypes.Music,
            _ => MartialTypes.Other,
        };
    }

    public static void WriteMartialToCSV(string outputPath)
    {
        var martials = GameConfig.Instance.KungfuDataScriptObject.KungfuData;

        StringBuilder csvContent = new StringBuilder();
        csvContent.AppendLine("Name,KungfuType,ItemType");

        foreach (var martial in martials) {
            string name = martial.UName;
            KungfuType kungfuType = martial.KungfuType;
            ItemType[] weaponTypes = martial.NeedWeaponToCast;

            string weaponTypeStr = string.Join(" ", weaponTypes); 

            csvContent.AppendLine($"{name},{kungfuType},{weaponTypeStr}");
        }

        File.WriteAllText(outputPath, csvContent.ToString(), Encoding.UTF8);
    }


}
