using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace JustUnknownCharacters;

/// <summary>
/// HarmonyX 补丁 — 将拼音匹配注入合成界面搜索流程。
/// </summary>
public static class HarmonyPatches
{
	public static void Apply()
	{
		var harmony = new Harmony("JustUnknownCharacters.pinyin");
		harmony.PatchAll(typeof(Patch_RefreshRecipeList));
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshRecipeList")]
	private static class Patch_RefreshRecipeList
	{
		private static FieldInfo _recipeObjectsField;
		private static FieldInfo _recipeItemFilterField;

		[HarmonyPrefix]
		// ReSharper disable once InconsistentNaming
		internal static void Prefix(PlayerCamera __instance, out string __state)
		{
			__state = __instance.recipeFilter;
			__instance.recipeFilter = "";
		}

		[HarmonyPostfix]
		// ReSharper disable once InconsistentNaming
		internal static void Postfix(PlayerCamera __instance, string __state)
		{
			var filter = __state;
			if (string.IsNullOrEmpty(filter))
				return;

			_recipeItemFilterField ??= AccessTools.Field(typeof(PlayerCamera), "recipeItemFilter");
			if (_recipeItemFilterField.GetValue(__instance) != null)
				return;

			_recipeObjectsField ??= AccessTools.Field(typeof(PlayerCamera), "recipeObjects");
			var recipeObjects = (List<GameObject>)_recipeObjectsField.GetValue(__instance);

			var toRemove = new List<GameObject>();
			foreach (var obj in recipeObjects)
			{
				var tooltip = obj.GetComponent<UITooltip>();
				if (tooltip == null) continue;

				var simpleName = tooltip.tipName;
				if (string.IsNullOrEmpty(simpleName)) continue;

				var origMatch = simpleName.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
				var pinyinMatch = PinyinMatcher.Contains(simpleName, filter);

				if (!origMatch && !pinyinMatch)
					toRemove.Add(obj);
			}

			foreach (var obj in toRemove)
			{
				recipeObjects.Remove(obj);
				Object.Destroy(obj);
			}
		}
	}
}
