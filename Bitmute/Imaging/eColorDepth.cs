using SkiaSharp;

namespace Bitmute.Imaging
{
	public enum eColorDepth
	{
		Eight,
		Sixteen,
		ThirtyTwoFloat
	}

	public static class ColorDepthExtensions
	{
		public static SKColorType ToColorType(this eColorDepth depth)
		{
			if (depth == eColorDepth.Eight)
			{
				return SKColorType.Rgba8888;
			}
			if (depth == eColorDepth.Sixteen)
			{
				return SKColorType.Rgba16161616;
			}
			if (depth == eColorDepth.ThirtyTwoFloat)
			{
				return SKColorType.RgbaF32;
			}
			return SKColorType.Rgba8888;
		}

		public static eColorDepth ToColorDepth(this SKColorType colorType)
		{
			if (colorType == SKColorType.Rgba8888)
			{
				return eColorDepth.Eight;
			}
			if (colorType == SKColorType.Rgba16161616)
			{
				return eColorDepth.Sixteen;
			}
			if (colorType == SKColorType.RgbaF32)
			{
				return eColorDepth.ThirtyTwoFloat;
			}
			return eColorDepth.Eight;
		}
	}
}
