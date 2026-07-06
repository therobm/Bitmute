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
			extensions.Add(".webp");
			extensions.Add(".gif");
			extensions.Add(".bitmute");

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

		public static async Task<string> PickOpenPaletteAsync()
		{
			List<string> extensions = new List<string>();
			extensions.Add(".plt");

			Dictionary<DevicePlatform, IEnumerable<string>> typeMap = new Dictionary<DevicePlatform, IEnumerable<string>>();
			typeMap.Add(DevicePlatform.WinUI, extensions);

			PickOptions options = new PickOptions();
			options.PickerTitle = "Import Palette Set";
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

			List<string> bitmuteExtensions = new List<string>();
			bitmuteExtensions.Add(".bitmute");
			picker.FileTypeChoices.Add("Bitmute Project", bitmuteExtensions);

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

			List<string> tgaExtensions = new List<string>();
			tgaExtensions.Add(".tga");
			picker.FileTypeChoices.Add("TGA Image", tgaExtensions);

			List<string> webpExtensions = new List<string>();
			webpExtensions.Add(".webp");
			picker.FileTypeChoices.Add("WebP Image", webpExtensions);

			picker.SuggestedFileName = suggestedName;

			Windows.Storage.StorageFile file = await picker.PickSaveFileAsync();
			if (file == null)
			{
				return null;
			}
			return file.Path;
		}

		public static async Task<string> PickSaveTypedAsync(string suggestedName, string label, string extension)
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

			List<string> extensions = new List<string>();
			extensions.Add(extension);
			picker.FileTypeChoices.Add(label, extensions);
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
