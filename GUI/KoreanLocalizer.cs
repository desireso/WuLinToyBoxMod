using TMPro;

namespace HaxxToyBox.GUI;

internal static class KoreanLocalizer
{
    private static readonly Dictionary<string, string> TextMap = new() {
        { "角色", "동료" },
        { "物品", "아이템" },
        { "武学", "무공" },
        { "特质", "특성" },
        { "杂项", "기타" },
        { "添加特质", "특성 추가" },
        { "删除", "삭제" },
        { "添加", "추가" },
        { "全部", "전체" },
        { "装备", "장비" },
        { "武功书", "무공서" },
        { "消耗品", "소비품" },
        { "材料", "재료" },
        { "地图", "지도" },
        { "其他", "기타" },
        { "武器", "무기" },
        { "内甲", "내갑" },
        { "配饰", "장신구" },
        { "外功", "외공" },
        { "内功", "내공" },
        { "食物", "음식" },
        { "丹药", "단약" },
        { "药品", "약품" },
        { "配方", "제법" },
        { "内功心法", "내공" },
        { "拳掌", "권장" },
        { "御剑", "검" },
        { "耍刀", "도" },
        { "长兵", "장병" },
        { "短兵", "단병" },
        { "音律", "악기" },
        { "乐器", "악기" },
        { "搜索", "검색" },
        { "数量", "수량" },
        { "金钱", "돈" },
        { "金币", "돈" },
        { "技能经验倍率", "기술 경험치 배율" },
        { "行走速度", "이동 속도" },
        { "战斗速度", "전투 속도" },
        { "时间冻结", "시간 정지" },
        { "自动恢复", "자동 회복" },
        { "不遇敌", "전투 회피" },
        { "满好感", "호감도 최대" },
        { "好感", "호감도" },
        { "启用成就", "도전 과제 허용" },
        { "武学大成", "무공 완성" },
        { "解锁全部成就", "모든 도전 과제 해금" },
        { "恢复", "회복" },
        { "面板开关", "패널 열기/닫기" },
        { "加速", "속도 증가" },
        { "减速", "속도 감소" },
        { "按下按键", "키를 누르세요" }
    };

    private static readonly Dictionary<string, string> NameMap = new() {
        { "RolePanel", "동료" },
        { "ItemPanel", "아이템" },
        { "MartialPanel", "무공" },
        { "MiscPanel", "기타" },
        { "TraitPanel", "특성" },
        { "AddTrait", "특성 추가" },
        { "DelButton", "삭제" },
        { "TimeFreeze", "시간 정지" },
        { "Recover", "회복" },
        { "NoCombat", "전투 회피" },
        { "Friendship", "호감도 최대" },
        { "EnableAchievement", "도전 과제 허용" },
        { "UltimateMartial", "무공 완성" },
        { "SkillExp", "기술 경험치 배율" },
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
