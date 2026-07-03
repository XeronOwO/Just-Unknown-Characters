using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace JustUnknownCharacters;

public static class HarmonyPatches
{
	public static void Apply()
	{
		var harmony = new Harmony("JustUnknownCharacters.pinyin");

		foreach (var method in typeof(PlayerCamera).GetMethods(
			BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public))
		{
			if (!method.Name.Contains("<RefreshRecipeList>"))
				continue;

			Plugin.Log.LogInfo($"Patching lambda: {method.Name}");
			harmony.Patch(method,
				transpiler: new HarmonyMethod(typeof(Patch_StringContains).GetMethod("Transpiler")));
		}
	}

	private static class Patch_StringContains
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var containsMethod = AccessTools.Method(
				typeof(string), "Contains",
				new[] { typeof(string), typeof(StringComparison) });

			var replacement = AccessTools.Method(
				typeof(PinyinMatcher), nameof(PinyinMatcher.Match));

			foreach (var inst in instructions)
			{
				if (inst.Calls(containsMethod))
					yield return new CodeInstruction(OpCodes.Call, replacement);
				else
					yield return inst;
			}
		}
	}
}
