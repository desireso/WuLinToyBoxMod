using TMPro;

namespace HaxxToyBox.GUI;

internal static class KoreanLocalizer
{
    private static readonly Dictionary<string, string> TextMap = new() {
        { "\u6578\u91cf", "수량" },
        { "\u6570\u91cf", "수량" },
        { "Role", "프로필" },
        { "Roles", "프로필" },
        { "Profile", "프로필" },
        { "Item", "아이템" },
        { "Items", "아이템" },
        { "Martial", "무공" },
        { "Martials", "무공" },
        { "Misc", "기타" },
        { "Trait", "특성" },
        { "Traits", "특성" },
        { "Add", "추가" },
        { "Delete", "삭제" },
        { "Remove", "삭제" },
        { "Equipment", "장비" },
        { "Kungfu Book", "무공서" },
        { "Consumable", "소비품" },
        { "Material", "재료" },
        { "Map", "지도" },
        { "Other", "기타" },
        { "武学", "무공" },
        { "武功", "무공" },
        { "武功书", "무공서" },
        { "其他", "기타" },
        { "技能经验倍率", "기술 EXP" },
        { "移动速度", "이동 속도" },
        { "战斗速度", "전투 속도" },
        { "时间停止", "시간 정지" },
        { "自动回复", "회복" },
        { "战斗回避", "전투 회피" },
        { "好感度最大", "호감도 최대" },
        { "好感度", "호감도" },
        { "成就开启", "도전 과제 허용" },
        { "武学大成", "무공 완성" },
        { "全部成就解锁", "도전 과제 해금" },
        { "回复", "회복" },
        { "面板打开/关闭", "패널 열기/닫기" },
        { "速度增加", "속도 증가" },
        { "速度减少", "속도 감소" },
        { "请输入", "입력하세요" }
    };

    private static readonly Dictionary<string, string> NameMap = new() {
        { "Role", "프로필" },
        { "Roles", "프로필" },
        { "RolePanel", "프로필" },
        { "Item", "아이템" },
        { "ItemPanel", "아이템" },
        { "Martial", "무공" },
        { "MartialPanel", "무공" },
        { "Misc", "기타" },
        { "MiscPanel", "기타" },
        { "Trait", "특성" },
        { "TraitPanel", "특성" },
        { "AddTrait", "특성 추가" },
        { "DelButton", "삭제" },
        { "TimeFreeze", "시간 정지" },
        { "Recover", "회복" },
        { "NoCombat", "전투 회피" },
        { "Friendship", "호감도 최대" },
        { "EnableAchievement", "도전 과제 허용" },
        { "UltimateMartial", "무공 완성" },
        { "SkillExp", "기술 EXP" },
        { "KungfuBattleExp", "무공 EXP" },
        { "Gold", "돈" },
        { "WalkSpeed", "이동 속도" },
        { "BattleSpeed", "전투 속도" },
        { "Achievement", "도전 과제 해금" },
        { "PanelToggle", "패널 열기/닫기" },
        { "SpeedupToggle", "속도 증가" },
        { "SpeeddownToggle", "속도 감소" }
    };

    public static void Localize(GameObject root)
    {
        if (root == null) return;

        foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)) {
            if (text == null) continue;

            var current = text.text?.Trim();
            if (!string.IsNullOrEmpty(current) && TextMap.TryGetValue(current, out var mapped)) {
                text.text = mapped;
                continue;
            }

            if (NameMap.TryGetValue(text.transform.parent?.name ?? "", out mapped) ||
                NameMap.TryGetValue(text.transform.name, out mapped)) {
                text.text = mapped;
            }
        }
    }

    public static void SetToggleLabels(Transform group, params string[] labels)
    {
        if (group == null) return;

        int count = Math.Min(group.childCount, labels.Length);
        for (int i = 0; i < count; i++) {
            var text = group.GetChild(i).GetComponentInChildren<TextMeshProUGUI>(true);
            if (text != null) {
                text.text = labels[i];
            }
        }
    }
}
