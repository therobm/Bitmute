using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Maui.Storage;

namespace Bitmute.Storage
{
	public static class RecentFiles
	{
		private const string Key = "recent_files";
		private const int MaxEntries = 20;

		private static void RemoveMatching(List<string> paths, string path)
		{
			for (int i = paths.Count - 1; i >= 0; i--)
			{
				if (string.Equals(paths[i], path, StringComparison.OrdinalIgnoreCase))
				{
					paths.RemoveAt(i);
				}
			}
		}

		private static void Save(List<string> paths)
		{
			StringBuilder builder = new StringBuilder();
			for (int i = 0; i < paths.Count; i++)
			{
				if (i > 0)
				{
					builder.Append('\n');
				}
				builder.Append(paths[i]);
			}
			Preferences.Default.Set(Key, builder.ToString());
		}

		public static List<string> List()
		{
			List<string> paths = new List<string>();
			string stored = Preferences.Default.Get(Key, "");
			if (stored == null)
			{
				return paths;
			}
			if (stored.Length == 0)
			{
				return paths;
			}
			string[] segments = stored.Split(new char[] { '\n' });
			for (int i = 0; i < segments.Length; i++)
			{
				string segment = segments[i];
				if (segment == null)
				{
					continue;
				}
				if (segment.Length == 0)
				{
					continue;
				}
				paths.Add(segment);
			}
			return paths;
		}

		public static void Add(string path)
		{
			if (path == null)
			{
				return;
			}
			if (path.Length == 0)
			{
				return;
			}
			List<string> paths = List();
			RemoveMatching(paths, path);
			paths.Insert(0, path);
			if (paths.Count > MaxEntries)
			{
				paths.RemoveRange(MaxEntries, paths.Count - MaxEntries);
			}
			Save(paths);
		}

		public static void Remove(string path)
		{
			if (path == null)
			{
				return;
			}
			if (path.Length == 0)
			{
				return;
			}
			List<string> paths = List();
			RemoveMatching(paths, path);
			Save(paths);
		}

		public static void Clear()
		{
			Preferences.Default.Set(Key, "");
		}
	}
}
