using GameData;
using TMPro;
using WuLin;

namespace HaxxToyBox.GUI;

[RegisterInIl2Cpp]
public class ItemPanel : MonoBehaviour
{
    private TMP_InputField _numberInput;
    private TMP_InputField _searchInput;
    private ToggleGroup _typeGroup;
    private ToggleGroup _subtypeGroup;

    private InfinityScrollItemData _infinityScroll;

    private ItemType[][] _typeList = { 
        new ItemType[] { ItemType.Equip,
            ItemType.Equip_Weapon, ItemType.Equip_Armor, ItemType.Equip_Amulet },
        new ItemType[] { ItemType.KungfuBook,
            ItemType.KungfuBook_Outer,
            ItemType.KungfuBook_Inner },
        new ItemType[] { ItemType.Consumeable_Recipe|ItemType.Consumeable_Edible,
            ItemType.Consumeable_Edible_Meal|ItemType.Consumeable_Edible_Fruit,
            ItemType.Consumeable_Edible_Elixir, ItemType.Consumeable_Edible_Medicine, ItemType.Consumeable_Recipe},
        new ItemType[] { ItemType.Consumeable_Material },
        new ItemType[] { ItemType.Misc_Map },
        new ItemType[] { ItemType.Misc^ItemType.Misc_Map },
    };
    private Dictionary<ItemType, List<ItemData>> _classifiedItems = new ();

    private int _selectedType = 0;
    private int _selectedSubtype = 0;
    private string _searchKeyword = "";
    private string _lastRawSearchText = "";

    public List<ItemData> ItemList = new ();
    public int Number = 1;

    public static ItemPanel Instance { get; private set; }

    public ItemPanel(IntPtr ptr) : base(ptr) { }

    private void Awake()
    {
        Instance = this;

        _typeGroup = transform.Find("TypeGroup/Toggles").GetComponent<ToggleGroup>();
        _subtypeGroup = transform.Find("SubtypeGroup").GetComponent<ToggleGroup>();
        for (int i = 0; i < _typeGroup.transform.childCount; i++) {
            var toggle = _typeGroup.transform.GetChild(i).GetComponent<Toggle>();
            toggle.onValueChanged.RemoveAllListeners();
            var type = i;
            toggle.onValueChanged.AddListener((bool value) => {
                if (value) {
                    UpdateItemList(type);
                    UpdateSubToggles(type);
                }
            });
        }
        KoreanLocalizer.SetToggleLabels(
            _typeGroup.transform,
            "장비", "무공서", "소비품", "재료", "지도", "기타"
        );

        for (int i = 0; i < _subtypeGroup.transform.childCount; i++) {
            var toggle = _subtypeGroup.transform.GetChild(i).GetComponent<Toggle>();
            int subtype = i;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((bool value) => {
                if (value) {
                    UpdateItemList(_selectedType, subtype);
                }
            });
        }
        
        _numberInput = transform.Find("NumInput").GetComponent<TMP_InputField>();
        SetNearbyLabel(_numberInput.transform, "수량");
        _numberInput.onValueChanged.RemoveAllListeners();
        _numberInput.onValueChanged.AddListener((string input) => {
            int.TryParse(input, out Number);
            Number = Mathf.Clamp(Number, 1, 9999);
        });

        _searchInput = transform.Find("SearchInput").GetComponent<TMP_InputField>();
        _searchInput.SetTextWithoutNotify("");
        if (_searchInput.textComponent != null) {
            _searchInput.textComponent.text = "";
        }
        _searchInput.onValueChanged.RemoveAllListeners();
        _searchInput.onValueChanged.AddListener((string input) => {
            _searchKeyword = GetSearchKeyword(input);
            UpdateItemList(_selectedType, _selectedSubtype);
        });

        var scrollView = transform.Find("ScrollView").gameObject;
        var entryPrefab = transform.Find("ScrollView/Viewport/EntryPrefab").gameObject;
        entryPrefab.AddComponent<ItemEntry>();
        _infinityScroll = scrollView.AddComponent<InfinityScrollItemData>();
        _infinityScroll.ItemPrefab = entryPrefab;
        _infinityScroll.Columns = 11;
        _infinityScroll.SpaceX = 25;
        _infinityScroll.SpaceY = 25;
        
        LoadItemData();
        UpdateSubToggles(_selectedType);
        UpdateItemList(_selectedType, _selectedSubtype);
    }

    private void LateUpdate()
    {
        if (_searchInput == null) return;

        string rawSearchText = ReadRawSearchText();
        if (rawSearchText == _lastRawSearchText) return;

        _lastRawSearchText = rawSearchText;
        _searchKeyword = GetSearchKeyword(rawSearchText);
        UpdateItemList(_selectedType, _selectedSubtype);
    }

