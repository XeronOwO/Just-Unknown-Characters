using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace JustUnknownCharacters;

/// <summary>
/// 从嵌入的 pinyin_data.txt 加载汉字→拼音映射字典。
/// 数据来源: PinIn 项目 (https://github.com/Towdium/PinIn)
/// </summary>
public static class PinyinDict
{
	private static readonly Dictionary<char, string[]> Map = new();

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
			// 跳过 ": " 前缀取拼音部分
			var pinyinPart = line.Substring(2).TrimStart(' ', ':');

			var rawParts = pinyinPart.Split(new[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries);
			if (rawParts.Length == 0) continue;

			// 去声调数字，去重
			var cleaned = new List<string>(rawParts.Length);
			foreach (var raw in rawParts)
			{
				var clean = raw.TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				if (clean.Length > 0 && !cleaned.Contains(clean))
					cleaned.Add(clean);
			}

			if (cleaned.Count > 0)
				Map[ch] = cleaned.ToArray();
		}
	}

	/// <summary>
	/// 获取汉字的所有拼音（已去声调）。非汉字或未知字返回空数组。
	/// </summary>
	public static string[] GetPinyins(char c)
	{
		Map.TryGetValue(c, out var result);
		return result ?? new string[0];
	}
}
