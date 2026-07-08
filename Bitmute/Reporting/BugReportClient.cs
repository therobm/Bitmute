using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace Bitmute.Reporting
{
	public static class BugReportClient
	{
		private static readonly HttpClient s_httpClient = new HttpClient();

		static BugReportClient()
		{
			s_httpClient.Timeout = TimeSpan.FromSeconds(15.0);
		}

		public static async Task<bool> SubmitAsync(eReportKind kind, string title, string description, string diagnostics)
		{
			string token = ReportConfig.ReportToken();
			if (token.Length == 0)
			{
				return false;
			}
			try
			{
				byte[] payload = BuildPayload(kind, title, description, diagnostics);
				HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, ReportConfig.IntakeUrl);
				request.Headers.Add("X-Report-Token", token);
				ByteArrayContent content = new ByteArrayContent(payload);
				content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
				request.Content = content;
				HttpResponseMessage response = await s_httpClient.SendAsync(request);
				return response.IsSuccessStatusCode;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static byte[] BuildPayload(eReportKind kind, string title, string description, string diagnostics)
		{
			MemoryStream stream = new MemoryStream();
			Utf8JsonWriter writer = new Utf8JsonWriter(stream);
			writer.WriteStartObject();
			writer.WriteString("AppId", ReportConfig.AppId);
			writer.WriteString("AppVersion", ReportDiagnostics.AppVersion());
			writer.WriteString("Os", ReportDiagnostics.OperatingSystem());
			writer.WriteString("InstallId", ReportDiagnostics.InstallId());
			writer.WriteString("ReportKind", kind.ToString());
			writer.WriteString("Title", title);
			writer.WriteString("Description", description);
			writer.WriteString("Diagnostics", diagnostics);
			writer.WriteEndObject();
			writer.Flush();
			return stream.ToArray();
		}
	}
}
