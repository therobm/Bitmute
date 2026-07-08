using System.Reflection;

namespace Bitmute.Reporting
{
	public static class ReportConfig
	{
		public const string IntakeUrl = "https://bugs.bitmute.ca/api/report";
		public const string AppId = "bitmute";

		private static string s_reportToken;
		private static bool s_reportTokenResolved;

		public static string ReportToken()
		{
			if (!s_reportTokenResolved)
			{
				s_reportToken = ResolveReportToken();
				s_reportTokenResolved = true;
			}
			return s_reportToken;
		}

		private static string ResolveReportToken()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			object[] attributes = assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
			int count = attributes.Length;
			for (int index = 0; index < count; index++)
			{
				AssemblyMetadataAttribute metadata = (AssemblyMetadataAttribute)attributes[index];
				if (metadata.Key == "ReportToken")
				{
					if (metadata.Value == null)
					{
						return "";
					}
					return metadata.Value;
				}
			}
			return "";
		}
	}
}
