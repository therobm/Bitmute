using System;
using System.Runtime.InteropServices;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace Bitmute.Reporting
{
	public static class ReportDiagnostics
	{
		private const string InstallIdPreferenceKey = "report_install_id";

		public static string AppVersion()
		{
			return AppInfo.Current.VersionString;
		}

		public static string OperatingSystem()
		{
			return RuntimeInformation.OSDescription;
		}

		public static string InstallId()
		{
			string existing = Preferences.Default.Get(InstallIdPreferenceKey, "");
			if (existing.Length > 0)
			{
				return existing;
			}
			string generated = Guid.NewGuid().ToString("N");
			Preferences.Default.Set(InstallIdPreferenceKey, generated);
			return generated;
		}
	}
}
