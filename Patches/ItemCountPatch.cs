using GameData;
using HarmonyLib;
using HaxxToyBox.Config;
using HaxxToyBox.GUI;
using WuLin;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;

namespace HaxxToyBox.Patches;

internal static class FixedItemCountHelper
{
    private static bool _isApplying;

    private static readonly ItemType ExcludedTypes =
        ItemType.Misc_Quest |
        ItemType.Misc_Map |
        ItemType.Consumeable_Recipe;

    public static void ApplyToPack(GameItemPack pack, string source)
    {
        if (!IsEnabled() || pack == null) return;

        foreach (var item in pack.Contents)
        {
            ApplyToItemId(item.TempleteId, source);
        }
    }

    public static void ApplyToItemId(int itemId, string source)
    {
        if (!IsEnabled() || _isApplying) return;

        var inventory = PlayerTeamManager.Instance?.TeamInventory;
        if (inventory == null) return;

        try
        {
            var item = inventory.GetItem(itemId);
            if (item != null)
            {
                ApplyToItem(inventory, item, source);
                return;
            }

            var itemData = FindItemData(itemId);
            if (!CanApply(itemData)) return;

            int targetCount = GetTargetCount();
            _isApplying = true;
            if (inventory.AddItem(itemData, targetCount, true))
            {
                ToyBox.LogMessage($"[FixedItemCount] {source}: ID={itemData.Uid}, Name={itemData.GetName(true)}, Count 0->{targetCount}");
            }
        }
        catch (Exception ex)
        {
            ToyBox.LogWarning($"[FixedItemCount] Failed to apply fixed item count for ID={itemId}. {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _isApplying = false;
        }
    }

    public static void ApplyToItemIdNextFrame(int itemId, string source)
    {
        if (ToyBoxBehaviour.Instance == null) return;

        ToyBoxBehaviour.Instance.StartCoroutine(ApplyNextFrame(itemId, source).WrapToIl2Cpp());
    }

    public static void LogItemState(ItemData itemData, string source)
    {
        if (itemData == null) return;

        var inventory = PlayerTeamManager.Instance?.TeamInventory;
        var item = inventory?.GetItem(itemData.Uid);
        string currentCount = item == null ? "not-owned" : item.Stack.ToString();
        string instanceStackable = item == null ? "n/a" : item.IsStackable.ToString();
        string enabled = IsEnabled().ToString();
        string targetCount = GetTargetCount().ToString();

        ToyBox.LogMessage(
            $"[FixedItemCount] {source} state: Enabled={enabled}, Target={targetCount}, ID={itemData.Uid}, Name={itemData.GetName(true)}, " +
            $"DataStackable={itemData.IsStackable}, InstanceStackable={instanceStackable}, Type={itemData.Type}, Current={currentCount}");
    }

    public static void ApplyToItem(GameItemPack inventory, GameItemInstance item, string source)
    {
        if (!IsEnabled() || _isApplying || inventory == null || item == null) return;
        if (!CanApply(item)) return;

        try
        {
            _isApplying = true;

            int targetCount = GetTargetCount();
            int currentCount = item.Stack;
            if (currentCount == targetCount) return;

            if (item.ChangeStack(targetCount - currentCount))
            {
                ToyBox.LogMessage($"[FixedItemCount] {source}: ID={item.TempleteId}, Name={item.ItemName}, Count {currentCount}->{targetCount}");
            }
        }
        catch (Exception ex)
        {
            ToyBox.LogWarning($"[FixedItemCount] Failed to apply fixed item count for ID={item.TempleteId}. {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            _isApplying = false;
        }
    }

    public static void ApplyChangedInventoryItem(GameItemInstance item, string source)
    {
        if (!IsEnabled() || _isApplying || item == null) return;

        var inventory = PlayerTeamManager.Instance?.TeamInventory;
        if (inventory == null) return;

        var inventoryItem = inventory.GetItem(item.TempleteId);
        if (inventoryItem == null) return;

        ApplyToItem(inventory, inventoryItem, source);
    }

    private static bool IsEnabled()
    {
        return GetTargetCount() > 0;
    }

    private static int GetTargetCount()
    {
        return Mathf.Clamp(ConfigManager.FixedItemCount?.Value ?? 0, 0, 9999);
    }

    private static IEnumerator ApplyNextFrame(int itemId, string source)
    {
        yield return null;
        ApplyToItemId(itemId, source);
    }

    private static bool CanApply(GameItemInstance item)
    {
        return item != null && item.IsStackable && CanApply(item.Templete);
    }

    public static bool IsAppraisalItem(GameItemInstance item)
    {
        try
        {
            return item != null && (item.GetItemType() & ItemType.Consumeable_Special) != 0;
        }
        catch
        {
            return false;
        }
    }

    private static string SafeItemName(GameItemInstance item)
    {
        try
        {
            return item?.ItemName ?? "n/a";
        }
        catch
        {
            return "name-error";
        }
    }

    private static string SafeItemName(ItemData itemData)
    {
        try
        {
            return itemData?.GetName(true) ?? "n/a";
        }
        catch
        {
            return "name-error";
        }
    }

    private static bool CanApply(ItemData itemData)
    {
        if (itemData == null || !itemData.IsStackable) return false;

        return (itemData.Type & ExcludedTypes) == 0;
    }

    private static ItemData FindItemData(int itemId)
    {
        var items = GameConfig.Instance?.ItemDataScriptObject?.ItemData;
        if (items == null) return null;

        foreach (var itemData in items)
        {
            if (itemData.Uid == itemId)
            {
                return itemData;
            }
        }

        return null;
    }
}

public class FixedItemCountAppraisalPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(UIItem), "ChestClick")]
    public static void ChestClick_Prefix(GameItemInstance Data, ref int __state)
    {
        __state = FixedItemCountHelper.IsAppraisalItem(Data) ? Data.TempleteId : 0;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIItem), "ChestClick")]
    public static void ChestClick_Postfix(int __state)
    {
        if (__state == 0) return;

        FixedItemCountHelper.ApplyToItemId(__state, "Appraisal");
    }
}