    private void LoadItemData()
    {
#if DEBUGMODE
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
#endif

        foreach (var group in _typeList) {
            ItemType mainType = group[0];
            if (!_classifiedItems.ContainsKey(mainType)) {
                _classifiedItems[mainType] = new List<ItemData>();
            }
        }

        var itemsConfig = GameConfig.Instance.ItemDataScriptObject.ItemData;
        foreach (var itemData in itemsConfig) {
            foreach (var group in _typeList) {
                ItemType mainType = group[0];
                if ((itemData.Type & mainType) == itemData.Type) {
                    _classifiedItems[mainType].Add(itemData);
                    break; 
                }
            }
        }

        foreach (var list in _classifiedItems.Values) {
            list.Sort((a, b) => a.Piror.CompareTo(b.Piror));
        }

#if DEBUGMODE
        stopwatch.Stop();
        ToyBox.LogMessage("LoadItemData Execution time: " + stopwatch.ElapsedMilliseconds + "ms");
#endif
    }

    private void UpdateSubToggles(int type)
    {

#if DEBUGMODE
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
#endif

        var toggles = _subtypeGroup.transform;

        int togglenum = _typeList[type].Length;
        toggles.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text = "전체";
        switch(type) {
            case 0:
                toggles.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text = "무기";
                toggles.GetChild(2).GetComponentInChildren<TextMeshProUGUI>().text = "내갑";
                toggles.GetChild(3).GetComponentInChildren<TextMeshProUGUI>().text = "장신구";
                break;
            case 1:
                toggles.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text = "외공";
                toggles.GetChild(2).GetComponentInChildren<TextMeshProUGUI>().text = "내공";
                break;
            case 2:
                toggles.GetChild(1).GetComponentInChildren<TextMeshProUGUI>().text = "음식";
                toggles.GetChild(2).GetComponentInChildren<TextMeshProUGUI>().text = "단약";
                toggles.GetChild(3).GetComponentInChildren<TextMeshProUGUI>().text = "약품";
                toggles.GetChild(4).GetComponentInChildren<TextMeshProUGUI>().text = "제법";
                break;
        }

        for (int i = 0; i < toggles.childCount; i++) {
            toggles.GetChild(i).gameObject.SetActive(i < togglenum);
        }

        toggles.GetChild(0).GetComponent<Toggle>().isOn = true;

#if DEBUGMODE
        stopwatch.Stop();
        ToyBox.LogMessage("UpdateSubToggles Execution time: " + stopwatch.ElapsedMilliseconds + "ms");
#endif
    }


    private void UpdateItemList(int maintype, int subType = 0)
    {

        if (maintype < 0 || maintype >= _typeList.Length) return;

#if DEBUGMODE
        System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
        stopwatch.Start();
#endif

        _selectedType = maintype;
        _selectedSubtype = subType;
        var type = _typeList[maintype][0];
        ItemList = _classifiedItems[type].Where(x =>
            (x.Type & _typeList[maintype][subType]) == x.Type)
            .ToList();

        int beforeSearchCount = ItemList.Count;
        if (!string.IsNullOrEmpty(_searchKeyword)) {
            ItemList = ItemList.Where(x => MatchesSearch(x, _searchKeyword)).ToList();
        }

        _infinityScroll.Data = ItemList;

#if DEBUGMODE
        stopwatch.Stop();
        ToyBox.LogMessage("UpdateItemList Execution time: " + stopwatch.ElapsedMilliseconds + "ms");
#endif
    }

    private static string GetSearchKeyword(string rawKeyword)
    {
        rawKeyword ??= "";
        if (rawKeyword.Contains("<u>", StringComparison.OrdinalIgnoreCase) ||
            rawKeyword.Contains("</u>", StringComparison.OrdinalIgnoreCase)) {
            return "";
        }

        string keyword = NormalizeSearchText(rawKeyword);
        if (string.IsNullOrEmpty(keyword)) return "";

        if (keyword.Equals("Enter text ...", StringComparison.OrdinalIgnoreCase)) {
            return "";
        }

        if (!keyword.Any(char.IsLetterOrDigit)) {
            return "";
        }

        return keyword;
    }

    private string ReadRawSearchText()
    {
        string inputText = _searchInput?.text ?? "";
        if (IsUsableRawSearchText(inputText)) {
            return inputText;
        }

        foreach (var text in _searchInput.GetComponentsInChildren<TextMeshProUGUI>(true)) {
            if (text == null) continue;

            string name = text.transform.name;
            if (name.Contains("Placeholder", StringComparison.OrdinalIgnoreCase)) continue;

            string candidate = text.text ?? "";
            if (IsUsableRawSearchText(candidate)) {
                return candidate;
            }
        }

        return "";
    }

