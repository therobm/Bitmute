using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.UI.Input;
using SkiaSharp;

namespace Bitmute.Platforms.Windows
{
	public static class NativeCursors
	{
		private const uint BI_RGB = 0;
		private const uint DIB_RGB_COLORS = 0;

		private static Dictionary<string, InputCursor> s_cache = new Dictionary<string, InputCursor>();
		private static InputCursor s_transparent;
		private static bool s_transparentBuilt;
		private static IInputCursorStaticsInterop s_interop;
		private static bool s_interopResolved;

		[StructLayout(LayoutKind.Sequential)]
		private struct ICONINFO
		{
			public int fIcon;
			public int xHotspot;
			public int yHotspot;
			public IntPtr hbmMask;
			public IntPtr hbmColor;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct BITMAPV5HEADER
		{
			public uint bV5Size;
			public int bV5Width;
			public int bV5Height;
			public ushort bV5Planes;
			public ushort bV5BitCount;
			public uint bV5Compression;
			public uint bV5SizeImage;
			public int bV5XPelsPerMeter;
			public int bV5YPelsPerMeter;
			public uint bV5ClrUsed;
			public uint bV5ClrImportant;
			public uint bV5RedMask;
			public uint bV5GreenMask;
			public uint bV5BlueMask;
			public uint bV5AlphaMask;
			public uint bV5CSType;
			public int bV5Endpoint0;
			public int bV5Endpoint1;
			public int bV5Endpoint2;
			public int bV5Endpoint3;
			public int bV5Endpoint4;
			public int bV5Endpoint5;
			public int bV5Endpoint6;
			public int bV5Endpoint7;
			public int bV5Endpoint8;
			public uint bV5GammaRed;
			public uint bV5GammaGreen;
			public uint bV5GammaBlue;
			public uint bV5Intent;
			public uint bV5ProfileData;
			public uint bV5ProfileSize;
			public uint bV5Reserved;
		}

		[ComImport]
		[Guid("ac6f5065-90c4-46ce-beb7-05e138e54117")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		private interface IInputCursorStaticsInterop
		{
			void GetIids(out uint iidCount, out IntPtr iids);
			void GetRuntimeClassName(out IntPtr className);
			void GetTrustLevel(out int trustLevel);
			IntPtr CreateFromHCursor(IntPtr hcursor);
		}

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr CreateIconIndirect(ref ICONINFO iconInfo);

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateBitmap(int width, int height, uint planes, uint bitCount, IntPtr bits);

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPV5HEADER header, uint usage, out IntPtr bits, IntPtr section, uint offset);

		[DllImport("gdi32.dll")]
		private static extern int DeleteObject(IntPtr handle);

		[DllImport("user32.dll")]
		private static extern IntPtr GetDC(IntPtr window);

		[DllImport("user32.dll")]
		private static extern int ReleaseDC(IntPtr window, IntPtr dc);

