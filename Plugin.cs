using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AutoDisplayCards;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private readonly Harmony harmony = new Harmony("AutoStocker");

    public static ConfigEntry<KeyboardShortcut> FillCardTableKey;
    public static ConfigEntry<KeyboardShortcut> FillItemShelfKey;
    public static ConfigEntry<ECardExpansionType> FillCardExpansionType;
    public static ConfigEntry<int> AmountToHold;
    public static ConfigEntry<float> MaxCardValue;
    public static ConfigEntry<float> MinCardValue;
    public static ConfigEntry<bool> EnableDebugLogging;
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
        Plugin.EnableDebugLogging = base.Config.Bind<bool>("Debug", "enableDebugLogging", false, "Enable logging of all actions.");
        Plugin.FillCardTableKey = base.Config.Bind<KeyboardShortcut>("Keybinds", "FillCardTableKey", new KeyboardShortcut(KeyCode.F7), "Keyboard Shortcut to auto fill all card tables.");
        Plugin.FillItemShelfKey = base.Config.Bind<KeyboardShortcut>("Keybinds", "FillItemShelfKey", new KeyboardShortcut(KeyCode.F6), "Keyboard Shortcut to auto fill all item shelves.");
        Plugin.FillCardExpansionType = base.Config.Bind<ECardExpansionType>("Settings", "FillCardExpansionType", ECardExpansionType.Tetramon, "Which set to pull from when filling card tables.");
        Plugin.AmountToHold = base.Config.Bind<int>("Settings", "AmountToHold", 1, "Tells the mod to keep at least this number of each card before putting them for sale.");
        Plugin.MaxCardValue = base.Config.Bind<float>("Settings", "MaxCardValue", 7500.0f, "The max value of a card that can be put for sale by the mod.");
        Plugin.MinCardValue = base.Config.Bind<float>("Settings", "MinCardValue", 3.0f, "The min value of a card that can be put for sale by the mod.");

    }

    public static void LogDebugMessage(string message)
    {
        if (EnableDebugLogging.Value)
        {
            Logger.LogInfo(message);
        }
    }
}
