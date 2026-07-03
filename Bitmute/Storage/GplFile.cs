using System.Collections.Generic;
using System.IO;
using System.Text;
using SkiaSharp;

namespace Bitmute.Storage
{
	public static class GplFile
	{
		private static bool IsWhitespace(char character)
		{
			if (character == ' ')
			{
				return true;
			}
			if (character == '\t')
			{
				return true;
			}
			if (character == '\r')
			{
				return true;
			}
			if (character == '\n')
			{
				return true;
			}
			if (character == '\f')
			{
				return true;
			}
			if (character == '\v')
			{
				return true;
			}
			return false;
		}

		private static List<string> SplitTokens(string line)
		{
			List<string> tokens = new List<string>();
			StringBuilder current = new StringBuilder();
			for (int i = 0; i < line.Length; i++)
			{
				char character = line[i];
				if (IsWhitespace(character))
				{
					if (current.Length > 0)
					{
						tokens.Add(current.ToString());
						current.Clear();
					}
				}
				else
				{
					current.Append(character);
				}
			}
			if (current.Length > 0)
			{
				tokens.Add(current.ToString());
			}
			return tokens;
		}

		private static bool ParseComponent(string token, out byte component)
		{
			component = 0;
			int value;
			if (!int.TryParse(token, out value))
			{
				return false;
			}
			if (value < 0)
			{
				value = 0;
			}
			if (value > 255)
			{
				value = 255;
			}
			component = (byte)value;
			return true;
		}

		public static List<SKColor> Read(string path)
		{
			List<SKColor> colors = new List<SKColor>();
			string[] lines = File.ReadAllLines(path);
			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i];
				if (line == null)
				{
					continue;
				}
				string trimmed = line.Trim();
				if (trimmed.Length == 0)
				{
					continue;
				}
				if (trimmed[0] == '#')
				{
					continue;
				}
				if (trimmed.StartsWith("GIMP Palette"))
				{
					continue;
				}
				if (trimmed.StartsWith("Name:"))
				{
					continue;
				}
				if (trimmed.StartsWith("Columns:"))
				{
					continue;
				}
				List<string> tokens = SplitTokens(trimmed);
				if (tokens.Count < 3)
				{
					continue;
				}
				byte red;
				byte green;
				byte blue;
				if (!ParseComponent(tokens[0], out red))
				{
					continue;
				}
				if (!ParseComponent(tokens[1], out green))
				{
					continue;
				}
				if (!ParseComponent(tokens[2], out blue))
				{
					continue;
				}
				colors.Add(new SKColor(red, green, blue, 255));
			}
			return colors;
		}

		public static void Write(string path, List<SKColor> colors)
		{
			StringBuilder builder = new StringBuilder();
			builder.Append("GIMP Palette");
			builder.Append("\n");
			builder.Append("Name: Bitmute");
			builder.Append("\n");
			builder.Append("#");
			builder.Append("\n");
			for (int i = 0; i < colors.Count; i++)
			{
				SKColor color = colors[i];
				builder.Append((int)color.Red);
				builder.Append(" ");
				builder.Append((int)color.Green);
				builder.Append(" ");
				builder.Append((int)color.Blue);
				builder.Append("\t");
				builder.Append("Untitled");
				builder.Append("\n");
			}
			File.WriteAllText(path, builder.ToString());
		}
	}
}
