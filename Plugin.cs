using BepInEx;
using BepInEx.Logging;
using BepInEx.NET.Common;
using HarmonyLib;

namespace JustUnknownCharacters;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;

    public override void Load()
    {
        Log = base.Log;
        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        // 注册拼音搜索 Harmony 补丁
        HarmonyPatches.Apply();
        Log.LogInfo("Pinyin search patches applied.");
    }
}
