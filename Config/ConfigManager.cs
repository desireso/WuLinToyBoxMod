using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace HaxxToyBox.Config;

public static class ConfigManager
{
    internal static readonly Dictionary<string, ConfigElement> ConfigElements = new();

    public static ConfigHandler Handler { get; private set; }

    public static ConfigElement Canvas_Toggle;
    public static ConfigElement SpeedUp_Toggle;
    public static ConfigElement SpeedDown_Toggle;
    public static ConfigElement Recover_Toggle;

    public static ConfigEntry<bool> TimeFreezeEnabled;
    public static ConfigEntry<bool> RecoverEnabled;
    public static ConfigEntry<bool> NoCombatEnabled;
    public static ConfigEntry<bool> RelationEnabled;
    public static ConfigEntry<bool> EnableAchievement;
    public static ConfigEntry<bool> UltimateMartial;

    public static ConfigEntry<int> SkillExpMultiple;
    public static ConfigEntry<int> WalkSpeed;
    public static ConfigEntry<int> BattleSpeed;

    public static void Init(ConfigHandler configHandler)
    {
        Handler = configHandler;

        CreateConfigElements();

        Handler.LoadConfig();
    }

    internal static void RegisterConfigElement(ConfigElement configElement)
    {
        Handler.RegisterConfigElement(configElement);
        ConfigElements.Add(configElement.Name, configElement);
    }

    private static void CreateConfigElements()
    {
        Canvas_Toggle = new("HaxxToyBox Toggle",
            "The key to show and hide ToyBox panel.",
            KeyCode.Tab);

        SpeedUp_Toggle = new("SpeedUp Toggle",
            "The key to increase game speed.",
            KeyCode.Equals);
        SpeedDown_Toggle = new("SpeedDown Toggle",
            "The key to decrease game speed.",
            KeyCode.Minus);

        Recover_Toggle = new("Recover Toggle",
            "The key to recover all statuses of the entire team.",
            KeyCode.F1);

        TimeFreezeEnabled = Handler.BindConfig("TimeFreeze Enabled",
            "Whether time freeze is enabled.",
            false);
        RecoverEnabled = Handler.BindConfig("Recover Enabled",
            "Whether automatic recovery after battle is enabled.",
            false);
        NoCombatEnabled = Handler.BindConfig("NoCombat Enabled",
            "Whether avoiding combat is enabled.",
            false);
        RelationEnabled = Handler.BindConfig("Relation Enabled",
            "Whether max relation helper is enabled.",
            false);
        EnableAchievement = Handler.BindConfig("EnableAchievement Enabled",
            "Whether achievements are allowed while mods are active.",
            false);
        UltimateMartial = Handler.BindConfig("UltimateMartial Enabled",
            "Whether instant ultimate martial learning is enabled.",
            false);

        SkillExpMultiple = Handler.BindConfig("SkillExp Multiple",
            "Skill experience multiplier.",
            1);
        WalkSpeed = Handler.BindConfig("WalkSpeed",
            "Walk speed multiplier.",
            1);
        BattleSpeed = Handler.BindConfig("BattleSpeed",
            "Battle speed multiplier.",
            1);
    }
}