		public static InputCursor FromBitmap(SKBitmap source, string cacheKey, int hotspotX, int hotspotY, int targetSize)
		{
			if (source == null)
			{
				return null;
			}
			InputCursor cached;
			if (s_cache.TryGetValue(cacheKey, out cached))
			{
				return cached;
			}
			SKBitmap scaled = source;
			int scaledHotspotX = hotspotX;
			int scaledHotspotY = hotspotY;
			bool disposeScaled = false;
			if (source.Width != targetSize || source.Height != targetSize)
			{
				scaled = new SKBitmap(new SKImageInfo(targetSize, targetSize, SKColorType.Bgra8888, SKAlphaType.Unpremul));
				source.ScalePixels(scaled, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
				scaledHotspotX = (int)Math.Round((double)hotspotX * targetSize / source.Width);
				scaledHotspotY = (int)Math.Round((double)hotspotY * targetSize / source.Height);
				disposeScaled = true;
			}
			InputCursor built = BuildFromBitmap(scaled, scaledHotspotX, scaledHotspotY);
			if (disposeScaled)
			{
				scaled.Dispose();
			}
			if (built != null)
			{
				s_cache[cacheKey] = built;
			}
			return built;
		}

		public static InputCursor Transparent()
		{
			if (s_transparentBuilt)
			{
				return s_transparent;
			}
			s_transparentBuilt = true;
			SKBitmap blank = new SKBitmap(new SKImageInfo(2, 2, SKColorType.Bgra8888, SKAlphaType.Unpremul));
			blank.Erase(new SKColor(0,0,0,0));//SKColors.Transparent);
			s_transparent = BuildFromBitmap(blank, 0, 0);
			blank.Dispose();
			return s_transparent;
		}

		private static InputCursor BuildFromBitmap(SKBitmap bitmap, int hotspotX, int hotspotY)
		{
			IntPtr hcursor = CreateHCursor(bitmap, hotspotX, hotspotY);
			if (hcursor == IntPtr.Zero)
			{
				return null;
			}
			try
			{
				IInputCursorStaticsInterop interop = Interop();
				if (interop == null)
				{
					return null;
				}
				IntPtr abi = interop.CreateFromHCursor(hcursor);
				if (abi == IntPtr.Zero)
				{
					return null;
				}
				InputCursor cursor = WinRT.MarshalInspectable<InputCursor>.FromAbi(abi);
				Marshal.Release(abi);
				return cursor;
			}
			catch (Exception error)
			{
				Log.Exception(error);
				return null;
			}
		}

		private static IntPtr CreateHCursor(SKBitmap bitmap, int hotspotX, int hotspotY)
		{
			int width = bitmap.Width;
			int height = bitmap.Height;
			if (width <= 0 || height <= 0)
			{
				return IntPtr.Zero;
			}
			try
			{
				BITMAPV5HEADER header = new BITMAPV5HEADER();
				header.bV5Size = (uint)Marshal.SizeOf<BITMAPV5HEADER>();
				header.bV5Width = width;
				header.bV5Height = -height;
				header.bV5Planes = 1;
				header.bV5BitCount = 32;
				header.bV5Compression = BI_RGB;
				header.bV5RedMask = 0x00FF0000;
				header.bV5GreenMask = 0x0000FF00;
				header.bV5BlueMask = 0x000000FF;
				header.bV5AlphaMask = 0xFF000000;
				IntPtr hdc = GetDC(IntPtr.Zero);
				IntPtr bits;
				IntPtr hbmColor = CreateDIBSection(hdc, ref header, DIB_RGB_COLORS, out bits, IntPtr.Zero, 0);
				ReleaseDC(IntPtr.Zero, hdc);
				if (hbmColor == IntPtr.Zero || bits == IntPtr.Zero)
				{
					if (hbmColor != IntPtr.Zero)
					{
						DeleteObject(hbmColor);
					}
					return IntPtr.Zero;
				}
				SKImageInfo targetInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
				SKPixmap pixmap = bitmap.PeekPixels();
				if (pixmap == null)
				{
					DeleteObject(hbmColor);
					return IntPtr.Zero;
				}
				pixmap.ReadPixels(targetInfo, bits, width * 4);
				pixmap.Dispose();
				int maskStride = ((width + 15) / 16) * 2;
				byte[] maskData = new byte[maskStride * height];
				GCHandle maskHandle = GCHandle.Alloc(maskData, GCHandleType.Pinned);
				IntPtr hbmMask = CreateBitmap(width, height, 1, 1, maskHandle.AddrOfPinnedObject());
				maskHandle.Free();
				ICONINFO iconInfo = new ICONINFO();
				iconInfo.fIcon = 0;
				iconInfo.xHotspot = hotspotX;
				iconInfo.yHotspot = hotspotY;
				iconInfo.hbmMask = hbmMask;
				iconInfo.hbmColor = hbmColor;
				IntPtr hcursor = CreateIconIndirect(ref iconInfo);
				DeleteObject(hbmColor);
				DeleteObject(hbmMask);
				return hcursor;
			}
			catch (Exception error)
			{
				Log.Exception(error);
				return IntPtr.Zero;
			}
		}

		private static IInputCursorStaticsInterop Interop()
		{
			if (s_interopResolved)
			{
				return s_interop;
			}
			s_interopResolved = true;
			try
			{
				WinRT.IObjectReference reference = WinRT.ActivationFactory.Get("Microsoft.UI.Input.InputCursor");
				s_interop = (IInputCursorStaticsInterop)Marshal.GetObjectForIUnknown(reference.ThisPtr);
			}
			catch (Exception error)
			{
				Log.Exception(error);
				s_interop = null;
			}
			return s_interop;
		}
	}
}
