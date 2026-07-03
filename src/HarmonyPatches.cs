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
    /// <summary>
    /// 在 Harmony.CreateAndPatchAll 中注册所有补丁。
    /// </summary>
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
            // 保存原始搜索文本，清空以绕过游戏的 string.Contains 过滤
            __state = __instance.recipeFilter;
            __instance.recipeFilter = "";
        }

        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        internal static void Postfix(PlayerCamera __instance, string __state)
        {
            string filter = __state;
            if (string.IsNullOrEmpty(filter))
                return;

            // 如果有物品过滤（拖拽物品查看配方），跳过文本筛选
            _recipeItemFilterField ??= AccessTools.Field(typeof(PlayerCamera), "recipeItemFilter");
            if (_recipeItemFilterField.GetValue(__instance) != null)
                return;

            // 获取私有字段 recipeObjects
            _recipeObjectsField ??= AccessTools.Field(typeof(PlayerCamera), "recipeObjects");
            var recipeObjects = (List<GameObject>)_recipeObjectsField.GetValue(__instance);

            // 收集需要移除的条目
            var toRemove = new List<GameObject>();
            foreach (GameObject obj in recipeObjects)
            {
                var tooltip = obj.GetComponent<UITooltip>();
                if (tooltip == null) continue;

                string simpleName = tooltip.tipName;
                if (string.IsNullOrEmpty(simpleName)) continue;

                // 原有 Contains 匹配
                bool origMatch = simpleName.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) >= 0;
                // 拼音匹配
                bool pinyinMatch = PinyinMatcher.Contains(simpleName, filter);

                if (!origMatch && !pinyinMatch)
                    toRemove.Add(obj);
            }

            // 销毁不匹配的条目
            foreach (GameObject obj in toRemove)
            {
                recipeObjects.Remove(obj);
                Object.Destroy(obj);
            }
        }
    }
}
