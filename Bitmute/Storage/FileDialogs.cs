using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace Bitmute.Storage
{
	public static class FileDialogs
	{
		public static async Task<string> PickOpenAsync()
		{
			List<string> extensions = new List<string>();
			extensions.Add(".png");
			extensions.Add(".jpg");
			extensions.Add(".jpeg");
			extensions.Add(".bmp");
			extensions.Add(".tga");

			Dictionary<DevicePlatform, IEnumerable<string>> typeMap = new Dictionary<DevicePlatform, IEnumerable<string>>();
			typeMap.Add(DevicePlatform.WinUI, extensions);

			PickOptions options = new PickOptions();
			options.PickerTitle = "Open Image";
			options.FileTypes = new FilePickerFileType(typeMap);

			FileResult result = await FilePicker.Default.PickAsync(options);
			if (result == null)
			{
				return null;
			}
			return result.FullPath;
		}

		public static async Task<string> PickSaveAsync(string suggestedName)
		{
			IReadOnlyList<Window> windows = Application.Current.Windows;
			if (windows.Count == 0)
			{
				return null;
			}
			object platformView = windows[0].Handler.PlatformView;
			Microsoft.UI.Xaml.Window nativeWindow = platformView as Microsoft.UI.Xaml.Window;
			if (nativeWindow == null)
			{
				return null;
			}
			IntPtr handle = WinRT.Interop.WindowNative.GetWindowHandle(nativeWindow);

			Windows.Storage.Pickers.FileSavePicker picker = new Windows.Storage.Pickers.FileSavePicker();
			WinRT.Interop.InitializeWithWindow.Initialize(picker, handle);

			List<string> pngExtensions = new List<string>();
			pngExtensions.Add(".png");
			picker.FileTypeChoices.Add("PNG Image", pngExtensions);

			List<string> jpegExtensions = new List<string>();
			jpegExtensions.Add(".jpg");
			jpegExtensions.Add(".jpeg");
			picker.FileTypeChoices.Add("JPEG Image", jpegExtensions);

			List<string> bmpExtensions = new List<string>();
			bmpExtensions.Add(".bmp");
			picker.FileTypeChoices.Add("Bitmap Image", bmpExtensions);

			picker.SuggestedFileName = suggestedName;

			Windows.Storage.StorageFile file = await picker.PickSaveFileAsync();
			if (file == null)
			{
				return null;
			}
			return file.Path;
		}
	}
}
