using BepInEx;
using BepInEx.Logging;

namespace JustUnknownCharacters;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
	internal static ManualLogSource Log { get; private set; }

	private void Awake()
	{
		Log = base.Logger;
		Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

		HarmonyPatches.Apply();
		Log.LogInfo("Pinyin search patches applied.");
	}
}
