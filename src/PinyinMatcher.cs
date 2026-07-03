using System;
using System.Collections.Generic;

namespace JustUnknownCharacters;

/// <summary>
/// 拼音搜索匹配器 — PinIn NFA 回溯算法的 C# 移植。
/// 支持: 全拼、首字母、中英混合、多音字、模糊音、声调容错。
/// 来源: Just Enough Characters / PinIn (https://github.com/Towdium/PinIn)
/// </summary>
public static class PinyinMatcher
{
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
	/// 递归回溯 NFA 匹配。
	/// PinIn Matcher.check() 的移植。
	/// </summary>
	/// <param name="name">被搜索文本</param>
	/// <param name="namePos">name 中的当前位置</param>
	/// <param name="filter">搜索查询</param>
	/// <param name="filterPos">filter 中的当前位置</param>
	private static bool MatchFrom(string name, int namePos, string filter, int filterPos)
	{
		// Base case 1: filter 耗尽 → partial 模式下始终成功
		if (filterPos >= filter.Length)
			return true;

		// Base case 2: name 耗尽但 filter 还有字符 → 失败
		if (namePos >= name.Length)
			return false;

		var ch = name[namePos];
		var syllables = PinyinDict.GetSyllables(ch);
		var consumed = CharMatch(ch, syllables, filter, filterPos);

		var isLastChar = namePos == name.Length - 1;

		if (isLastChar)
		{
			// 最后一个字符：必须精确消耗剩余的 filter 字符数
			var remaining = filter.Length - filterPos;
			return consumed.Contains(remaining);
		}
		else
		{
			// 递归尝试每种消耗长度
			foreach (var n in consumed)
			{
				if (n > 0 && MatchFrom(name, namePos + 1, filter, filterPos + n))
					return true;
			}
			return false;
		}
	}

	/// <summary>
	/// 尝试用一个汉字匹配 filter 从 filterPos 开始的前缀。
	/// 返回所有可能的消耗长度（PinIn IndexSet 的简化版）。
	/// PinIn Char.match() + Pinyin.match() 的移植。
	/// </summary>
	private static HashSet<int> CharMatch(char ch, Syllable[] syllables, string filter, int filterPos)
	{
		var result = new HashSet<int>();

		// 1. 字面匹配：中文字符直接匹配自身，也适用于英文/数字
		//    对应 PinIn: str.charAt(start) == ch ? ONE : NONE
		if (filterPos < filter.Length && CharsEqual(filter[filterPos], ch))
			result.Add(1);

		// 非中文或无语义拼音 → 只有字面匹配可用
		if (syllables.Length == 0)
			return result;

		// 2. 拼音匹配
		foreach (var syl in syllables)
		{
			// 顺序音素 NFA 匹配
			// 对应 PinIn Pinyin.match() 的 QuanPin 分支
			var states = new HashSet<int>();
			states.Add(0);

			// 声母匹配（空声母 = pass-through）
			if (syl.Initials.Length > 0)
			{
				states = MatchPhonemeSet(syl.Initials, filter, filterPos, states);
				if (states.Count == 0)
					continue; // 声母必须匹配
			}

			// 累积中间结果 → 声调容错
			foreach (var s in states)
				result.Add(s);

			// 韵母匹配
			if (syl.Finals.Length > 0)
			{
				states = MatchPhonemeSet(syl.Finals, filter, filterPos, states);
				if (states.Count == 0)
					continue; // 韵母必须匹配
			}

			// 累积中间结果 → 声调容错
			foreach (var s in states)
				result.Add(s);

			// 声调匹配（可选失败 — 中间结果已保存）
			if (syl.Tone.Length > 0)
			{
				var toneStates = MatchPhoneme(syl.Tone, filter, filterPos, states);
				foreach (var s in toneStates)
					result.Add(s);
			}

			// 3. 首字母序列匹配（Sequence）
			//    对应 PinIn: if (sequence && phonemes[0].matchSequence(...)) ret.set(1);
			//    只要 filter 当前位置匹配任意声母的首字母，就可以只消耗 1 字符
			if (syl.Initials.Length > 0 && filterPos < filter.Length)
			{
				foreach (var init in syl.Initials)
				{
					if (init.Length > 0 && CharsEqual(init[0], filter[filterPos]))
					{
						result.Add(1);
						break;
					}
				}
			}
		}

		return result;
	}

	/// <summary>
	/// 将一组音素替代匹配到 filter，返回新的消耗长度集合。
	/// 对应 PinIn Phoneme.match(String, IndexSet, int, boolean) — NFA 状态转移。
	/// </summary>
	/// <param name="phonemes">音素的模糊替代（如 ["zh","z"]）</param>
	/// <param name="filter">搜索查询</param>
	/// <param name="start">filter 中音素匹配的起始位置</param>
	/// <param name="offsets">已有的消耗长度集合</param>
	private static HashSet<int> MatchPhonemeSet(string[] phonemes, string filter, int start, HashSet<int> offsets)
	{
		var result = new HashSet<int>();

		foreach (var offset in offsets)
		{
			foreach (var ph in phonemes)
			{
				if (ph.Length == 0)
				{
					// 空音素 = pass-through
					result.Add(offset);
					continue;
				}

				var pos = start + offset;

				// 完整匹配：音素所有字符都匹配
				if (pos + ph.Length <= filter.Length)
				{
					var fullMatch = true;
					for (var i = 0; i < ph.Length; i++)
					{
						if (!CharsEqual(ph[i], filter[pos + i]))
						{
							fullMatch = false;
							break;
						}
					}
					if (fullMatch)
						result.Add(offset + ph.Length);
				}

				// 末尾部分匹配：音素部分匹配且到达 filter 末尾
				// 对应 PinIn: partial && start + size == source.length()
				if (pos < filter.Length)
				{
					var matchLen = 0;
					var maxLen = Math.Min(ph.Length, filter.Length - pos);
					for (var i = 0; i < maxLen; i++)
					{
						if (!CharsEqual(ph[i], filter[pos + i]))
							break;
						matchLen++;
					}
					if (pos + matchLen == filter.Length)
						result.Add(offset + matchLen);
				}
			}
		}

		return result;
	}

	/// <summary>
	/// 匹配单个音素字符串，简化版（用于声调）。
	/// 对应 PinIn Phoneme.match(String, int, boolean)。
	/// </summary>
	private static HashSet<int> MatchPhoneme(string phoneme, string filter, int start, HashSet<int> offsets)
	{
		var result = new HashSet<int>();

		foreach (var offset in offsets)
		{
			var pos = start + offset;

			// 完整匹配
			if (pos + phoneme.Length <= filter.Length)
			{
				var fullMatch = true;
				for (var i = 0; i < phoneme.Length; i++)
				{
					if (!CharsEqual(phoneme[i], filter[pos + i]))
					{
						fullMatch = false;
						break;
					}
				}
				if (fullMatch)
					result.Add(offset + phoneme.Length);
			}
		}

		return result;
	}

	private static bool CharsEqual(char a, char b)
	{
		return char.ToUpperInvariant(a) == char.ToUpperInvariant(b);
	}
}
