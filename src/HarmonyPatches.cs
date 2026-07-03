using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace JustUnknownCharacters;

/// <summary>
/// HarmonyX 补丁 — 将拼音匹配注入合成界面搜索流程。
/// 使用反射访问 Unity API，避免直接引用 UnityEngine.CoreModule 和 netstandard。
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
        private static MethodInfo _destroyMethod;
        private static MethodInfo _getComponentMethod;

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
            var recipeObjects = (IList)_recipeObjectsField.GetValue(__instance);

            // 延迟获取 GetComponent<T> 泛型方法
            if (_getComponentMethod == null)
            {
                // GameObject 来自 UnityEngine.CoreModule，运行时通过 Assembly-CSharp 链式加载
                var goType = recipeObjects[0].GetType();
                _getComponentMethod = goType.GetMethod("GetComponent", Type.EmptyTypes);
            }

            var toRemove = new List<object>();
            foreach (var obj in recipeObjects)
            {
                var typedGetComponent = _getComponentMethod.MakeGenericMethod(typeof(UITooltip));
                var tooltip = typedGetComponent.Invoke(obj, null);
                if (tooltip == null) continue;

                // UITooltip.tipName (public field)
                var simpleName = (string)AccessTools.Field(typeof(UITooltip), "tipName").GetValue(tooltip);
                if (string.IsNullOrEmpty(simpleName)) continue;

                var origMatch = simpleName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
                var pinyinMatch = PinyinMatcher.Contains(simpleName, filter);

                if (!origMatch && !pinyinMatch)
                    toRemove.Add(obj);
            }

            // 销毁不匹配的条目
            foreach (var obj in toRemove)
            {
                recipeObjects.Remove(obj);
                Destroy(obj);
            }
        }

        private static void Destroy(object obj)
        {
            if (_destroyMethod == null)
            {
                var objectType = obj.GetType().Assembly.GetType("UnityEngine.Object");
                _destroyMethod = AccessTools.Method(objectType, "Destroy", new[] { objectType });
            }
            _destroyMethod.Invoke(null, new[] { obj });
        }
    }
}
