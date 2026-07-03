using System;
using System.IO;
using Microsoft.Maui.Storage;

namespace Bitmute.UI
{
	public static class DebugLog
	{
		private static readonly object s_lock = new object();

		public static string Path()
		{
			return System.IO.Path.Combine(FileSystem.AppDataDirectory, "bitmute-debug.log");
		}

		public static void Write(string message)
		{
			try
			{
				string line = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message + Environment.NewLine;
				lock (s_lock)
				{
					File.AppendAllText(Path(), line);
				}
			}
			catch (Exception error)
			{
				System.Diagnostics.Debug.WriteLine("DebugLog failed: " + error.Message);
			}
		}
	}
}
