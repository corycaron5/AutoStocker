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
    }

    public static void LogDebugMessage(string message)
    {
        if (EnableDebugLogging.Value)
        {
            Logger.LogInfo(message);
        }
    }
}
