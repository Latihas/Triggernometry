using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Triggernometry.Common;

public static class CsvHelper {
	public static List<string[]> ReadCsv(string filePath) {
		var result = new List<string[]>(512);

		using var sr = new StreamReader(filePath, Encoding.UTF8);
		var expected = -1;
		var row = 0;

		// 行内字符缓冲区，整文件复用，减少每行 new char[]
		char[]? sb = null;

		while (ReadLogicalCsvLine(sr) is { } logicalLine) {
			row++;

			string[] arr;

			if (expected == -1) {
				// 第一行：列数未知，用不限制列数的解析器
				arr = ParseFirstLine(logicalLine, ref sb);
				expected = arr.Length;

				for (var i = 0; i < arr.Length; i++) {
					if (string.IsNullOrWhiteSpace(arr[i])) {
						arr[i] = "unk_" + i;
					}
				}
			} else {
				// 后续行：按固定列数解析
				arr = ParseLine(logicalLine, expected, ref sb);

				if (arr.Length != expected) {
					throw new InvalidDataException($"CSV {filePath} 第 {row} 行的列数不一致：期望 {expected}，实际 {arr.Length}");
				}
			}

			result.Add(arr);
		}

		return result;
	}

	// ----------------------------------------------------
	// 带引号的多行字段处理
	// ----------------------------------------------------
	private static string? ReadLogicalCsvLine(StreamReader sr) {
		var line = sr.ReadLine();
		if (line == null)
			return null;

		var sb = new StringBuilder();
		sb.Append(line);

		// 判断当前行的引号数量是否是奇数 → 引号未闭合，说明当前行最后一个字段包含换行
		var quoteCount = CountQuotes(line);

		while (quoteCount % 2 != 0) {
			var next = sr.ReadLine();
			if (next == null)
				break;

			sb.Append("\n");
			sb.Append(next);
			quoteCount += CountQuotes(next);
		}

		return sb.ToString();
	}

	private static int CountQuotes(string s) {
		return s.Count(t => t == '"');
	}

	// -----------------------------------------
	// 第一行用：列数未知 → 用 List 自动扩容
	// 使用外部复用的 char[] 缓冲区
	// -----------------------------------------
	private static string[] ParseFirstLine(string line, ref char[]? sb) {
		var span = line.AsSpan();
		var len = span.Length;

		if (sb == null || sb.Length < len) {
			sb = new char[len];
		}

		var list = new List<string>(32);
		var sbLen = 0;
		var inQuotes = false;

		for (var i = 0; i < len; i++) {
			var c = span[i];

			if (inQuotes) {
				if (c == '"') {
					if (i + 1 < len && span[i + 1] == '"') {
						sb[sbLen++] = '"';
						i++;
					} else {
						inQuotes = false;
					}
				} else {
					sb[sbLen++] = c;
				}
			} else {
				if (c == ',') {
					list.Add(new string(sb, 0, sbLen));
					sbLen = 0;
				} else if (c == '"') {
					inQuotes = true;
				} else {
					sb[sbLen++] = c;
				}
			}
		}

		list.Add(new string(sb, 0, sbLen));
		return list.ToArray();
	}

	// -----------------------------------------
	// 后续行用：已知列数 → 预分配数组，最高性能
	// 使用外部复用的 char[] 缓冲区
	// -----------------------------------------
	private static string[] ParseLine(string line, int expectedColumns, ref char[]? sb) {
		var span = line.AsSpan();
		var len = span.Length;

		if (sb == null || sb.Length < len) {
			sb = new char[len];
		}

		var tmp = new string[expectedColumns];
		var count = 0;

		var sbLen = 0;
		var inQuotes = false;

		for (var i = 0; i < len; i++) {
			var c = span[i];

			if (inQuotes) {
				if (c == '"') {
					if (i + 1 < len && span[i + 1] == '"') {
						sb[sbLen++] = '"';
						i++;
					} else {
						inQuotes = false;
					}
				} else {
					sb[sbLen++] = c;
				}
			} else {
				if (c == ',') {
					tmp[count++] = new string(sb, 0, sbLen);
					sbLen = 0;
				} else if (c == '"') {
					inQuotes = true;
				} else {
					sb[sbLen++] = c;
				}
			}
		}

		tmp[count] = new string(sb, 0, sbLen);
		return tmp;
	}
}