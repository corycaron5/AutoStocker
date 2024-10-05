using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AutoDisplayCards;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal new static ManualLogSource Logger;
    private readonly Harmony harmony = new Harmony("AutoStocker");

    public static ConfigEntry<KeyboardShortcut> FillCardTableKey;
    public static ConfigEntry<KeyboardShortcut> FillItemShelfKey;
    public static ConfigEntry<KeyboardShortcut> MoveFloorBoxesToShelvesKey;
    public static ConfigEntry<ECardExpansionType> FillCardExpansionType;
    public static ConfigEntry<int> AmountToHold;
    public static ConfigEntry<float> MaxCardValue;
    public static ConfigEntry<float> MinCardValue;
    public static ConfigEntry<bool> EnableDebugLogging;
    public static ConfigEntry<bool> PluginEnabled;
    public static ConfigEntry<bool> RefillSprayers;
    public static ConfigEntry<bool> PrioritizeSprayersWhenRefillingStock;
    public static ConfigEntry<bool> RandomCardFill;
    public static ConfigEntry<bool> EnableMultiExpansion;
    public static Dictionary<ECardExpansionType, ConfigEntry<bool>> EnabledExpansions;
    public static KeyboardShortcut debugKey = new KeyboardShortcut(KeyCode.F9);
    
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        SetupConfig();
        this.harmony.PatchAll();
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }
    
    private void SetupConfig()
    {
        Plugin.PluginEnabled = base.Config.Bind<bool>("BepInEx", "PluginEnabled", true, "Enable or disable this plugin.");
        Plugin.EnableDebugLogging = base.Config.Bind<bool>("Debug", "enableDebugLogging", false, "Enable logging of all actions.");
        Plugin.FillCardTableKey = base.Config.Bind<KeyboardShortcut>("Keybinds", "FillCardTableKey", new KeyboardShortcut(KeyCode.F7), "Keyboard Shortcut to auto fill all card tables.");
        Plugin.FillItemShelfKey = base.Config.Bind<KeyboardShortcut>("Keybinds", "FillItemShelfKey", new KeyboardShortcut(KeyCode.F6), "Keyboard Shortcut to auto fill all item shelves.");
        Plugin.MoveFloorBoxesToShelvesKey = base.Config.Bind<KeyboardShortcut>("Keybinds", "MoveFloorBoxesToShelvesKey", new KeyboardShortcut(KeyCode.F8), "Keyboard Shortcut to move closed boxes from floor to applicable warehouse shelves.");
        Plugin.FillCardExpansionType = base.Config.Bind<ECardExpansionType>("Settings", "FillCardExpansionType", ECardExpansionType.Tetramon, "Which set to pull from when filling card tables.");
        Plugin.AmountToHold = base.Config.Bind<int>("Settings", "AmountToHold", 1, "Tells the mod to keep at least this number of each card before putting them for sale.");
        Plugin.MaxCardValue = base.Config.Bind<float>("Settings", "MaxCardValue", 7500.0f, "The max value of a card that can be put for sale by the mod.");
        Plugin.MinCardValue = base.Config.Bind<float>("Settings", "MinCardValue", 3.0f, "The min value of a card that can be put for sale by the mod.");
        Plugin.RefillSprayers = base.Config.Bind<bool>("Settings", "RefillSprayers", true, "Whether sprayers should be refilled when restocking items.");
        Plugin.PrioritizeSprayersWhenRefillingStock = base.Config.Bind<bool>("Settings", "PrioritizeSprayersWhenRefillingStock", true, "Whether sprayers should be refilled before item shelves.");
        Plugin.RandomCardFill = base.Config.Bind<bool>("Settings", "RandomCardFill", false, "Whether to randomly fill card tables with cards that meet the filter criteria instead of highest price first.");
        Plugin.EnableMultiExpansion = base.Config.Bind<bool>("Settings", "EnableMultiExpansion", false, "Whether to allow multiple expansions to be enabled at the same time.");
        BuildEnabledExpansions();
    }

    private void BuildEnabledExpansions()
    {
        EnabledExpansions = new Dictionary<ECardExpansionType, ConfigEntry<bool>>();
        foreach (ECardExpansionType expansionType in Enum.GetValues(typeof(ECardExpansionType)))
        {
            ConfigEntry<bool> configEntry = base.Config.Bind<bool>("Enabled Expansions | Requires EnableMultiExpansion to be enabled", expansionType.ToString(), false, "Whether this expansion should be included when filling card tables.");
            EnabledExpansions.Add(expansionType, configEntry);
        }
    }

    public static void LogDebugMessage(string message)
    {
        if (EnableDebugLogging.Value)
        {
            Logger.LogInfo(message);
        }
    }
}