public class FixedItemCountPickupPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerTeamManager), "PickupPack")]
    public static void PickupPack_Postfix(GameItemPack pack)
    {
        FixedItemCountHelper.ApplyToPack(pack, "PickupPack");
    }
}

public class FixedItemCountAddItemInstancePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameItemPack), "AddItem", typeof(GameItemInstance), typeof(bool))]
    public static void AddItem_Postfix(GameItemPack __instance, GameItemInstance gameItemInstance, bool __result)
    {
        if (__result && __instance == PlayerTeamManager.Instance?.TeamInventory)
        {
            FixedItemCountHelper.ApplyToItemId(gameItemInstance.TempleteId, "AddItem");
        }
    }
}

public class FixedItemCountAddItemIdPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameItemPack), "AddItem", typeof(int), typeof(int), typeof(bool))]
    public static void AddItem_Postfix(GameItemPack __instance, int itemId, bool __result)
    {
        if (__result && __instance == PlayerTeamManager.Instance?.TeamInventory)
        {
            FixedItemCountHelper.ApplyToItemId(itemId, "AddItem");
        }
    }
}

public class FixedItemCountAddItemDataPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameItemPack), "AddItem", typeof(ItemData), typeof(int), typeof(bool))]
    public static void AddItem_Postfix(GameItemPack __instance, ItemData itemData, bool __result)
    {
        if (__result && __instance == PlayerTeamManager.Instance?.TeamInventory)
        {
            FixedItemCountHelper.ApplyToItemId(itemData.Uid, "AddItem");
        }
    }
}

public class FixedItemCountRemovePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerTeamManager), "RemoveItem")]
    public static void RemoveItem_Postfix(int itemId, bool __result)
    {
        if (__result)
        {
            FixedItemCountHelper.ApplyToItemId(itemId, "RemoveItem");
        }
    }
}

public class FixedItemCountTakePatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerTeamManager), "TakeItem")]
    public static void TakeItem_Postfix(GameItemInstance gameItemInstance, GameItemInstance __result)
    {
        if (__result != null)
        {
            FixedItemCountHelper.ApplyToItemId(gameItemInstance.TempleteId, "TakeItem");
        }
    }
}

public class FixedItemCountUseOnCharacterPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameItemPack), "UseItemOnCharacter", typeof(GameItemInstance), typeof(GameCharacterInstance), typeof(int))]
    public static void UseItemOnCharacter_Postfix(GameItemPack __instance, GameItemInstance gameItemInstance, bool __result)
    {
        if (__result && __instance == PlayerTeamManager.Instance?.TeamInventory)
        {
            FixedItemCountHelper.ApplyToItemId(gameItemInstance.TempleteId, "UseItemOnCharacter");
        }
    }
}

public class FixedItemCountUseOnBattleActorPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameItemPack), "UseItemOnBattleActor", typeof(GameItemInstance), typeof(BattleActor), typeof(BattleActor), typeof(int), typeof(bool))]
    public static void UseItemOnBattleActor_Postfix(GameItemPack __instance, GameItemInstance gameItemInstance, bool consumeItem, bool __result)
    {
        if (__result && consumeItem && __instance == PlayerTeamManager.Instance?.TeamInventory)
        {
            FixedItemCountHelper.ApplyToItemId(gameItemInstance.TempleteId, "UseItemOnBattleActor");
        }
    }
}

public class FixedItemCountChangeStackPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameItemInstance), "ChangeStack")]
    public static void ChangeStack_Postfix(GameItemInstance __instance, bool __result)
    {
        if (!__result || __instance == null) return;

        ToyBox.LogMessage($"[FixedItemCount] ChangeStack hit: ID={__instance.TempleteId}, Name={__instance.ItemName}, Count={__instance.Stack}");
        FixedItemCountHelper.ApplyChangedInventoryItem(__instance, "ChangeStack");
    }
}

public class FixedItemCountFactionBuyItemPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(TradingWithFactionManager), "BuyItem")]
    public static void BuyItem_Postfix(FactionGoodItem factionGoodItem, int count)
    {
        var itemData = factionGoodItem?.ItemData;
        if (itemData == null) return;

        ToyBox.LogMessage($"[FixedItemCount] BuyItem hit: ID={itemData.Uid}, Name={itemData.GetName(true)}, Count={count}");

        FixedItemCountHelper.LogItemState(itemData, "FactionBuyItem before");
        FixedItemCountHelper.ApplyToItemId(itemData.Uid, "FactionBuyItem");
        FixedItemCountHelper.ApplyToItemIdNextFrame(itemData.Uid, "FactionBuyItem next-frame");
    }
}

public class FixedItemCountGiftingPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GiftingWithNpcManager), "ConfirmGifting", typeof(GameItemInstance))]
    public static void ConfirmGifting_Postfix(GameItemInstance gameItemInstance)
    {
        if (gameItemInstance == null) return;

        ToyBox.LogMessage($"[FixedItemCount] ConfirmGifting hit: ID={gameItemInstance.TempleteId}, Name={gameItemInstance.ItemName}, Count={gameItemInstance.Stack}");

        FixedItemCountHelper.ApplyToItemId(gameItemInstance.TempleteId, "ConfirmGifting");
        FixedItemCountHelper.ApplyToItemIdNextFrame(gameItemInstance.TempleteId, "ConfirmGifting next-frame");
    }
}

public class FixedItemCountNpcTradingPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TradingWithNpcManager), "ConfirmTrading")]
    public static void ConfirmTrading_Prefix(ref List<int> __state)
    {
        __state = new List<int>();
        AddPackItemIds(__state, TradingWithNpcManager.playerTradingZone);
        AddPackItemIds(__state, TradingWithNpcManager.npcTradingZone);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TradingWithNpcManager), "ConfirmTrading")]
    public static void ConfirmTrading_Postfix(List<int> __state)
    {
        if (__state == null || __state.Count == 0) return;

        foreach (int itemId in __state.Distinct())
        {
            ToyBox.LogMessage($"[FixedItemCount] ConfirmTrading hit: ID={itemId}");
            FixedItemCountHelper.ApplyToItemId(itemId, "ConfirmTrading");
            FixedItemCountHelper.ApplyToItemIdNextFrame(itemId, "ConfirmTrading next-frame");
        }
    }

    private static void AddPackItemIds(List<int> itemIds, GameItemPack pack)
    {
        if (itemIds == null || pack == null) return;

        foreach (var item in pack.Contents)
        {
            if (item != null)
            {
                itemIds.Add(item.TempleteId);
            }
        }
    }
}
