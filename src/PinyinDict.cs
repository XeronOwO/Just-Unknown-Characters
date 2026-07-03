using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace JustUnknownCharacters;

/// <summary>
/// 拼音音节 — 声母 + 韵母 + 声调，含模糊音替代。
/// </summary>
public sealed class Syllable
{
	/// <summary>声母替代（含模糊音），如 ["zh","z"]；元音开头为空数组</summary>
	public string[] Initials { get; }
	/// <summary>韵母替代（含模糊音），如 ["eng","en"]；无韵母为空数组</summary>
	public string[] Finals { get; }
	/// <summary>声调数字 "1"~"5"</summary>
	public string Tone { get; }

	public Syllable(string[] initials, string[] finals, string tone)
	{
		Initials = initials;
		Finals = finals;
		Tone = tone;
	}
}

/// <summary>
/// 从嵌入的 pinyin_data.txt 加载汉字→拼音音节映射字典。
/// 数据来源: PinIn 项目 (https://github.com/Towdium/PinIn)
/// </summary>
public static class PinyinDict
{
	private static readonly Syllable[] EmptySyllables = new Syllable[0];
	private static readonly string[] EmptyStrings = new string[0];
	private static readonly Dictionary<char, Syllable[]> Map = new();

	static PinyinDict()
	{
		var assembly = Assembly.GetExecutingAssembly();
		using var stream = assembly.GetManifestResourceStream("JustUnknownCharacters.Resources.pinyin_data.txt");
		if (stream == null)
		{
			Plugin.Log?.LogWarning("pinyin_data.txt not found in embedded resources, pinyin search disabled");
			return;
		}

		using var reader = new StreamReader(stream);
		string line;
		while ((line = reader.ReadLine()) != null)
		{
			if (line.Length < 4) continue;

			var ch = line[0];
			var pinyinPart = line.Substring(2).TrimStart(' ', ':');

			var rawParts = pinyinPart.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
			if (rawParts.Length == 0) continue;

			var syllables = new List<Syllable>(rawParts.Length);
			foreach (var raw in rawParts)
			{
				var syl = SplitPinyin(raw);
				if (syl != null && !ContainsSyllable(syllables, syl))
					syllables.Add(syl);
			}

			if (syllables.Count > 0)
				Map[ch] = syllables.ToArray();
		}
	}

	/// <summary>
	/// 拆分拼音字符串为声母/韵母/声调，并生成模糊音替代。
	/// 格式: "zhong1", "ce4", "an1", "nv3", "m2"
	/// </summary>
	private static Syllable SplitPinyin(string pinyin)
	{
		if (string.IsNullOrEmpty(pinyin) || pinyin.Length < 2)
			return null;

		// 最后一个字符是声调
		var tone = pinyin.Substring(pinyin.Length - 1);
		if (tone[0] < '0' || tone[0] > '9')
			return null; // 非法格式，跳过

		var body = pinyin.Substring(0, pinyin.Length - 1);
		if (body.Length == 0)
			return null;

		string initial;
		string final;

		// 判断是否有声母（首字母不是 a e i o u v）
		var first = body[0];
		var hasInitial = first != 'a' && first != 'e' && first != 'i'
			&& first != 'o' && first != 'u' && first != 'v';

		if (hasInitial)
		{
			// zh, ch, sh 是双字母声母
			var initialLen = (body.Length > 1 && body[1] == 'h') ? 2 : 1;
			initial = body.Substring(0, initialLen);
			final = body.Substring(initialLen);
		}
		else
		{
			initial = "";
			final = body;
		}

		var initials = GenerateInitialAlts(initial);
		var finals = GenerateFinalAlts(final);

		return new Syllable(initials, finals, tone);
	}

	/// <summary>
	/// 生成声母的模糊音替代（zh↔z, ch↔c, sh↔s, u↔v）。
	/// 遵循 PinIn Phoneme.reload 的规则。
	/// </summary>
	private static string[] GenerateInitialAlts(string initial)
	{
		if (initial.Length == 0)
			return EmptyStrings;

		var set = new HashSet<string> { initial };

		// c <-> ch
		if (initial[0] == 'c')
		{
			set.Add("c");
			set.Add("ch");
		}
		// s <-> sh
		if (initial[0] == 's')
		{
			set.Add("s");
			set.Add("sh");
		}
		// z <-> zh
		if (initial[0] == 'z')
		{
			set.Add("z");
			set.Add("zh");
		}
		// v -> u (ü 替代输入)
		if (initial[0] == 'v')
			set.Add("u" + initial.Substring(1));

		var result = new string[set.Count];
		set.CopyTo(result);
		return result;
	}

	/// <summary>
	/// 生成韵母的模糊音替代（ang↔an, eng↔en, ing↔in）。
	/// 遵循 PinIn Phoneme.reload 的规则。
	/// </summary>
	private static string[] GenerateFinalAlts(string final)
	{
		if (final.Length == 0)
			return EmptyStrings;

		var set = new HashSet<string> { final };

		// ang -> an (去掉 g)
		if (final.EndsWith("ang", StringComparison.Ordinal))
			set.Add(final.Substring(0, final.Length - 1));
		// eng -> en
		if (final.EndsWith("eng", StringComparison.Ordinal))
			set.Add(final.Substring(0, final.Length - 1));
		// ing -> in
		if (final.EndsWith("ing", StringComparison.Ordinal))
			set.Add(final.Substring(0, final.Length - 1));
		// an -> ang (加 g)
		if (final.EndsWith("an", StringComparison.Ordinal))
			set.Add(final + "g");
		// en -> eng
		if (final.EndsWith("en", StringComparison.Ordinal))
			set.Add(final + "g");
		// in -> ing
		if (final.EndsWith("in", StringComparison.Ordinal))
			set.Add(final + "g");

		var result = new string[set.Count];
		set.CopyTo(result);
		return result;
	}

	/// <summary>
	/// 检查音节列表中是否已存在等效音节（仅比较原始声母/韵母，忽略模糊音替代）。
	/// </summary>
	private static bool ContainsSyllable(List<Syllable> list, Syllable syl)
	{
		foreach (var existing in list)
		{
			if (existing.Initials.Length > 0 && syl.Initials.Length > 0
				&& existing.Initials[0] == syl.Initials[0]
				&& existing.Finals.Length > 0 && syl.Finals.Length > 0
				&& existing.Finals[0] == syl.Finals[0]
				&& existing.Tone == syl.Tone)
				return true;
		}
		return false;
	}

	/// <summary>
	/// 获取汉字的拼音音节列表（含模糊音替代）。
	/// </summary>
	public static Syllable[] GetSyllables(char c)
	{
		Map.TryGetValue(c, out var result);
		return result ?? EmptySyllables;
	}
}
