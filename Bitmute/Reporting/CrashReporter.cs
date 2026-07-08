using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;

namespace Bitmute.Reporting
{
	public static class CrashReporter
	{
		private static bool s_installed;
		private static readonly string[] s_noFiles = new string[0];

		public static void Install()
		{
			if (s_installed)
			{
				return;
			}
			s_installed = true;
			AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
			TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
		}

		public static void RecordException(Exception exception)
		{
			if (exception == null)
			{
				return;
			}
			try
			{
				string directory = CrashDirectory();
				Directory.CreateDirectory(directory);
				byte[] payload = BuildCrashFile(exception);
				string fileName = "crash-" + Guid.NewGuid().ToString("N") + ".json";
				File.WriteAllBytes(Path.Combine(directory, fileName), payload);
			}
			catch (Exception writeException)
			{
				System.Diagnostics.Debug.WriteLine("CrashReporter failed to persist crash: " + writeException.Message);
			}
		}

		public static bool HasPendingCrashes()
		{
			return PendingCrashFiles().Length > 0;
		}

		public static async Task SendAllPendingAsync()
		{
			string[] files = PendingCrashFiles();
			int count = files.Length;
			for (int index = 0; index < count; index++)
			{
				string path = files[index];
				bool sent = await SendCrashFileAsync(path);
				if (sent)
				{
					TryDeleteFile(path);
				}
			}
		}

		public static void DiscardPendingCrashes()
		{
			string[] files = PendingCrashFiles();
			int count = files.Length;
			for (int index = 0; index < count; index++)
			{
				TryDeleteFile(files[index]);
			}
		}

		private static void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
		{
			RecordException(eventArgs.ExceptionObject as Exception);
		}

		private static void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs eventArgs)
		{
			RecordException(eventArgs.Exception);
			eventArgs.SetObserved();
		}

		private static string CrashDirectory()
		{
			return Path.Combine(FileSystem.AppDataDirectory, "crashes");
		}

		private static string[] PendingCrashFiles()
		{
			try
			{
				string directory = CrashDirectory();
				if (!Directory.Exists(directory))
				{
					return s_noFiles;
				}
				return Directory.GetFiles(directory, "crash-*.json");
			}
			catch (Exception listException)
			{
				System.Diagnostics.Debug.WriteLine("CrashReporter failed to list crashes: " + listException.Message);
				return s_noFiles;
			}
		}

		private static async Task<bool> SendCrashFileAsync(string path)
		{
			string title;
			string diagnostics;
			try
			{
				string text = File.ReadAllText(path);
				JsonDocument document = JsonDocument.Parse(text);
				title = ReadStringProperty(document.RootElement, "Title");
				diagnostics = ReadStringProperty(document.RootElement, "Diagnostics");
			}
			catch (Exception readException)
			{
				System.Diagnostics.Debug.WriteLine("CrashReporter failed to read crash file: " + readException.Message);
				return false;
			}
			if (title.Length == 0)
			{
				title = "Crash report";
			}
			return await BugReportClient.SubmitAsync(eReportKind.CrashReport, title, "", diagnostics);
		}

		private static string ReadStringProperty(JsonElement element, string propertyName)
		{
			JsonElement value;
			if (element.TryGetProperty(propertyName, out value))
			{
				if (value.ValueKind == JsonValueKind.String)
				{
					string text = value.GetString();
					if (text == null)
					{
						return "";
					}
					return text;
				}
			}
			return "";
		}

		private static void TryDeleteFile(string path)
		{
			try
			{
				File.Delete(path);
			}
			catch (Exception deleteException)
			{
				System.Diagnostics.Debug.WriteLine("CrashReporter failed to delete crash file: " + deleteException.Message);
			}
		}

		private static byte[] BuildCrashFile(Exception exception)
		{
			string title = BuildTitle(exception);
			string diagnostics = BuildDiagnostics(exception);
			MemoryStream stream = new MemoryStream();
			Utf8JsonWriter writer = new Utf8JsonWriter(stream);
			writer.WriteStartObject();
			writer.WriteString("Title", title);
			writer.WriteString("Diagnostics", diagnostics);
			writer.WriteEndObject();
			writer.Flush();
			return stream.ToArray();
		}

		private static string BuildTitle(Exception exception)
		{
			string typeName = exception.GetType().Name;
			string message = exception.Message;
			if (message == null)
			{
				message = "";
			}
			if (message.Length > 80)
			{
				message = message.Substring(0, 80);
			}
			if (message.Length == 0)
			{
				return "Crash: " + typeName;
			}
			return "Crash: " + typeName + ": " + message;
		}

		private static string BuildDiagnostics(Exception exception)
		{
			string details = exception.ToString();
			if (details.Length > 6000)
			{
				details = details.Substring(0, 6000);
			}
			StringBuilder builder = new StringBuilder();
			builder.Append("Version: ");
			builder.Append(SafeVersion());
			builder.Append("\n");
			builder.Append("OS: ");
			builder.Append(SafeOs());
			builder.Append("\n\n");
			builder.Append(details);
			return builder.ToString();
		}

		private static string SafeVersion()
		{
			try
			{
				return ReportDiagnostics.AppVersion();
			}
			catch (Exception versionException)
			{
				System.Diagnostics.Debug.WriteLine("CrashReporter version lookup failed: " + versionException.Message);
				return "unknown";
			}
		}

		private static string SafeOs()
		{
			try
			{
				return ReportDiagnostics.OperatingSystem();
			}
			catch (Exception osException)
			{
				System.Diagnostics.Debug.WriteLine("CrashReporter OS lookup failed: " + osException.Message);
				return "unknown";
			}
		}
	}
}
