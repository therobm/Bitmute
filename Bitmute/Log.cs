using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;

namespace Bitmute
{
	public static class Log
	{
		private enum eLogLevel
		{
			Info,
			Warn,
			Error
		}

		private static readonly object s_lock = new object();
		private static readonly long s_maxFileSize = 2 * 1024 * 1024;
		private static string s_logPath = "";

		private static string ResolveLogPath()
		{
			if (s_logPath.Length == 0)
			{
				s_logPath = Path.Combine(FileSystem.AppDataDirectory, "bitmute.log");
			}
			return s_logPath;
		}

		private static string LevelText(eLogLevel level)
		{
			if (level == eLogLevel.Info)
			{
				return "INFO";
			}
			if (level == eLogLevel.Warn)
			{
				return "WARN";
			}
			return "ERROR";
		}

		private static void Write(eLogLevel level, string message, string filePath, string memberName)
		{
			lock (s_lock)
			{
				try
				{
					string source = Path.GetFileNameWithoutExtension(filePath);
					string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
					string line = timestamp + " [" + LevelText(level) + "][" + source + "." + memberName + "] " + message;
					Debug.WriteLine(line);
					string path = ResolveLogPath();
					bool exists = File.Exists(path);
					if (exists)
					{
						FileInfo info = new FileInfo(path);
						if (info.Length > s_maxFileSize)
						{
							File.WriteAllText(path, "");
						}
					}
					File.AppendAllText(path, line + "\n");
				}
				catch (Exception logException)
				{
					Debug.WriteLine(logException.Message);
				}
			}
		}

		public static void Info(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
		{
			Write(eLogLevel.Info, message, filePath, memberName);
		}

		public static void Warn(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
		{
			Write(eLogLevel.Warn, message, filePath, memberName);
		}

		public static void Error(string message, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
		{
			Write(eLogLevel.Error, message, filePath, memberName);
		}

		public static void Exception(Exception exception, [CallerFilePath] string filePath = "", [CallerMemberName] string memberName = "")
		{
			Write(eLogLevel.Error, exception.Message + "\n" + exception.StackTrace, filePath, memberName);
		}
	}
}
