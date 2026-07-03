namespace Bitmute.Imaging
{
	public static class BlendModes
	{
		private static int Clamp(int value)
		{
			if (value < 0)
			{
				return 0;
			}
			if (value > 255)
			{
				return 255;
			}
			return value;
		}

		private static int Luma(byte red, byte green, byte blue)
		{
			int weighted = (299 * red) + (587 * green) + (114 * blue);
			return weighted / 1000;
		}

		private static int ColorBurn(byte baseChannel, int source)
		{
			if (source <= 0)
			{
				return 0;
			}
			int scaled = ((255 - baseChannel) * 255) / source;
			if (scaled > 255)
			{
				scaled = 255;
			}
			return 255 - scaled;
		}

		private static int ColorDodge(byte baseChannel, int source)
		{
			if (source >= 255)
			{
				return 255;
			}
			int scaled = (baseChannel * 255) / (255 - source);
			if (scaled > 255)
			{
				scaled = 255;
			}
			return scaled;
		}

		private static int VividLightChannel(byte baseChannel, byte blendChannel)
		{
			if (blendChannel < 128)
			{
				return ColorBurn(baseChannel, 2 * blendChannel);
			}
			return ColorDodge(baseChannel, 2 * (blendChannel - 128));
		}

		private static int LinearBurnChannel(byte baseChannel, byte blendChannel)
		{
			return Clamp(baseChannel + blendChannel - 255);
		}

		private static int LinearLightChannel(byte baseChannel, byte blendChannel)
		{
			return Clamp(baseChannel + (2 * blendChannel) - 255);
		}

		private static int PinLightChannel(byte baseChannel, byte blendChannel)
		{
			if (blendChannel < 128)
			{
				int lowered = 2 * blendChannel;
				if (baseChannel < lowered)
				{
					return baseChannel;
				}
				return lowered;
			}
			int raised = 2 * (blendChannel - 128);
			if (baseChannel > raised)
			{
				return baseChannel;
			}
			return raised;
		}

		private static int SubtractChannel(byte baseChannel, byte blendChannel)
		{
			return Clamp(baseChannel - blendChannel);
		}

		private static int DivideChannel(byte baseChannel, byte blendChannel)
		{
			if (blendChannel == 0)
			{
				return 255;
			}
			int scaled = (baseChannel * 255) / blendChannel;
			if (scaled > 255)
			{
				scaled = 255;
			}
			return scaled;
		}

		private static int HardMixChannel(byte baseChannel, byte blendChannel)
		{
			int vivid = VividLightChannel(baseChannel, blendChannel);
			if (vivid >= 128)
			{
				return 255;
			}
			return 0;
		}

		public static void Blend(eBlendMode mode, byte baseR, byte baseG, byte baseB, byte blendR, byte blendG, byte blendB, out byte outR, out byte outG, out byte outB)
		{
			if (mode == eBlendMode.LinearBurn)
			{
				outR = (byte)LinearBurnChannel(baseR, blendR);
				outG = (byte)LinearBurnChannel(baseG, blendG);
				outB = (byte)LinearBurnChannel(baseB, blendB);
				return;
			}
			if (mode == eBlendMode.DarkerColor)
			{
				int baseLuma = Luma(baseR, baseG, baseB);
				int blendLuma = Luma(blendR, blendG, blendB);
				if (baseLuma <= blendLuma)
				{
					outR = baseR;
					outG = baseG;
					outB = baseB;
				}
				else
				{
					outR = blendR;
					outG = blendG;
					outB = blendB;
				}
				return;
			}
			if (mode == eBlendMode.LighterColor)
			{
				int baseLuma = Luma(baseR, baseG, baseB);
				int blendLuma = Luma(blendR, blendG, blendB);
				if (baseLuma >= blendLuma)
				{
					outR = baseR;
					outG = baseG;
					outB = baseB;
				}
				else
				{
					outR = blendR;
					outG = blendG;
					outB = blendB;
				}
				return;
			}
			if (mode == eBlendMode.VividLight)
			{
				outR = (byte)VividLightChannel(baseR, blendR);
				outG = (byte)VividLightChannel(baseG, blendG);
				outB = (byte)VividLightChannel(baseB, blendB);
				return;
			}
			if (mode == eBlendMode.LinearLight)
			{
				outR = (byte)LinearLightChannel(baseR, blendR);
				outG = (byte)LinearLightChannel(baseG, blendG);
				outB = (byte)LinearLightChannel(baseB, blendB);
				return;
			}
			if (mode == eBlendMode.PinLight)
			{
				outR = (byte)PinLightChannel(baseR, blendR);
				outG = (byte)PinLightChannel(baseG, blendG);
				outB = (byte)PinLightChannel(baseB, blendB);
				return;
			}
			if (mode == eBlendMode.HardMix)
			{
				outR = (byte)HardMixChannel(baseR, blendR);
				outG = (byte)HardMixChannel(baseG, blendG);
				outB = (byte)HardMixChannel(baseB, blendB);
				return;
			}
			if (mode == eBlendMode.Subtract)
			{
				outR = (byte)SubtractChannel(baseR, blendR);
				outG = (byte)SubtractChannel(baseG, blendG);
				outB = (byte)SubtractChannel(baseB, blendB);
				return;
			}
			if (mode == eBlendMode.Divide)
			{
				outR = (byte)DivideChannel(baseR, blendR);
				outG = (byte)DivideChannel(baseG, blendG);
				outB = (byte)DivideChannel(baseB, blendB);
				return;
			}
			if (mode == eBlendMode.Dissolve)
			{
				outR = baseR;
				outG = baseG;
				outB = baseB;
				return;
			}
			outR = blendR;
			outG = blendG;
			outB = blendB;
		}
	}
}