    private static (string Name, string PlainName, string Reading, string CompactReading) GetSearchText(ItemData item)
    {
        string name = NormalizeSearchText(item.GetName(true));
        string plainName = NormalizeSearchText(item.GetName(false));
        string reading = NormalizeSearchText(HanjaReading.ToHangulReading($"{name} {plainName}"));
        string compactReading = reading.Replace(" ", "");
        return (name, plainName, reading, compactReading);
    }

    private static bool IsUsableRawSearchText(string text)
    {
        string keyword = GetSearchKeyword(text);
        return !string.IsNullOrEmpty(keyword);
    }

    private static void SetNearbyLabel(Transform target, string label)
    {
        if (target == null) return;

        var targetRect = target.GetComponent<RectTransform>();
        if (targetRect == null) return;

        TranslateQuantityLabels(target.root);

        Vector3 targetPosition = targetRect.TransformPoint(targetRect.rect.center);
        var root = target.root;
        TextMeshProUGUI best = null;
        float bestDistance = float.MaxValue;

        foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)) {
            if (text == null || text.GetComponentInParent<TMP_InputField>() != null) continue;

            string textName = text.transform.name;
            string parentName = text.transform.parent?.name ?? "";
            string current = text.text?.Trim() ?? "";
            bool nameHint = textName.Contains("Num", StringComparison.OrdinalIgnoreCase) ||
                            parentName.Contains("Num", StringComparison.OrdinalIgnoreCase);
            bool textHint = current is "數量" or "数量" or "數" or "量";

            if (nameHint || textHint) {
                text.text = label;
                return;
            }

            var textRect = text.GetComponent<RectTransform>();
            if (textRect == null) continue;

            Vector3 textPosition = textRect.TransformPoint(textRect.rect.center);
            float verticalDistance = Mathf.Abs(textPosition.y - targetPosition.y);
            float horizontalDistance = Mathf.Abs(textPosition.x - targetPosition.x);
            if (verticalDistance > 80f || horizontalDistance > 700f) continue;

            float distance = Vector3.Distance(textPosition, targetPosition);
            if (distance < bestDistance) {
                bestDistance = distance;
                best = text;
            }
        }

        if (best != null && bestDistance < 700f) {
            best.text = label;
        }
    }

    private static void TranslateQuantityLabels(Transform root)
    {
        if (root == null) return;

        foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)) {
            string current = text.text?.Trim() ?? "";
            if (current is "\u6578\u91cf" or "\u6570\u91cf" or "\u6578" or "\u6570") {
                text.text = "수량";
            }
        }
    }

    private static bool MatchesSearch(ItemData item, string keyword)
    {
        string normalizedKeyword = NormalizeSearchText(keyword);
        string compactKeyword = normalizedKeyword.Replace(" ", "");
        if (string.IsNullOrEmpty(normalizedKeyword)) return true;

        var searchText = GetSearchText(item);
        return MatchesSearchText(searchText.Name, normalizedKeyword, compactKeyword) ||
               MatchesSearchText(searchText.PlainName, normalizedKeyword, compactKeyword) ||
               MatchesSearchText(searchText.Reading, normalizedKeyword, compactKeyword);
    }

    private static bool MatchesSearchText(string text, string keyword, string compactKeyword)
    {
        string normalizedText = NormalizeSearchText(text);
        if (normalizedText.Contains(keyword, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        string readingText = NormalizeSearchText(HanjaReading.ToHangulReading(normalizedText));
        if (readingText.Contains(keyword, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        string compactReadingText = readingText.Replace(" ", "");
        if (compactKeyword.Length > 0 &&
            compactReadingText.Contains(compactKeyword, StringComparison.OrdinalIgnoreCase)) {
            return true;
        }

        return compactKeyword.Length > 0 &&
               normalizedText.Replace(" ", "").Contains(compactKeyword, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeSearchText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        string stripped = StripRichText(text).Replace("\uFFFD", "");
        var result = new System.Text.StringBuilder(stripped.Length);

        foreach (char c in stripped) {
            var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
            if (category is System.Globalization.UnicodeCategory.Format or
                System.Globalization.UnicodeCategory.Control or
                System.Globalization.UnicodeCategory.NonSpacingMark) {
                continue;
            }

            result.Append(c);
        }

        return result.ToString().Trim();
    }

    private static string StripRichText(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var result = new System.Text.StringBuilder(text.Length);
        bool inTag = false;

        foreach (char c in text) {
            if (c == '<') {
                inTag = true;
                continue;
            }

            if (c == '>') {
                inTag = false;
                continue;
            }

            if (!inTag) {
                result.Append(c);
            }
        }

        return result.ToString();
    }

}
