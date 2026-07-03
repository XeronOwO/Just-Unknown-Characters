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
	/// 保留原始 Contains 行为并叠加拼音匹配。
	/// 供 Harmony Transpiler 使用。
	/// </summary>
	public static bool Match(string haystack, string needle, StringComparison comparison)
	{
		return haystack.IndexOf(needle, comparison) >= 0 || Contains(haystack, needle);
	}

	/// <summary>
	/// 判断 <paramref name="name"/> 是否与 <paramref name="filter"/> 拼音匹配。
	/// 从 name 的每个位置开始尝试，实现 Contains 语义。
	/// </summary>
	public static bool Contains(string name, string filter)
	{
		if (string.IsNullOrEmpty(filter))
			return true;
		if (string.IsNullOrEmpty(name))
			return false;

		for (var start = 0; start < name.Length; start++)
			if (MatchFrom(name, start, filter, 0))
				return true;
		return false;
	}

	/// <summary>
	/// 递归回溯匹配。name 耗尽返回 true（支持部分匹配，如 sh→绳）。
	/// </summary>
	private static bool MatchFrom(string name, int namePos, string filter, int filterPos)
	{
		if (filterPos >= filter.Length)
			return true;
		if (namePos >= name.Length)
			return true;

		var nameChar = name[namePos];
		var filterChar = filter[filterPos];

		// 直接字符匹配
		if (CharsEqualIgnoreCase(nameChar, filterChar))
			if (MatchFrom(name, namePos + 1, filter, filterPos + 1))
				return true;

		// 拼音匹配
		if (IsChinese(nameChar))
		{
			var pinyins = PinyinDict.GetPinyins(nameChar);
			var remaining = filter.Substring(filterPos);

			foreach (var py in pinyins)
			{
				// 全拼
				if (remaining.StartsWith(py, StringComparison.OrdinalIgnoreCase))
					if (MatchFrom(name, namePos + 1, filter, filterPos + py.Length))
						return true;

				// 首字母
				if (CharsEqualIgnoreCase(filterChar, py[0]))
					if (MatchFrom(name, namePos + 1, filter, filterPos + 1))
						return true;
			}
		}

		return false;
	}

	private static bool IsChinese(char c)
	{
		return (c >= 0x4E00 && c <= 0x9FFF)
			|| (c >= 0x3400 && c <= 0x4DBF);
	}

	private static bool CharsEqualIgnoreCase(char a, char b)
	{
		return char.ToUpperInvariant(a) == char.ToUpperInvariant(b);
	}
}
