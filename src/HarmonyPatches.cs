using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace JustUnknownCharacters;

public static class HarmonyPatches
{
	public static void Apply()
	{
		var harmony = new Harmony("JustUnknownCharacters.pinyin");
		harmony.PatchAll(typeof(Patch_RefreshRecipeList));
		Plugin.Log.LogInfo("Patches applied.");
	}

	[HarmonyPatch(typeof(PlayerCamera), "RefreshRecipeList")]
	private static class Patch_RefreshRecipeList
	{
		private static FieldInfo _recipeObjectsField;
		private static FieldInfo _recipeItemFilterField;
		private static FieldInfo _recipeListContentField;

		[HarmonyPrefix]
		internal static void Prefix(PlayerCamera __instance, out string __state)
		{
			__state = __instance.recipeFilter;
			if (!string.IsNullOrEmpty(__state))
				__instance.recipeFilter = "";
		}

		[HarmonyPostfix]
		internal static void Postfix(PlayerCamera __instance, string __state)
		{
			var filter = __state;
			if (string.IsNullOrEmpty(filter))
				return;

			_recipeItemFilterField ??= AccessTools.Field(typeof(PlayerCamera), "recipeItemFilter");
			if (_recipeItemFilterField.GetValue(__instance) != null)
				return;

			_recipeObjectsField ??= AccessTools.Field(typeof(PlayerCamera), "recipeObjects");
			_recipeListContentField ??= AccessTools.Field(typeof(PlayerCamera), "recipeListContent");

			var recipeObjects = (List<GameObject>)_recipeObjectsField.GetValue(__instance);
			var content = (RectTransform)_recipeListContentField.GetValue(__instance);

			var toKeep = new List<GameObject>();
			foreach (var obj in recipeObjects)
			{
				var tooltip = obj.GetComponent<UITooltip>();
				var name = tooltip != null ? tooltip.tipName : null;
				if (string.IsNullOrEmpty(name))
				{
					toKeep.Add(obj);
					continue;
				}

				var match = name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0
					|| PinyinMatcher.Contains(name, filter);

				if (match)
					toKeep.Add(obj);
				else
					Object.Destroy(obj);
			}

			recipeObjects.Clear();
			recipeObjects.AddRange(toKeep);

			for (var i = 0; i < recipeObjects.Count; i++)
				recipeObjects[i].GetComponent<RectTransform>().anchoredPosition =
					new Vector2(-9f, -i * 64f);

			content.sizeDelta = new Vector2(1f, recipeObjects.Count * 64f);
		}
	}
}
