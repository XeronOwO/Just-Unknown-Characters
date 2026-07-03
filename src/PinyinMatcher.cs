using System;

namespace JustUnknownCharacters;

/// <summary>
/// 拼音搜索匹配器 — PinIn 算法的简化 C# 移植。
/// 支持: 全拼、首字母缩写、中英混合输入、多音字。
/// 来源: Just Enough Characters / PinIn (https://github.com/Towdium/PinIn)
/// </summary>
public static class PinyinMatcher
{
    /// <summary>
    /// 判断 <paramref name="name"/> 是否与 <paramref name="filter"/> 拼音匹配。
    /// 会自动尝试每个起始位置，实现 Contains 语义。
    /// </summary>
    public static bool Contains(string name, string filter)
    {
        if (string.IsNullOrEmpty(filter))
            return true;
        if (string.IsNullOrEmpty(name))
            return false;

        // 尝试从 name 的每个位置开始匹配
        for (var start = 0; start < name.Length; start++)
        {
            if (MatchFrom(name, start, filter, 0))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 递归回溯匹配核心。
    /// </summary>
    /// <param name="name">配方名（含中文）</param>
    /// <param name="namePos">当前在 name 中的位置</param>
    /// <param name="filter">用户输入的搜索文本</param>
    /// <param name="filterPos">当前在 filter 中的位置</param>
    private static bool MatchFrom(string name, int namePos, string filter, int filterPos)
    {
        // filter 全部匹配完成 → 成功
        if (filterPos >= filter.Length)
            return true;
        // name 耗尽但 filter 还有剩余 → 失败
        if (namePos >= name.Length)
            return false;

        var nameChar = name[namePos];
        var filterChar = filter[filterPos];

        // 1. 直接字符匹配（中文对中文、英文对英文）
        if (CharsEqualIgnoreCase(nameChar, filterChar))
        {
            if (MatchFrom(name, namePos + 1, filter, filterPos + 1))
                return true;
        }

        // 2. 如果当前字是汉字，尝试拼音匹配
        if (IsChinese(nameChar))
        {
            var pinyins = PinyinDict.GetPinyins(nameChar);
            var remaining = filter.Substring(filterPos);

            foreach (var py in pinyins)
            {
                // 2a. 全拼匹配
                if (remaining.StartsWith(py, StringComparison.OrdinalIgnoreCase))
                {
                    if (MatchFrom(name, namePos + 1, filter, filterPos + py.Length))
                        return true;
                }

                // 2b. 首字母匹配
                if (CharsEqualIgnoreCase(filterChar, py[0]))
                {
                    if (MatchFrom(name, namePos + 1, filter, filterPos + 1))
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 判断字符是否为 CJK 统一表意文字（基本区 + 扩展 A 区）。
    /// </summary>
    private static bool IsChinese(char c)
    {
        return (c >= 0x4E00 && c <= 0x9FFF)   // CJK Unified Ideographs
            || (c >= 0x3400 && c <= 0x4DBF);   // CJK Extension A
    }

    private static bool CharsEqualIgnoreCase(char a, char b)
    {
        return char.ToUpperInvariant(a) == char.ToUpperInvariant(b);
    }
}
