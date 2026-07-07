using System;
using System.Collections.Generic;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public enum eRulerUnits
	{
		Pixels,
		Millimeters,
		Centimeters,
		Percent
	}

	public class Document
	{
		private static int s_maxUndoDepth = 100;

		public static int MaxUndoDepth()
		{
			return s_maxUndoDepth;
		}

		public static void SetMaxUndoDepth(int depth)
		{
			if (depth < 10)
			{
				depth = 10;
			}
			if (depth > 500)
			{
				depth = 500;
			}
			s_maxUndoDepth = depth;
		}

		private int m_width;
		private int m_height;
		private string m_title;
		private eColorDepth m_colorDepth;
		private List<Layer> m_layers;
		private int m_activeLayerIndex;
		private List<int> m_selectedLayerIndices;
		private List<EditCommand> m_undoStack;
		private List<EditCommand> m_redoStack;
		private bool m_lastHistoryWasUndo;
		private SKBitmap m_strokeSnapshot;
		private bool m_strokeSnapshotValid;
		private int m_strokeLayerIndex;
		private ePaintTarget m_paintTarget;
		private ePaintTarget m_strokePaintTarget;
		private Selection m_selection;
		private SKBitmap m_composite;
		private int m_compositeVersion;
		private SKRectI m_composeDirtyRect;
		private bool m_composeDirtyAny;
		private bool m_composeDirtyAll;
		private SKRectI m_strokeDirtyRect;
		private bool m_strokeDirtyValid;
		private bool m_dirty;
		private eRulerUnits m_rulerUnits;
		private string m_sourcePath;
		private Guides m_guides;
		private DocumentStateCommand m_pendingDocEdit;
		private bool m_floatActive;
		private SKBitmap m_floatBitmap;
		private int m_floatDeltaX;
		private int m_floatDeltaY;
		private int m_floatLayerIndex;
		private SKBitmap m_floatOriginalBitmap;
		private int m_floatOriginalOffsetX;
		private int m_floatOriginalOffsetY;
		private byte[] m_floatSourceMask;
		private SKRectI m_floatSourceMaskRect;
		private SKRectI m_floatSourceBounds;

		private sealed unsafe class CompositeBandWorker
		{
			public IntPtr[] m_sourceBases;
			public int[] m_sourceRowBytes;
			public int[] m_sourceWidths;
			public int[] m_sourceHeights;
			public int[] m_sourceOffsetsX;
			public int[] m_sourceOffsetsY;
			public int[] m_sourceOpacities;
			public IntPtr[] m_sourceMaskBases;
			public int[] m_sourceMaskRowBytes;
			public int m_visibleCount;
			public IntPtr m_targetBase;
			public int m_targetRowBytes;
			public int m_left;
			public int m_right;

			public void Band(int start, int end)
			{
				byte* targetBase = (byte*)m_targetBase.ToPointer();
				for (int canvasY = start; canvasY < end; canvasY++)
				{
					byte* targetRow = targetBase + (canvasY * m_targetRowBytes);
					for (int canvasX = m_left; canvasX < m_right; canvasX++)
					{
						int accumulatedRed = 0;
						int accumulatedGreen = 0;
						int accumulatedBlue = 0;
						int accumulatedAlpha = 0;
						for (int layerIndex = 0; layerIndex < m_visibleCount; layerIndex++)
						{
							int bitmapX = canvasX - m_sourceOffsetsX[layerIndex];
							int bitmapY = canvasY - m_sourceOffsetsY[layerIndex];
							if (bitmapX < 0 || bitmapY < 0 || bitmapX >= m_sourceWidths[layerIndex] || bitmapY >= m_sourceHeights[layerIndex])
							{
								continue;
							}
							byte* sourcePixel = (byte*)m_sourceBases[layerIndex].ToPointer() + (bitmapY * m_sourceRowBytes[layerIndex]) + (bitmapX * 4);
							int sourceAlpha = sourcePixel[3];
							if (sourceAlpha == 0)
							{
								continue;
							}
							if (m_sourceMaskBases[layerIndex] != IntPtr.Zero)
							{
								byte* maskPixel = (byte*)m_sourceMaskBases[layerIndex].ToPointer() + (bitmapY * m_sourceMaskRowBytes[layerIndex]) + (bitmapX * 4);
								int maskCoverage = maskPixel[0];
								sourceAlpha = ((sourceAlpha * maskCoverage) + 127) / 255;
								if (sourceAlpha == 0)
								{
									continue;
								}
							}
							int effectiveAlpha = ((sourceAlpha * m_sourceOpacities[layerIndex]) + 127) / 255;
							if (effectiveAlpha == 0)
							{
								continue;
							}
							int premultipliedRed = ((sourcePixel[0] * effectiveAlpha) + 127) / 255;
							int premultipliedGreen = ((sourcePixel[1] * effectiveAlpha) + 127) / 255;
							int premultipliedBlue = ((sourcePixel[2] * effectiveAlpha) + 127) / 255;
							int inverseAlpha = 255 - effectiveAlpha;
							accumulatedRed = premultipliedRed + (((accumulatedRed * inverseAlpha) + 127) / 255);
							accumulatedGreen = premultipliedGreen + (((accumulatedGreen * inverseAlpha) + 127) / 255);
							accumulatedBlue = premultipliedBlue + (((accumulatedBlue * inverseAlpha) + 127) / 255);
							accumulatedAlpha = effectiveAlpha + (((accumulatedAlpha * inverseAlpha) + 127) / 255);
						}
						byte* targetPixel = targetRow + (canvasX * 4);
						targetPixel[0] = (byte)accumulatedRed;
						targetPixel[1] = (byte)accumulatedGreen;
						targetPixel[2] = (byte)accumulatedBlue;
						targetPixel[3] = (byte)accumulatedAlpha;
					}
				}
			}
		}

		private sealed unsafe class CustomBlendBandWorker
		{
			public IntPtr m_sourceBase;
			public int m_sourceRowBytes;
			public int m_sourceWidth;
			public int m_sourceHeight;
			public int m_offsetX;
			public int m_offsetY;
			public int m_opacity;
			public eBlendMode m_mode;
			public IntPtr m_maskBase;
			public int m_maskRowBytes;
			public IntPtr m_targetBase;
			public int m_targetRowBytes;
			public int m_left;
			public int m_right;

			public void Band(int start, int end)
			{
				byte* sourceBase = (byte*)m_sourceBase.ToPointer();
				byte* targetBase = (byte*)m_targetBase.ToPointer();
				for (int canvasY = start; canvasY < end; canvasY++)
				{
					int bitmapY = canvasY - m_offsetY;
					if (bitmapY < 0 || bitmapY >= m_sourceHeight)
					{
						continue;
					}
					byte* sourceRow = sourceBase + (bitmapY * m_sourceRowBytes);
					byte* targetRow = targetBase + (canvasY * m_targetRowBytes);
					for (int canvasX = m_left; canvasX < m_right; canvasX++)
					{
						int bitmapX = canvasX - m_offsetX;
						if (bitmapX < 0 || bitmapX >= m_sourceWidth)
						{
							continue;
						}
						byte* sourcePixel = sourceRow + (bitmapX * 4);
						int sourceAlpha = sourcePixel[3];
						if (sourceAlpha == 0)
						{
							continue;
						}
						if (m_maskBase != IntPtr.Zero)
						{
							byte* maskPixel = (byte*)m_maskBase.ToPointer() + (bitmapY * m_maskRowBytes) + (bitmapX * 4);
							int maskCoverage = maskPixel[0];
							sourceAlpha = ((sourceAlpha * maskCoverage) + 127) / 255;
							if (sourceAlpha == 0)
							{
								continue;
							}
						}
						int effectiveAlpha = ((sourceAlpha * m_opacity) + 127) / 255;
						if (effectiveAlpha == 0)
						{
							continue;
						}
						byte* targetPixel = targetRow + (canvasX * 4);
						if (m_mode == eBlendMode.Dissolve)
						{
							int threshold = ((canvasX * 73) + (canvasY * 151)) & 255;
							if (effectiveAlpha > threshold)
							{
								targetPixel[0] = sourcePixel[0];
								targetPixel[1] = sourcePixel[1];
								targetPixel[2] = sourcePixel[2];
								targetPixel[3] = 255;
							}
							continue;
						}
						int baseAlpha = targetPixel[3];
						byte blendedRed;
						byte blendedGreen;
						byte blendedBlue;
						if (baseAlpha == 0)
						{
							blendedRed = sourcePixel[0];
							blendedGreen = sourcePixel[1];
							blendedBlue = sourcePixel[2];
						}
						else
						{
							int baseRed = ((targetPixel[0] * 255) + (baseAlpha / 2)) / baseAlpha;
							int baseGreen = ((targetPixel[1] * 255) + (baseAlpha / 2)) / baseAlpha;
							int baseBlue = ((targetPixel[2] * 255) + (baseAlpha / 2)) / baseAlpha;
							if (baseRed > 255)
							{
								baseRed = 255;
							}
							if (baseGreen > 255)
							{
								baseGreen = 255;
							}
							if (baseBlue > 255)
							{
								baseBlue = 255;
							}
							BlendModes.Blend(m_mode, (byte)baseRed, (byte)baseGreen, (byte)baseBlue, sourcePixel[0], sourcePixel[1], sourcePixel[2], out blendedRed, out blendedGreen, out blendedBlue);
						}
						int inverseAlpha = 255 - effectiveAlpha;
						int resultRed = (((blendedRed * effectiveAlpha) + 127) / 255) + (((targetPixel[0] * inverseAlpha) + 127) / 255);
						int resultGreen = (((blendedGreen * effectiveAlpha) + 127) / 255) + (((targetPixel[1] * inverseAlpha) + 127) / 255);
						int resultBlue = (((blendedBlue * effectiveAlpha) + 127) / 255) + (((targetPixel[2] * inverseAlpha) + 127) / 255);
						int resultAlpha = effectiveAlpha + (((baseAlpha * inverseAlpha) + 127) / 255);
						if (resultRed > 255)
						{
							resultRed = 255;
						}
						if (resultGreen > 255)
						{
							resultGreen = 255;
						}
						if (resultBlue > 255)
						{
							resultBlue = 255;
						}
						if (resultAlpha > 255)
						{
							resultAlpha = 255;
						}
						targetPixel[0] = (byte)resultRed;
						targetPixel[1] = (byte)resultGreen;
						targetPixel[2] = (byte)resultBlue;
						targetPixel[3] = (byte)resultAlpha;
					}
				}
			}
		}

		public static Document OpenImage(string title, SKBitmap source)
		{
			Document document = new Document(title, source.Width, source.Height);
			document.ActiveLayer().SetPixelsFrom(source);
			return document;
		}

		public Document(string title, int width, int height)
		{
			m_title = title;
			m_width = width;
			m_height = height;
			m_colorDepth = eColorDepth.Eight;
			m_layers = new List<Layer>();
			Layer background = new Layer("Background", width, height, m_colorDepth);
			background.FillWhite();
			background.SetIsBackground(true);
			m_layers.Add(background);
			m_activeLayerIndex = 0;
			m_selectedLayerIndices = new List<int>();
			m_selectedLayerIndices.Add(0);
			m_undoStack = new List<EditCommand>();
			m_redoStack = new List<EditCommand>();
			m_strokeSnapshot = null;
			m_strokeSnapshotValid = false;
			m_strokeLayerIndex = 0;
			m_selection = new Selection(width, height);
			m_composeDirtyRect = SKRectI.Empty;
			m_composeDirtyAny = false;
			m_composeDirtyAll = false;
			m_strokeDirtyRect = SKRectI.Empty;
			m_strokeDirtyValid = false;
			m_dirty = false;
			m_sourcePath = null;
			m_guides = new Guides();
			m_pendingDocEdit = null;
			m_floatActive = false;
			m_floatBitmap = null;
			m_floatDeltaX = 0;
			m_floatDeltaY = 0;
			m_floatLayerIndex = 0;
			m_floatOriginalBitmap = null;
			m_floatOriginalOffsetX = 0;
			m_floatOriginalOffsetY = 0;
			m_floatSourceMask = null;
			m_floatSourceMaskRect = SKRectI.Empty;
			m_floatSourceBounds = SKRectI.Empty;
		}

		private unsafe SKBitmap ExtractSelected(Layer layer, Selection selection)
		{
			SKBitmap source = layer.Bitmap();
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int width = source.Width;
			int height = source.Height;
			SKBitmap moving = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			moving.Erase(SKColors.Transparent);
			SKRectI bounds = selection.Bounds();
			byte[] mask = selection.Mask();
			int maskOriginX = selection.MaskOriginX();
			int maskOriginY = selection.MaskOriginY();
			int maskStride = selection.MaskWidth();
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			int sourceRowBytes = source.RowBytes;
			byte* movingBase = (byte*)moving.GetPixels().ToPointer();
			int movingRowBytes = moving.RowBytes;
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				int maskRow = ((y - maskOriginY) * maskStride) - maskOriginX;
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					int coverage = mask[maskRow + x];
					if (coverage == 0)
					{
						continue;
					}
					int bitmapX = x - offsetX;
					int bitmapY = y - offsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= width || bitmapY >= height)
					{
						continue;
					}
					byte* sourcePixel = sourceBase + (bitmapY * sourceRowBytes) + (bitmapX * 4);
					byte* movingPixel = movingBase + (bitmapY * movingRowBytes) + (bitmapX * 4);
					movingPixel[0] = sourcePixel[0];
					movingPixel[1] = sourcePixel[1];
					movingPixel[2] = sourcePixel[2];
					if (coverage == 255)
					{
						movingPixel[3] = sourcePixel[3];
					}
					else
					{
						movingPixel[3] = (byte)(((sourcePixel[3] * coverage) + 127) / 255);
					}
				}
			}
			return moving;
		}

		private unsafe SKBitmap CloneWithSelectionCleared(Layer layer, Selection selection)
		{
			SKBitmap remainder = layer.Bitmap().Copy();
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int width = remainder.Width;
			int height = remainder.Height;
			SKRectI bounds = selection.Bounds();
			byte[] mask = selection.Mask();
			int maskOriginX = selection.MaskOriginX();
			int maskOriginY = selection.MaskOriginY();
			int maskStride = selection.MaskWidth();
			byte* remainderBase = (byte*)remainder.GetPixels().ToPointer();
			int remainderRowBytes = remainder.RowBytes;
			for (int y = bounds.Top; y < bounds.Bottom; y++)
			{
				int maskRow = ((y - maskOriginY) * maskStride) - maskOriginX;
				for (int x = bounds.Left; x < bounds.Right; x++)
				{
					int coverage = mask[maskRow + x];
					if (coverage == 0)
					{
						continue;
					}
					int bitmapX = x - offsetX;
					int bitmapY = y - offsetY;
					if (bitmapX < 0 || bitmapY < 0 || bitmapX >= width || bitmapY >= height)
					{
						continue;
					}
					byte* remainderPixel = remainderBase + (bitmapY * remainderRowBytes) + (bitmapX * 4);
					if (coverage == 255)
					{
						remainderPixel[3] = 0;
					}
					else
					{
						remainderPixel[3] = (byte)(((remainderPixel[3] * (255 - coverage)) + 127) / 255);
					}
				}
			}
			return remainder;
		}

		private unsafe SKBitmap ComposeFloatOntoLayer(Layer layer)
		{
			SKBitmap holed = layer.Bitmap();
			int width = holed.Width;
			int height = holed.Height;
			SKBitmap result = holed.Copy();
			byte* floatBase = (byte*)m_floatBitmap.GetPixels().ToPointer();
			int floatRowBytes = m_floatBitmap.RowBytes;
			int floatWidth = m_floatBitmap.Width;
			int floatHeight = m_floatBitmap.Height;
			byte* resultBase = (byte*)result.GetPixels().ToPointer();
			int resultRowBytes = result.RowBytes;
			for (int floatY = 0; floatY < floatHeight; floatY++)
			{
				int destinationY = floatY + m_floatDeltaY;
				if (destinationY < 0 || destinationY >= height)
				{
					continue;
				}
				byte* floatRow = floatBase + (floatY * floatRowBytes);
				byte* resultRow = resultBase + (destinationY * resultRowBytes);
				for (int floatX = 0; floatX < floatWidth; floatX++)
				{
					byte* floatPixel = floatRow + (floatX * 4);
					int floatAlpha = floatPixel[3];
					if (floatAlpha == 0)
					{
						continue;
					}
					int destinationX = floatX + m_floatDeltaX;
					if (destinationX < 0 || destinationX >= width)
					{
						continue;
					}
					byte* resultPixel = resultRow + (destinationX * 4);
					if (floatAlpha == 255)
					{
						resultPixel[0] = floatPixel[0];
						resultPixel[1] = floatPixel[1];
						resultPixel[2] = floatPixel[2];
						resultPixel[3] = 255;
						continue;
					}
					int inverseAlpha = 255 - floatAlpha;
					int baseAlpha = resultPixel[3];
					int outAlpha = floatAlpha + (((baseAlpha * inverseAlpha) + 127) / 255);
					if (outAlpha <= 0)
					{
						resultPixel[0] = 0;
						resultPixel[1] = 0;
						resultPixel[2] = 0;
						resultPixel[3] = 0;
						continue;
					}
					int baseRed = resultPixel[0];
					int baseGreen = resultPixel[1];
					int baseBlue = resultPixel[2];
					int outRed = (((floatPixel[0] * floatAlpha) + (((baseRed * baseAlpha) * inverseAlpha) / 255)) + (outAlpha / 2)) / outAlpha;
					int outGreen = (((floatPixel[1] * floatAlpha) + (((baseGreen * baseAlpha) * inverseAlpha) / 255)) + (outAlpha / 2)) / outAlpha;
					int outBlue = (((floatPixel[2] * floatAlpha) + (((baseBlue * baseAlpha) * inverseAlpha) / 255)) + (outAlpha / 2)) / outAlpha;
					if (outRed > 255)
					{
						outRed = 255;
					}
					if (outGreen > 255)
					{
						outGreen = 255;
					}
					if (outBlue > 255)
					{
						outBlue = 255;
					}
					resultPixel[0] = (byte)outRed;
					resultPixel[1] = (byte)outGreen;
					resultPixel[2] = (byte)outBlue;
					resultPixel[3] = (byte)outAlpha;
				}
			}
			return result;
		}

		public bool HasFloatingSelection()
		{
			return m_floatActive;
		}

		public SKBitmap FloatBitmap()
		{
			return m_floatBitmap;
		}

		public int FloatDeltaX()
		{
			return m_floatDeltaX;
		}

		public int FloatDeltaY()
		{
			return m_floatDeltaY;
		}

		public int FloatLayerIndex()
		{
			return m_floatLayerIndex;
		}

		public void LiftFloatingSelection()
		{
			if (m_floatActive)
			{
				return;
			}
			Selection selection = m_selection;
			if (!selection.IsActive())
			{
				return;
			}
			Layer layer = ActiveLayer();
			if (layer == null)
			{
				return;
			}
			int index = m_activeLayerIndex;
			m_floatOriginalOffsetX = layer.OffsetX();
			m_floatOriginalOffsetY = layer.OffsetY();
			SKBitmap preExpand = layer.Bitmap();
			layer.ExpandToCover(m_width, m_height);
			if (ReferenceEquals(layer.Bitmap(), preExpand))
			{
				m_floatOriginalBitmap = preExpand.Copy();
			}
			else
			{
				m_floatOriginalBitmap = preExpand;
			}
			m_floatBitmap = ExtractSelected(layer, selection);
			SKBitmap holed = CloneWithSelectionCleared(layer, selection);
			SKBitmap previous = layer.Bitmap();
			layer.SetBitmap(holed);
			previous.Dispose();
			m_floatSourceMask = selection.MaskCopy();
			m_floatSourceMaskRect = new SKRectI(selection.MaskOriginX(), selection.MaskOriginY(), selection.MaskOriginX() + selection.MaskWidth(), selection.MaskOriginY() + selection.MaskHeight());
			m_floatSourceBounds = selection.Bounds();
			m_floatDeltaX = 0;
			m_floatDeltaY = 0;
			m_floatLayerIndex = index;
			m_floatActive = true;
			MarkComposeDirtyAll();
		}

		public void SetFloatingSelectionDelta(int totalDeltaX, int totalDeltaY)
		{
			if (!m_floatActive)
			{
				return;
			}
			m_floatDeltaX = totalDeltaX;
			m_floatDeltaY = totalDeltaY;
			m_selection.SetShifted(m_floatSourceMask, m_floatSourceMaskRect, m_floatSourceBounds, m_floatDeltaX, m_floatDeltaY);
		}

		public void CommitFloatingSelection()
		{
			if (!m_floatActive)
			{
				return;
			}
			if (m_floatLayerIndex < 0 || m_floatLayerIndex >= m_layers.Count)
			{
				m_floatBitmap.Dispose();
				m_floatBitmap = null;
				m_floatOriginalBitmap = null;
				m_floatSourceMask = null;
				m_floatSourceMaskRect = SKRectI.Empty;
				m_floatSourceBounds = SKRectI.Empty;
				m_floatActive = false;
				MarkComposeDirtyAll();
				return;
			}
			Layer layer = m_layers[m_floatLayerIndex];
			SKBitmap holed = layer.Bitmap();
			SKBitmap committed = ComposeFloatOntoLayer(layer);
			PushCommand(new MoveLayerCommand(m_floatLayerIndex, m_floatOriginalBitmap, m_floatOriginalOffsetX, m_floatOriginalOffsetY, committed, layer.OffsetX(), layer.OffsetY()));
			layer.SetBitmap(committed);
			holed.Dispose();
			m_floatBitmap.Dispose();
			m_floatBitmap = null;
			m_floatOriginalBitmap = null;
			m_floatSourceMask = null;
			m_floatSourceMaskRect = SKRectI.Empty;
			m_floatSourceBounds = SKRectI.Empty;
			m_floatActive = false;
			MarkComposeDirtyAll();
		}

		public void CancelFloatingSelection()
		{
			if (!m_floatActive)
			{
				return;
			}
			if (m_floatLayerIndex >= 0 && m_floatLayerIndex < m_layers.Count)
			{
				Layer layer = m_layers[m_floatLayerIndex];
				SKBitmap holed = layer.Bitmap();
				layer.SetBitmap(m_floatOriginalBitmap);
				layer.SetOffset(m_floatOriginalOffsetX, m_floatOriginalOffsetY);
				holed.Dispose();
			}
			else
			{
				m_floatOriginalBitmap.Dispose();
			}
			m_selection.SelectMaskPlaced(m_floatSourceMask, m_floatSourceMaskRect, m_floatSourceBounds);
			m_floatBitmap.Dispose();
			m_floatBitmap = null;
			m_floatOriginalBitmap = null;
			m_floatSourceMask = null;
			m_floatSourceMaskRect = SKRectI.Empty;
			m_floatSourceBounds = SKRectI.Empty;
			m_floatActive = false;
			MarkComposeDirtyAll();
		}

		public string SourcePath()
		{
			return m_sourcePath;
		}

		public void SetSourcePath(string path)
		{
			m_sourcePath = path;
		}

		public bool FlatCompatible()
		{
			if (m_layers.Count != 1)
			{
				return false;
			}
			if (m_layers[0].IsText())
			{
				return false;
			}
			return true;
		}

		public bool IsDirty()
		{
			return m_dirty;
		}

		public void MarkClean()
		{
			m_dirty = false;
		}

		private static SKRectI UnionRects(SKRectI first, SKRectI second)
		{
			int left = first.Left;
			if (second.Left < left)
			{
				left = second.Left;
			}
			int top = first.Top;
			if (second.Top < top)
			{
				top = second.Top;
			}
			int right = first.Right;
			if (second.Right > right)
			{
				right = second.Right;
			}
			int bottom = first.Bottom;
			if (second.Bottom > bottom)
			{
				bottom = second.Bottom;
			}
			return new SKRectI(left, top, right, bottom);
		}

		public void MarkComposeDirtyAll()
		{
			m_composeDirtyAny = true;
			m_composeDirtyAll = true;
		}

		public void MarkComposeDirtyRegion(SKRectI canvasRect)
		{
			if (canvasRect.Width <= 0 || canvasRect.Height <= 0)
			{
				return;
			}
			if (!m_composeDirtyAny || m_composeDirtyRect.Width <= 0)
			{
				m_composeDirtyRect = canvasRect;
			}
			else
			{
				m_composeDirtyRect = UnionRects(m_composeDirtyRect, canvasRect);
			}
			m_composeDirtyAny = true;
			if (!m_strokeDirtyValid)
			{
				m_strokeDirtyRect = canvasRect;
				m_strokeDirtyValid = true;
			}
			else
			{
				m_strokeDirtyRect = UnionRects(m_strokeDirtyRect, canvasRect);
			}
		}

		public bool ComposeDirtyAny()
		{
			return m_composeDirtyAny;
		}

		public bool ComposeDirtyAll()
		{
			return m_composeDirtyAll;
		}

		public SKRectI ComposeDirtyRect()
		{
			return m_composeDirtyRect;
		}

		public void ClearComposeDirty()
		{
			m_composeDirtyRect = SKRectI.Empty;
			m_composeDirtyAny = false;
			m_composeDirtyAll = false;
		}

		public Selection Selection()
		{
			return m_selection;
		}

		public SKBitmap StrokeSnapshot()
		{
			if (!m_strokeSnapshotValid)
			{
				return null;
			}
			return m_strokeSnapshot;
		}

		public void ResetSelection()
		{
			if (m_floatActive)
			{
				CommitFloatingSelection();
			}
			m_selection = new Selection(m_width, m_height);
		}

		public unsafe void FillSelection(SKColor fill)
		{
			Layer layer = ActiveLayer();
			if (layer == null)
			{
				return;
			}
			if (!m_selection.IsActive())
			{
				return;
			}
			SKBitmap bitmap = layer.Bitmap();
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			SKRectI bounds = m_selection.Bounds();
			int left = bounds.Left;
			int top = bounds.Top;
			int right = bounds.Right;
			int bottom = bounds.Bottom;
			if (left < offsetX)
			{
				left = offsetX;
			}
			if (top < offsetY)
			{
				top = offsetY;
			}
			if (right > offsetX + bitmap.Width)
			{
				right = offsetX + bitmap.Width;
			}
			if (bottom > offsetY + bitmap.Height)
			{
				bottom = offsetY + bitmap.Height;
			}
			if (right <= left || bottom <= top)
			{
				return;
			}
			byte[] mask = m_selection.Mask();
			int maskOriginX = m_selection.MaskOriginX();
			int maskOriginY = m_selection.MaskOriginY();
			int maskStride = m_selection.MaskWidth();
			byte* basePointer = (byte*)bitmap.GetPixels().ToPointer();
			int rowBytes = bitmap.RowBytes;
			byte fillRed = fill.Red;
			byte fillGreen = fill.Green;
			byte fillBlue = fill.Blue;
			byte fillAlpha = fill.Alpha;
			for (int canvasY = top; canvasY < bottom; canvasY++)
			{
				int maskRow = ((canvasY - maskOriginY) * maskStride) - maskOriginX;
				byte* row = basePointer + ((canvasY - offsetY) * rowBytes);
				for (int canvasX = left; canvasX < right; canvasX++)
				{
					int coverage = mask[maskRow + canvasX];
					if (coverage == 0)
					{
						continue;
					}
					byte* pixel = row + ((canvasX - offsetX) * 4);
					if (coverage == 255)
					{
						pixel[0] = fillRed;
						pixel[1] = fillGreen;
						pixel[2] = fillBlue;
						pixel[3] = fillAlpha;
						continue;
					}
					int inverse = 255 - coverage;
					pixel[0] = (byte)(((pixel[0] * inverse) + (fillRed * coverage) + 127) / 255);
					pixel[1] = (byte)(((pixel[1] * inverse) + (fillGreen * coverage) + 127) / 255);
					pixel[2] = (byte)(((pixel[2] * inverse) + (fillBlue * coverage) + 127) / 255);
					pixel[3] = (byte)(((pixel[3] * inverse) + (fillAlpha * coverage) + 127) / 255);
				}
			}
			MarkComposeDirtyRegion(new SKRectI(left, top, right, bottom));
		}

		public void FillLayer(SKColor color)
		{
			Layer layer = ActiveLayer();
			if (layer == null)
			{
				return;
			}
			layer.Bitmap().Erase(color);
			MarkComposeDirtyAll();
		}

		public ePaintTarget PaintTarget()
		{
			return m_paintTarget;
		}

		public void SetPaintTarget(ePaintTarget target)
		{
			m_paintTarget = target;
		}

		public SKBitmap ActivePaintBitmap()
		{
			Layer active = ActiveLayer();
			if (active == null)
			{
				return null;
			}
			if (m_paintTarget == ePaintTarget.Mask && active.HasMask())
			{
				return active.MaskBitmap();
			}
			return active.Bitmap();
		}

		private SKBitmap StrokeTargetBitmap()
		{
			if (m_strokeLayerIndex < 0 || m_strokeLayerIndex >= m_layers.Count)
			{
				return null;
			}
			Layer layer = m_layers[m_strokeLayerIndex];
			if (m_strokePaintTarget == ePaintTarget.Mask && layer.HasMask())
			{
				return layer.MaskBitmap();
			}
			return layer.Bitmap();
		}

		public void BeginStroke()
		{
			m_strokeSnapshotValid = false;
			Layer active = ActiveLayer();
			if (active == null)
			{
				return;
			}
			SKBitmap bitmap = ActivePaintBitmap();
			if (m_strokeSnapshot == null || m_strokeSnapshot.Width != bitmap.Width || m_strokeSnapshot.Height != bitmap.Height)
			{
				if (m_strokeSnapshot != null)
				{
					m_strokeSnapshot.Dispose();
				}
				m_strokeSnapshot = new SKBitmap(bitmap.Width, bitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			}
			PixelRegion.CopyPixels(bitmap, m_strokeSnapshot);
			m_strokeLayerIndex = m_activeLayerIndex;
			m_strokePaintTarget = m_paintTarget;
			if (m_paintTarget == ePaintTarget.Mask && active.HasMask())
			{
				active.SetPaintRedirect(active.MaskBitmap());
			}
			else
			{
				active.SetPaintRedirect(null);
			}
			m_strokeSnapshotValid = true;
			m_strokeDirtyRect = SKRectI.Empty;
			m_strokeDirtyValid = false;
		}

		public unsafe void RestoreStrokeSnapshot()
		{
			if (!m_strokeSnapshotValid)
			{
				return;
			}
			if (m_strokeLayerIndex < 0 || m_strokeLayerIndex >= m_layers.Count)
			{
				return;
			}
			SKBitmap current = StrokeTargetBitmap();
			if (current == null)
			{
				return;
			}
			if (current.Width != m_strokeSnapshot.Width || current.Height != m_strokeSnapshot.Height)
			{
				return;
			}
			byte* sourceBase = (byte*)m_strokeSnapshot.GetPixels().ToPointer();
			int sourceRowBytes = m_strokeSnapshot.RowBytes;
			byte* targetBase = (byte*)current.GetPixels().ToPointer();
			int targetRowBytes = current.RowBytes;
			long rowLength = (long)current.Width * 4;
			int height = current.Height;
			for (int y = 0; y < height; y++)
			{
				byte* sourceRow = sourceBase + ((long)y * sourceRowBytes);
				byte* targetRow = targetBase + ((long)y * targetRowBytes);
				Buffer.MemoryCopy(sourceRow, targetRow, rowLength, rowLength);
			}
		}

		public void EndStroke()
		{
			if (!m_strokeSnapshotValid)
			{
				return;
			}
			m_strokeSnapshotValid = false;
			bool strokeMask = m_strokePaintTarget == ePaintTarget.Mask;
			if (m_strokeLayerIndex >= 0 && m_strokeLayerIndex < m_layers.Count)
			{
				m_layers[m_strokeLayerIndex].SetPaintRedirect(null);
			}
			if (!strokeMask && m_strokeLayerIndex >= 0 && m_strokeLayerIndex < m_layers.Count)
			{
				Layer strokeStyledLayer = m_layers[m_strokeLayerIndex];
				if (strokeStyledLayer.LayerStyle().HasAnyEffect())
				{
					strokeStyledLayer.InvalidateStyleCache();
					MarkComposeDirtyAll();
				}
			}
			if (m_strokeLayerIndex < 0 || m_strokeLayerIndex >= m_layers.Count)
			{
				return;
			}
			SKBitmap current = StrokeTargetBitmap();
			if (current == null)
			{
				return;
			}
			if (current.Width != m_strokeSnapshot.Width || current.Height != m_strokeSnapshot.Height)
			{
				return;
			}
			SKRectI searchRect = new SKRectI(0, 0, current.Width, current.Height);
			if (m_strokeDirtyValid)
			{
				Layer strokeLayer = m_layers[m_strokeLayerIndex];
				int bitmapLeft = m_strokeDirtyRect.Left - strokeLayer.OffsetX();
				int bitmapTop = m_strokeDirtyRect.Top - strokeLayer.OffsetY();
				int bitmapRight = m_strokeDirtyRect.Right - strokeLayer.OffsetX();
				int bitmapBottom = m_strokeDirtyRect.Bottom - strokeLayer.OffsetY();
				if (bitmapLeft < 0)
				{
					bitmapLeft = 0;
				}
				if (bitmapTop < 0)
				{
					bitmapTop = 0;
				}
				if (bitmapRight > current.Width)
				{
					bitmapRight = current.Width;
				}
				if (bitmapBottom > current.Height)
				{
					bitmapBottom = current.Height;
				}
				if (bitmapRight <= bitmapLeft || bitmapBottom <= bitmapTop)
				{
					return;
				}
				searchRect = new SKRectI(bitmapLeft, bitmapTop, bitmapRight, bitmapBottom);
			}
			SKRectI rect = PixelRegion.ComputeDirtyRect(m_strokeSnapshot, current, searchRect);
			if (rect.Width <= 0 || rect.Height <= 0)
			{
				return;
			}
			SKBitmap before = PixelRegion.ExtractRegion(m_strokeSnapshot, rect);
			SKBitmap after = PixelRegion.ExtractRegion(current, rect);
			LayerEditCommand command = new LayerEditCommand(m_strokeLayerIndex, rect, before, after, strokeMask);
			PushCommand(command);
		}

		public void PushCommand(EditCommand command)
		{
			m_undoStack.Add(command);
			m_redoStack.Clear();
			if (m_undoStack.Count > s_maxUndoDepth)
			{
				m_undoStack.RemoveAt(0);
			}
			m_dirty = true;
			m_lastHistoryWasUndo = false;
		}

		public Guides Guides()
		{
			return m_guides;
		}

		public List<Layer> CloneLayers()
		{
			List<Layer> copy = new List<Layer>();
			for (int index = 0; index < m_layers.Count; index++)
			{
				copy.Add(m_layers[index].Clone());
			}
			return copy;
		}

		public void ReplaceLayers(List<Layer> layers, int width, int height, int activeIndex)
		{
			m_layers = new List<Layer>();
			for (int index = 0; index < layers.Count; index++)
			{
				m_layers.Add(layers[index].Clone());
			}
			m_width = width;
			m_height = height;
			if (activeIndex < 0)
			{
				activeIndex = 0;
			}
			if (activeIndex >= m_layers.Count)
			{
				activeIndex = m_layers.Count - 1;
			}
			m_activeLayerIndex = activeIndex;
			MarkComposeDirtyAll();
		}

		public void RestoreSelection(byte[] mask, SKRectI maskRect, SKRectI bounds, bool active)
		{
			m_selection = new Selection(m_width, m_height);
			if (active && mask != null && mask.Length == maskRect.Width * maskRect.Height)
			{
				m_selection.SelectMaskPlaced(mask, maskRect, bounds);
			}
		}

		public void BeginCanvasEdit(string label)
		{
			if (m_floatActive)
			{
				CommitFloatingSelection();
			}
			if (m_pendingDocEdit != null)
			{
				return;
			}
			m_pendingDocEdit = new DocumentStateCommand(label);
			m_pendingDocEdit.CaptureBefore(this);
		}

		public void EndCanvasEdit()
		{
			if (m_pendingDocEdit == null)
			{
				return;
			}
			m_pendingDocEdit.CaptureAfter(this);
			PushCommand(m_pendingDocEdit);
			m_pendingDocEdit = null;
		}

		public bool Undo()
		{
			if (m_floatActive)
			{
				CancelFloatingSelection();
				m_lastHistoryWasUndo = true;
				return true;
			}
			if (m_undoStack.Count == 0)
			{
				return false;
			}
			int last = m_undoStack.Count - 1;
			EditCommand command = m_undoStack[last];
			m_undoStack.RemoveAt(last);
			command.ApplyBefore(this);
			m_redoStack.Add(command);
			m_dirty = true;
			m_lastHistoryWasUndo = true;
			return true;
		}

		public bool Redo()
		{
			if (m_redoStack.Count == 0)
			{
				return false;
			}
			int last = m_redoStack.Count - 1;
			EditCommand command = m_redoStack[last];
			m_redoStack.RemoveAt(last);
			command.ApplyAfter(this);
			m_undoStack.Add(command);
			m_dirty = true;
			m_lastHistoryWasUndo = false;
			return true;
		}

		// Ctrl+Z toggles the most recent change: undo, but redo if the last
		// history action was itself an undo (a fresh edit resets the direction).
		public bool UndoToggle()
		{
			if (m_lastHistoryWasUndo && m_redoStack.Count > 0)
			{
				return Redo();
			}
			return Undo();
		}

		public List<string> HistoryLabels()
		{
			List<string> labels = new List<string>();
			for (int index = 0; index < m_undoStack.Count; index++)
			{
				labels.Add(m_undoStack[index].Label());
			}
			for (int index = m_redoStack.Count - 1; index >= 0; index--)
			{
				labels.Add(m_redoStack[index].Label());
			}
			return labels;
		}

		public int HistoryIndex()
		{
			return m_undoStack.Count;
		}

		public void JumpToHistory(int appliedCount)
		{
			int current = m_undoStack.Count;
			if (appliedCount < current)
			{
				int steps = current - appliedCount;
				for (int index = 0; index < steps; index++)
				{
					Undo();
				}
				return;
			}
			if (appliedCount > current)
			{
				int steps = appliedCount - current;
				for (int index = 0; index < steps; index++)
				{
					Redo();
				}
			}
		}

		public int Width()
		{
			return m_width;
		}

		public int Height()
		{
			return m_height;
		}

		public eColorDepth ColorDepth()
		{
			return m_colorDepth;
		}

		public void ConvertColorDepth(eColorDepth target)
		{
			if (target == m_colorDepth)
			{
				return;
			}
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				layer.ConvertDepth(target);
			}
			m_colorDepth = target;
			if (m_composite != null)
			{
				m_composite.Dispose();
				m_composite = null;
			}
			MarkComposeDirtyAll();
		}

		public eRulerUnits RulerUnits()
		{
			return m_rulerUnits;
		}

		public void SetRulerUnits(eRulerUnits units)
		{
			m_rulerUnits = units;
			m_dirty = true;
		}

		public string Title()
		{
			return m_title;
		}

		public List<Layer> Layers()
		{
			return m_layers;
		}

		public int ActiveLayerIndex()
		{
			return m_activeLayerIndex;
		}

		public void SetActiveLayerIndex(int index)
		{
			if (index < 0)
			{
				return;
			}
			if (index >= m_layers.Count)
			{
				return;
			}
			m_activeLayerIndex = index;
			ResetLayerSelectionToActive();
		}

		private void ResetLayerSelectionToActive()
		{
			m_selectedLayerIndices.Clear();
			if (m_activeLayerIndex >= 0 && m_activeLayerIndex < m_layers.Count)
			{
				m_selectedLayerIndices.Add(m_activeLayerIndex);
			}
		}

		public List<int> SelectedLayerIndices()
		{
			List<int> result = new List<int>();
			for (int scan = 0; scan < m_selectedLayerIndices.Count; scan++)
			{
				int layerIndex = m_selectedLayerIndices[scan];
				if (layerIndex < 0 || layerIndex >= m_layers.Count)
				{
					continue;
				}
				bool exists = false;
				for (int check = 0; check < result.Count; check++)
				{
					if (result[check] == layerIndex)
					{
						exists = true;
						break;
					}
				}
				if (!exists)
				{
					result.Add(layerIndex);
				}
			}
			bool hasActive = false;
			for (int check = 0; check < result.Count; check++)
			{
				if (result[check] == m_activeLayerIndex)
				{
					hasActive = true;
					break;
				}
			}
			if (!hasActive && m_activeLayerIndex >= 0 && m_activeLayerIndex < m_layers.Count)
			{
				result.Add(m_activeLayerIndex);
			}
			return result;
		}

		public bool IsLayerSelected(int index)
		{
			for (int scan = 0; scan < m_selectedLayerIndices.Count; scan++)
			{
				if (m_selectedLayerIndices[scan] == index)
				{
					return true;
				}
			}
			return false;
		}

		public void ToggleLayerSelection(int index)
		{
			if (index < 0 || index >= m_layers.Count)
			{
				return;
			}
			int existing = -1;
			for (int scan = 0; scan < m_selectedLayerIndices.Count; scan++)
			{
				if (m_selectedLayerIndices[scan] == index)
				{
					existing = scan;
					break;
				}
			}
			if (existing >= 0)
			{
				m_selectedLayerIndices.RemoveAt(existing);
				if (m_selectedLayerIndices.Count == 0)
				{
					m_selectedLayerIndices.Add(index);
					m_activeLayerIndex = index;
				}
				else
				{
					m_activeLayerIndex = m_selectedLayerIndices[m_selectedLayerIndices.Count - 1];
				}
			}
			else
			{
				m_selectedLayerIndices.Add(index);
				m_activeLayerIndex = index;
			}
		}

		public void SelectLayerRange(int index)
		{
			if (index < 0 || index >= m_layers.Count)
			{
				return;
			}
			int anchor = m_activeLayerIndex;
			if (anchor < 0 || anchor >= m_layers.Count)
			{
				anchor = index;
			}
			int low = anchor;
			int high = index;
			if (high < low)
			{
				int swap = low;
				low = high;
				high = swap;
			}
			m_selectedLayerIndices.Clear();
			for (int layerIndex = low; layerIndex <= high; layerIndex++)
			{
				m_selectedLayerIndices.Add(layerIndex);
			}
			m_activeLayerIndex = index;
		}

		public Layer ActiveLayer()
		{
			if (m_activeLayerIndex < 0)
			{
				return null;
			}
			if (m_activeLayerIndex >= m_layers.Count)
			{
				return null;
			}
			return m_layers[m_activeLayerIndex];
		}

		public void AddMaskToActiveLayer(bool reveal)
		{
			Layer layer = ActiveLayer();
			if (layer == null)
			{
				return;
			}
			layer.CreateMask(reveal);
			MarkComposeDirtyAll();
		}

		public void DeleteActiveMask()
		{
			Layer layer = ActiveLayer();
			if (layer == null)
			{
				return;
			}
			layer.DeleteMask();
			MarkComposeDirtyAll();
		}

		public void SetActiveMaskEnabled(bool enabled)
		{
			Layer layer = ActiveLayer();
			if (layer == null)
			{
				return;
			}
			layer.SetMaskEnabled(enabled);
			MarkComposeDirtyAll();
		}

		public unsafe void ApplyActiveMask()
		{
			Layer layer = ActiveLayer();
			if (layer == null)
			{
				return;
			}
			if (!layer.HasMask())
			{
				return;
			}
			SKBitmap bitmap = layer.Bitmap();
			SKBitmap mask = layer.MaskBitmap();
			int width = bitmap.Width;
			int height = bitmap.Height;
			byte* bitmapBase = (byte*)bitmap.GetPixels().ToPointer();
			int bitmapRowBytes = bitmap.RowBytes;
			byte* maskBase = (byte*)mask.GetPixels().ToPointer();
			int maskRowBytes = mask.RowBytes;
			for (int y = 0; y < height; y++)
			{
				byte* bitmapRow = bitmapBase + (y * bitmapRowBytes);
				byte* maskRow = maskBase + (y * maskRowBytes);
				for (int x = 0; x < width; x++)
				{
					byte* pixel = bitmapRow + (x * 4);
					byte* maskPixel = maskRow + (x * 4);
					int alpha = pixel[3];
					int maskCoverage = maskPixel[0];
					int newAlpha = ((alpha * maskCoverage) + 127) / 255;
					pixel[3] = (byte)newAlpha;
				}
			}
			layer.DeleteMask();
			MarkComposeDirtyAll();
		}

		public bool ActiveLayerContentBox(out SKRectI box, out bool isBackground)
		{
			box = SKRectI.Empty;
			isBackground = false;
			Layer layer = ActiveLayer();
			if (layer == null)
			{
				return false;
			}
			if (layer.IsBackground())
			{
				box = new SKRectI(0, 0, m_width, m_height);
				isBackground = true;
				return true;
			}
			SKRectI bounds = PixelRegion.ComputeContentBounds(layer.Bitmap());
			if (bounds.IsEmpty)
			{
				return false;
			}
			box = new SKRectI(bounds.Left + layer.OffsetX(), bounds.Top + layer.OffsetY(), bounds.Right + layer.OffsetX(), bounds.Bottom + layer.OffsetY());
			return true;
		}

		public Layer AddLayer(string name)
		{
			Layer layer = new Layer(name, m_width, m_height, m_colorDepth);
			m_layers.Add(layer);
			m_activeLayerIndex = m_layers.Count - 1;
			ResetLayerSelectionToActive();
			return layer;
		}

		public void MoveLayer(int fromIndex, int toIndex)
		{
			if (fromIndex < 0 || fromIndex >= m_layers.Count)
			{
				return;
			}
			int target = toIndex;
			if (target < 0)
			{
				target = 0;
			}
			if (target > m_layers.Count - 1)
			{
				target = m_layers.Count - 1;
			}
			if (target == fromIndex)
			{
				return;
			}
			Layer layer = m_layers[fromIndex];
			m_layers.RemoveAt(fromIndex);
			m_layers.Insert(target, layer);
			if (m_activeLayerIndex == fromIndex)
			{
				m_activeLayerIndex = target;
			}
			else if (fromIndex < m_activeLayerIndex && m_activeLayerIndex <= target)
			{
				m_activeLayerIndex = m_activeLayerIndex - 1;
			}
			else if (target <= m_activeLayerIndex && m_activeLayerIndex < fromIndex)
			{
				m_activeLayerIndex = m_activeLayerIndex + 1;
			}
			m_dirty = true;
		}

		public void MoveLayerUp(int index)
		{
			MoveLayer(index, index + 1);
		}

		public void MoveLayerDown(int index)
		{
			MoveLayer(index, index - 1);
		}

		public void DuplicateLayer(int index)
		{
			if (index < 0 || index >= m_layers.Count)
			{
				return;
			}
			Layer source = m_layers[index];
			SKBitmap sourceBitmap = source.Bitmap();
			Layer copy = new Layer(source.Name() + " copy", sourceBitmap.Width, sourceBitmap.Height, m_colorDepth);
			copy.SetBitmap(sourceBitmap.Copy());
			copy.SetOffset(source.OffsetX(), source.OffsetY());
			copy.SetOpacity(source.Opacity());
			copy.SetBlendMode(source.BlendMode());
			copy.SetVisible(source.IsVisible());
			m_layers.Insert(index + 1, copy);
			m_activeLayerIndex = index + 1;
			m_dirty = true;
		}

		public void MergeDown(int index)
		{
			if (index <= 0 || index >= m_layers.Count)
			{
				return;
			}
			Layer upper = m_layers[index];
			Layer lower = m_layers[index - 1];
			SKBitmap upperBitmap = upper.Bitmap();
			SKBitmap lowerBitmap = lower.Bitmap();
			int upperLeft = upper.OffsetX();
			int upperTop = upper.OffsetY();
			int lowerLeft = lower.OffsetX();
			int lowerTop = lower.OffsetY();
			int left = upperLeft;
			if (lowerLeft < left)
			{
				left = lowerLeft;
			}
			int top = upperTop;
			if (lowerTop < top)
			{
				top = lowerTop;
			}
			int right = upperLeft + upperBitmap.Width;
			int lowerRight = lowerLeft + lowerBitmap.Width;
			if (lowerRight > right)
			{
				right = lowerRight;
			}
			int bottom = upperTop + upperBitmap.Height;
			int lowerBottom = lowerTop + lowerBitmap.Height;
			if (lowerBottom > bottom)
			{
				bottom = lowerBottom;
			}
			int mergedWidth = right - left;
			int mergedHeight = bottom - top;
			SKBitmap merged = new SKBitmap(mergedWidth, mergedHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			merged.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(merged);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			paint.BlendMode = SKBlendMode.SrcOver;
			paint.Color = SKColors.White.WithAlpha(lower.Opacity());
			SKImage lowerImage = SKImage.FromBitmap(lowerBitmap);
			canvas.DrawImage(lowerImage, lowerLeft - left, lowerTop - top, sampling, paint);
			lowerImage.Dispose();
			paint.BlendMode = Layer.ToSkBlendMode(upper.BlendMode());
			paint.Color = SKColors.White.WithAlpha(upper.Opacity());
			SKImage upperImage = SKImage.FromBitmap(upperBitmap);
			canvas.DrawImage(upperImage, upperLeft - left, upperTop - top, sampling, paint);
			upperImage.Dispose();
			paint.Dispose();
			canvas.Dispose();
			lower.SetBitmap(merged);
			lower.SetOffset(left, top);
			lower.SetOpacity(255);
			lower.SetBlendMode(eBlendMode.Normal);
			m_layers.RemoveAt(index);
			if (m_activeLayerIndex >= m_layers.Count)
			{
				m_activeLayerIndex = m_layers.Count - 1;
			}
			else
			{
				m_activeLayerIndex = index - 1;
			}
			m_dirty = true;
		}

		public void MergeLayers(List<int> indices)
		{
			List<int> sorted = new List<int>();
			for (int scan = 0; scan < indices.Count; scan++)
			{
				int candidate = indices[scan];
				if (candidate < 0 || candidate >= m_layers.Count)
				{
					continue;
				}
				bool exists = false;
				for (int check = 0; check < sorted.Count; check++)
				{
					if (sorted[check] == candidate)
					{
						exists = true;
						break;
					}
				}
				if (!exists)
				{
					sorted.Add(candidate);
				}
			}
			sorted.Sort();
			if (sorted.Count < 2)
			{
				return;
			}
			int lowest = sorted[0];
			SKBitmap merged = new SKBitmap(m_width, m_height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			merged.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(merged);
			SKPaint paint = new SKPaint();
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			for (int order = 0; order < sorted.Count; order++)
			{
				DrawStyledLayer(canvas, m_layers[sorted[order]], sampling, paint);
			}
			paint.Dispose();
			canvas.Dispose();
			Layer result = new Layer(m_layers[lowest].Name(), m_width, m_height, m_colorDepth);
			result.SetBitmap(merged);
			result.SetOffset(0, 0);
			List<Layer> rebuilt = new List<Layer>();
			for (int index = 0; index < m_layers.Count; index++)
			{
				bool selected = false;
				for (int check = 0; check < sorted.Count; check++)
				{
					if (sorted[check] == index)
					{
						selected = true;
						break;
					}
				}
				if (selected)
				{
					if (index == lowest)
					{
						rebuilt.Add(result);
					}
				}
				else
				{
					rebuilt.Add(m_layers[index]);
				}
			}
			m_layers = rebuilt;
			m_activeLayerIndex = rebuilt.IndexOf(result);
			ResetLayerSelectionToActive();
			m_dirty = true;
		}

		public void MergeVisible()
		{
			int visibleCount = 0;
			for (int index = 0; index < m_layers.Count; index++)
			{
				if (m_layers[index].IsVisible())
				{
					visibleCount = visibleCount + 1;
				}
			}
			if (visibleCount < 2)
			{
				return;
			}
			SKBitmap merged = new SKBitmap(m_width, m_height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			merged.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(merged);
			SKPaint paint = new SKPaint();
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				DrawStyledLayer(canvas, layer, sampling, paint);
			}
			paint.Dispose();
			canvas.Dispose();
			Layer result = new Layer("Merged", m_width, m_height, m_colorDepth);
			result.SetBitmap(merged);
			result.SetOffset(0, 0);
			List<Layer> rebuilt = new List<Layer>();
			bool inserted = false;
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (layer.IsVisible())
				{
					if (!inserted)
					{
						rebuilt.Add(result);
						inserted = true;
					}
				}
				else
				{
					rebuilt.Add(layer);
				}
			}
			m_layers = rebuilt;
			m_activeLayerIndex = rebuilt.IndexOf(result);
			ResetLayerSelectionToActive();
			m_dirty = true;
		}

		public void FlattenImage()
		{
			SKBitmap merged = new SKBitmap(m_width, m_height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			merged.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(merged);
			SKPaint paint = new SKPaint();
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				DrawStyledLayer(canvas, layer, sampling, paint);
			}
			paint.Dispose();
			canvas.Dispose();
			Layer result = new Layer("Background", m_width, m_height, m_colorDepth);
			result.SetBitmap(merged);
			result.SetOffset(0, 0);
			m_layers = new List<Layer>();
			m_layers.Add(result);
			m_activeLayerIndex = 0;
			ResetLayerSelectionToActive();
			m_dirty = true;
		}

		public void DeleteLayer(int index)
		{
			if (m_layers.Count <= 1)
			{
				return;
			}
			if (index < 0 || index >= m_layers.Count)
			{
				return;
			}
			m_layers.RemoveAt(index);
			if (m_activeLayerIndex >= m_layers.Count)
			{
				m_activeLayerIndex = m_layers.Count - 1;
			}
			for (int scan = m_selectedLayerIndices.Count - 1; scan >= 0; scan--)
			{
				int selected = m_selectedLayerIndices[scan];
				if (selected == index)
				{
					m_selectedLayerIndices.RemoveAt(scan);
				}
				else if (selected > index)
				{
					m_selectedLayerIndices[scan] = selected - 1;
				}
			}
		}

		public void DeleteSelectedLayers()
		{
			List<int> copy = new List<int>(SelectedLayerIndices());
			if (copy.Count == 0)
			{
				DeleteLayer(ActiveLayerIndex());
				return;
			}
			copy.Sort();
			for (int scan = copy.Count - 1; scan >= 0; scan--)
			{
				DeleteLayer(copy[scan]);
			}
			if (m_selectedLayerIndices.Count == 0)
			{
				m_selectedLayerIndices.Add(m_activeLayerIndex);
			}
		}

		public void MoveSelectedLayers(int shift)
		{
			if (shift == 0)
			{
				return;
			}
			List<int> copy = new List<int>(SelectedLayerIndices());
			if (copy.Count < 2)
			{
				return;
			}
			copy.Sort();
			int count = m_layers.Count;
			int blockCount = copy.Count;
			int minSelected = copy[0];
			Layer activeLayer = m_layers[m_activeLayerIndex];
			List<Layer> block = new List<Layer>();
			for (int scan = 0; scan < copy.Count; scan++)
			{
				block.Add(m_layers[copy[scan]]);
			}
			int newFirst = minSelected + shift;
			if (newFirst < 0)
			{
				newFirst = 0;
			}
			if (newFirst > count - blockCount)
			{
				newFirst = count - blockCount;
			}
			for (int scan = copy.Count - 1; scan >= 0; scan--)
			{
				m_layers.RemoveAt(copy[scan]);
			}
			for (int scan = 0; scan < block.Count; scan++)
			{
				m_layers.Insert(newFirst + scan, block[scan]);
			}
			m_selectedLayerIndices.Clear();
			for (int scan = 0; scan < blockCount; scan++)
			{
				m_selectedLayerIndices.Add(newFirst + scan);
			}
			int restored = -1;
			for (int scan = 0; scan < m_layers.Count; scan++)
			{
				if (ReferenceEquals(m_layers[scan], activeLayer))
				{
					restored = scan;
					break;
				}
			}
			if (restored < 0)
			{
				restored = newFirst;
			}
			m_activeLayerIndex = restored;
			m_dirty = true;
		}

		private void DrawStyledLayer(SKCanvas canvas, Layer layer, SKSamplingOptions sampling, SKPaint paint)
		{
			layer.DrawStyleUnder(canvas, sampling);
			paint.Color = SKColors.White.WithAlpha(layer.Opacity());
			paint.BlendMode = Layer.ToSkBlendMode(layer.BlendMode());
			SKPixmap pixmap = layer.Bitmap().PeekPixels();
			SKImage image = SKImage.FromPixels(pixmap);
			canvas.DrawImage(image, layer.OffsetX(), layer.OffsetY(), sampling, paint);
			image.Dispose();
			pixmap.Dispose();
			layer.DrawStyleOver(canvas, sampling);
		}

		private void DrawClippedStyleUnder(SKBitmap target, SKRect clipRect, SKSamplingOptions sampling, Layer layer)
		{
			if (!layer.LayerStyle().HasAnyEffect())
			{
				return;
			}
			SKCanvas canvas = new SKCanvas(target);
			canvas.Save();
			canvas.ClipRect(clipRect);
			layer.DrawStyleUnder(canvas, sampling);
			canvas.Restore();
			canvas.Dispose();
		}

		private void DrawClippedStyleOver(SKBitmap target, SKRect clipRect, SKSamplingOptions sampling, Layer layer)
		{
			if (!layer.LayerStyle().HasAnyEffect())
			{
				return;
			}
			SKCanvas canvas = new SKCanvas(target);
			canvas.Save();
			canvas.ClipRect(clipRect);
			layer.DrawStyleOver(canvas, sampling);
			canvas.Restore();
			canvas.Dispose();
		}

		private void DrawLayers(SKCanvas canvas)
		{
			SKPaint paint = new SKPaint();
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				DrawStyledLayer(canvas, layer, sampling, paint);
			}
			paint.Dispose();
		}

		private bool AllVisibleLayersNormal()
		{
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				if (layer.BlendMode() != eBlendMode.Normal)
				{
					return false;
				}
			}
			return true;
		}

		private bool AnyVisibleCustomBlend()
		{
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				if (Layer.IsCustomBlend(layer.BlendMode()))
				{
					return true;
				}
			}
			return false;
		}

		private bool AnyVisibleLayerHasStyle()
		{
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				if (layer.LayerStyle().HasAnyEffect())
				{
					return true;
				}
			}
			return false;
		}

		private bool AnyVisibleLayerHasMask()
		{
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				if (layer.HasMask() && layer.MaskEnabled())
				{
					return true;
				}
			}
			return false;
		}

		public SKBitmap Composite()
		{
			return m_composite;
		}

		public int CompositeVersion()
		{
			return m_compositeVersion;
		}

		public void ReleaseComposite()
		{
			if (m_composite != null)
			{
				m_composite.Dispose();
				m_composite = null;
			}
		}

		public void EnsureComposited(out bool updatedFull, out SKRectI updatedRegion)
		{
			updatedFull = false;
			updatedRegion = SKRectI.Empty;
			bool recreated = false;
			if (m_composite == null || m_composite.Width != m_width || m_composite.Height != m_height)
			{
				if (m_composite != null)
				{
					m_composite.Dispose();
				}
				m_composite = new SKBitmap(m_width, m_height, m_colorDepth.ToColorType(), SKAlphaType.Premul);
				recreated = true;
			}
			if (recreated || m_composeDirtyAll)
			{
				CompositeInto(m_composite);
				ClearComposeDirty();
				m_compositeVersion = m_compositeVersion + 1;
				updatedFull = true;
				return;
			}
			if (m_composeDirtyAny)
			{
				SKRectI region = ClampCompositeRegion(m_composeDirtyRect);
				if (region.Width > 0 && region.Height > 0)
				{
					CompositeRegion(m_composite, region);
					updatedRegion = region;
				}
				ClearComposeDirty();
				m_compositeVersion = m_compositeVersion + 1;
			}
		}

		private SKRectI ClampCompositeRegion(SKRectI rect)
		{
			int left = rect.Left;
			int top = rect.Top;
			int right = rect.Right;
			int bottom = rect.Bottom;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > m_width)
			{
				right = m_width;
			}
			if (bottom > m_height)
			{
				bottom = m_height;
			}
			if (right <= left || bottom <= top)
			{
				return SKRectI.Empty;
			}
			return new SKRectI(left, top, right, bottom);
		}

		public void CompositeInto(SKBitmap target)
		{
			CompositeRegion(target, new SKRectI(0, 0, m_width, m_height));
		}

		public void CompositeRegion(SKBitmap target, SKRectI region)
		{
			if (m_colorDepth != eColorDepth.Eight)
			{
				CompositeRegionSkia(target, region);
				return;
			}
			if (AllVisibleLayersNormal() && !AnyVisibleLayerHasStyle())
			{
				CompositeRegionRaw(target, region);
				return;
			}
			if (AnyVisibleCustomBlend() || AnyVisibleLayerHasMask())
			{
				CompositeRegionSoftware(target, region);
				return;
			}
			CompositeRegionSkia(target, region);
		}

		public void CompositeRangeInto(SKBitmap target, SKRectI region, int startIndex, int endExclusive)
		{
			int left = region.Left;
			int top = region.Top;
			int right = region.Right;
			int bottom = region.Bottom;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > target.Width)
			{
				right = target.Width;
			}
			if (bottom > target.Height)
			{
				bottom = target.Height;
			}
			if (right <= left || bottom <= top)
			{
				return;
			}
			if (startIndex < 0)
			{
				startIndex = 0;
			}
			if (endExclusive > m_layers.Count)
			{
				endExclusive = m_layers.Count;
			}
			SKRect clipRect = new SKRect(left, top, right, bottom);
			SKCanvas canvas = new SKCanvas(target);
			canvas.Save();
			canvas.ClipRect(clipRect);
			SKPaint clearPaint = new SKPaint();
			clearPaint.Color = SKColors.Transparent;
			clearPaint.BlendMode = SKBlendMode.Src;
			canvas.DrawRect(clipRect, clearPaint);
			clearPaint.Dispose();
			SKPaint paint = new SKPaint();
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			for (int index = startIndex; index < endExclusive; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				DrawStyledLayer(canvas, layer, sampling, paint);
			}
			paint.Dispose();
			canvas.Restore();
			canvas.Dispose();
		}

		private unsafe void CompositeRegionRaw(SKBitmap target, SKRectI region)
		{
			int canvasWidth = target.Width;
			int canvasHeight = target.Height;
			int left = region.Left;
			int top = region.Top;
			int right = region.Right;
			int bottom = region.Bottom;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > canvasWidth)
			{
				right = canvasWidth;
			}
			if (bottom > canvasHeight)
			{
				bottom = canvasHeight;
			}
			if (right <= left || bottom <= top)
			{
				return;
			}

			int layerCount = m_layers.Count;
			CompositeBandWorker worker = new CompositeBandWorker();
			worker.m_sourceBases = new IntPtr[layerCount];
			worker.m_sourceRowBytes = new int[layerCount];
			worker.m_sourceWidths = new int[layerCount];
			worker.m_sourceHeights = new int[layerCount];
			worker.m_sourceOffsetsX = new int[layerCount];
			worker.m_sourceOffsetsY = new int[layerCount];
			worker.m_sourceOpacities = new int[layerCount];
			worker.m_sourceMaskBases = new IntPtr[layerCount];
			worker.m_sourceMaskRowBytes = new int[layerCount];
			int visibleCount = 0;
			for (int index = 0; index < layerCount; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				SKBitmap bitmap = layer.Bitmap();
				worker.m_sourceBases[visibleCount] = bitmap.GetPixels();
				worker.m_sourceRowBytes[visibleCount] = bitmap.RowBytes;
				worker.m_sourceWidths[visibleCount] = bitmap.Width;
				worker.m_sourceHeights[visibleCount] = bitmap.Height;
				worker.m_sourceOffsetsX[visibleCount] = layer.OffsetX();
				worker.m_sourceOffsetsY[visibleCount] = layer.OffsetY();
				worker.m_sourceOpacities[visibleCount] = layer.Opacity();
				if (layer.HasMask() && layer.MaskEnabled())
				{
					worker.m_sourceMaskBases[visibleCount] = layer.MaskBitmap().GetPixels();
					worker.m_sourceMaskRowBytes[visibleCount] = layer.MaskBitmap().RowBytes;
				}
				else
				{
					worker.m_sourceMaskBases[visibleCount] = IntPtr.Zero;
					worker.m_sourceMaskRowBytes[visibleCount] = 0;
				}
				visibleCount++;
			}
			worker.m_visibleCount = visibleCount;
			worker.m_targetBase = target.GetPixels();
			worker.m_targetRowBytes = target.RowBytes;
			worker.m_left = left;
			worker.m_right = right;
			RowBands.Run(top, bottom, worker.Band);
		}

		private void CompositeRegionSkia(SKBitmap target, SKRectI region)
		{
			SKCanvas canvas = new SKCanvas(target);
			SKRect clipRect = new SKRect(region.Left, region.Top, region.Right, region.Bottom);
			canvas.Save();
			canvas.ClipRect(clipRect);
			SKPaint clearPaint = new SKPaint();
			clearPaint.Color = SKColors.Transparent;
			clearPaint.BlendMode = SKBlendMode.Src;
			canvas.DrawRect(clipRect, clearPaint);
			clearPaint.Dispose();
			DrawLayers(canvas);
			canvas.Restore();
			canvas.Dispose();
		}

		private void CompositeRegionSoftware(SKBitmap target, SKRectI region)
		{
			int left = region.Left;
			int top = region.Top;
			int right = region.Right;
			int bottom = region.Bottom;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > target.Width)
			{
				right = target.Width;
			}
			if (bottom > target.Height)
			{
				bottom = target.Height;
			}
			if (right <= left || bottom <= top)
			{
				return;
			}
			SKRect clipRect = new SKRect(left, top, right, bottom);

			SKCanvas clearCanvas = new SKCanvas(target);
			clearCanvas.Save();
			clearCanvas.ClipRect(clipRect);
			SKPaint clearPaint = new SKPaint();
			clearPaint.Color = SKColors.Transparent;
			clearPaint.BlendMode = SKBlendMode.Src;
			clearCanvas.DrawRect(clipRect, clearPaint);
			clearPaint.Dispose();
			clearCanvas.Restore();
			clearCanvas.Dispose();

			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				if (Layer.IsCustomBlend(layer.BlendMode()))
				{
					DrawClippedStyleUnder(target, clipRect, sampling, layer);
					BlendCustomLayer(target, left, top, right, bottom, layer);
					DrawClippedStyleOver(target, clipRect, sampling, layer);
				}
				else
				{
					if (layer.HasMask() && layer.MaskEnabled() && !layer.LayerStyle().HasAnyEffect())
					{
						BlendMaskedNormalLayer(target, left, top, right, bottom, layer);
					}
					else
					{
						SKCanvas canvas = new SKCanvas(target);
						canvas.Save();
						canvas.ClipRect(clipRect);
						SKPaint paint = new SKPaint();
						DrawStyledLayer(canvas, layer, sampling, paint);
						paint.Dispose();
						canvas.Restore();
						canvas.Dispose();
					}
				}
			}
		}

		private void BlendCustomLayer(SKBitmap target, int left, int top, int right, int bottom, Layer layer)
		{
			SKBitmap source = layer.Bitmap();
			CustomBlendBandWorker worker = new CustomBlendBandWorker();
			worker.m_sourceBase = source.GetPixels();
			worker.m_sourceRowBytes = source.RowBytes;
			worker.m_sourceWidth = source.Width;
			worker.m_sourceHeight = source.Height;
			worker.m_offsetX = layer.OffsetX();
			worker.m_offsetY = layer.OffsetY();
			worker.m_opacity = layer.Opacity();
			worker.m_mode = layer.BlendMode();
			if (layer.HasMask() && layer.MaskEnabled())
			{
				worker.m_maskBase = layer.MaskBitmap().GetPixels();
				worker.m_maskRowBytes = layer.MaskBitmap().RowBytes;
			}
			else
			{
				worker.m_maskBase = IntPtr.Zero;
				worker.m_maskRowBytes = 0;
			}
			worker.m_targetBase = target.GetPixels();
			worker.m_targetRowBytes = target.RowBytes;
			worker.m_left = left;
			worker.m_right = right;
			RowBands.Run(top, bottom, worker.Band);
		}

		private unsafe void BlendMaskedNormalLayer(SKBitmap target, int left, int top, int right, int bottom, Layer layer)
		{
			SKBitmap source = layer.Bitmap();
			SKBitmap mask = layer.MaskBitmap();
			IntPtr sourceBasePtr = source.GetPixels();
			int sourceRowBytes = source.RowBytes;
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			int offsetX = layer.OffsetX();
			int offsetY = layer.OffsetY();
			int opacity = layer.Opacity();
			IntPtr maskBasePtr = mask.GetPixels();
			int maskRowBytes = mask.RowBytes;
			IntPtr targetBasePtr = target.GetPixels();
			int targetRowBytes = target.RowBytes;
			byte* sourceBase = (byte*)sourceBasePtr.ToPointer();
			byte* maskBase = (byte*)maskBasePtr.ToPointer();
			byte* targetBase = (byte*)targetBasePtr.ToPointer();
			for (int canvasY = top; canvasY < bottom; canvasY++)
			{
				int bitmapY = canvasY - offsetY;
				if (bitmapY < 0 || bitmapY >= sourceHeight)
				{
					continue;
				}
				byte* sourceRow = sourceBase + (bitmapY * sourceRowBytes);
				byte* maskRow = maskBase + (bitmapY * maskRowBytes);
				byte* targetRow = targetBase + (canvasY * targetRowBytes);
				for (int canvasX = left; canvasX < right; canvasX++)
				{
					int bitmapX = canvasX - offsetX;
					if (bitmapX < 0 || bitmapX >= sourceWidth)
					{
						continue;
					}
					byte* sourcePixel = sourceRow + (bitmapX * 4);
					int sourceAlpha = sourcePixel[3];
					if (sourceAlpha == 0)
					{
						continue;
					}
					byte* maskPixel = maskRow + (bitmapX * 4);
					int maskCoverage = maskPixel[0];
					sourceAlpha = ((sourceAlpha * maskCoverage) + 127) / 255;
					if (sourceAlpha == 0)
					{
						continue;
					}
					int effectiveAlpha = ((sourceAlpha * opacity) + 127) / 255;
					if (effectiveAlpha == 0)
					{
						continue;
					}
					int premultipliedRed = ((sourcePixel[0] * effectiveAlpha) + 127) / 255;
					int premultipliedGreen = ((sourcePixel[1] * effectiveAlpha) + 127) / 255;
					int premultipliedBlue = ((sourcePixel[2] * effectiveAlpha) + 127) / 255;
					int inverseAlpha = 255 - effectiveAlpha;
					byte* targetPixel = targetRow + (canvasX * 4);
					int resultRed = premultipliedRed + (((targetPixel[0] * inverseAlpha) + 127) / 255);
					int resultGreen = premultipliedGreen + (((targetPixel[1] * inverseAlpha) + 127) / 255);
					int resultBlue = premultipliedBlue + (((targetPixel[2] * inverseAlpha) + 127) / 255);
					int resultAlpha = effectiveAlpha + (((targetPixel[3] * inverseAlpha) + 127) / 255);
					if (resultRed > 255)
					{
						resultRed = 255;
					}
					if (resultGreen > 255)
					{
						resultGreen = 255;
					}
					if (resultBlue > 255)
					{
						resultBlue = 255;
					}
					if (resultAlpha > 255)
					{
						resultAlpha = 255;
					}
					targetPixel[0] = (byte)resultRed;
					targetPixel[1] = (byte)resultGreen;
					targetPixel[2] = (byte)resultBlue;
					targetPixel[3] = (byte)resultAlpha;
				}
			}
		}

		private SKBitmap BakeLayerToCanvas(Layer layer)
		{
			SKBitmap baked = new SKBitmap(m_width, m_height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			baked.Erase(SKColors.Transparent);
			SKCanvas canvas = new SKCanvas(baked);
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			SKPaint paint = new SKPaint();
			paint.BlendMode = SKBlendMode.Src;
			SKImage image = SKImage.FromBitmap(layer.Bitmap());
			canvas.DrawImage(image, layer.OffsetX(), layer.OffsetY(), sampling, paint);
			image.Dispose();
			paint.Dispose();
			canvas.Dispose();
			return baked;
		}

		private static unsafe SKBitmap RotateFlipBitmap(SKBitmap source, int kind)
		{
			int sourceWidth = source.Width;
			int sourceHeight = source.Height;
			int destinationWidth = sourceWidth;
			int destinationHeight = sourceHeight;
			if (kind == 3 || kind == 4)
			{
				destinationWidth = sourceHeight;
				destinationHeight = sourceWidth;
			}
			SKBitmap destination = new SKBitmap(destinationWidth, destinationHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
			byte* sourceBase = (byte*)source.GetPixels().ToPointer();
			int sourceRowBytes = source.RowBytes;
			byte* destinationBase = (byte*)destination.GetPixels().ToPointer();
			int destinationRowBytes = destination.RowBytes;
			for (int y = 0; y < destinationHeight; y++)
			{
				byte* destinationRow = destinationBase + (y * destinationRowBytes);
				for (int x = 0; x < destinationWidth; x++)
				{
					int sourceX;
					int sourceY;
					if (kind == 0)
					{
						sourceX = sourceWidth - 1 - x;
						sourceY = y;
					}
					else if (kind == 1)
					{
						sourceX = x;
						sourceY = sourceHeight - 1 - y;
					}
					else if (kind == 2)
					{
						sourceX = sourceWidth - 1 - x;
						sourceY = sourceHeight - 1 - y;
					}
					else if (kind == 3)
					{
						sourceX = y;
						sourceY = sourceHeight - 1 - x;
					}
					else
					{
						sourceX = sourceWidth - 1 - y;
						sourceY = x;
					}
					byte* sourcePixel = sourceBase + (sourceY * sourceRowBytes) + (sourceX * 4);
					byte* destinationPixel = destinationRow + (x * 4);
					destinationPixel[0] = sourcePixel[0];
					destinationPixel[1] = sourcePixel[1];
					destinationPixel[2] = sourcePixel[2];
					destinationPixel[3] = sourcePixel[3];
				}
			}
			return destination;
		}

		private void ApplyRotateFlip(int kind, bool swapDimensions)
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap destination = RotateFlipBitmap(baked, kind);
				baked.Dispose();
				layer.SetBitmap(destination);
				layer.SetOffset(0, 0);
			}
			if (swapDimensions)
			{
				int temp = m_width;
				m_width = m_height;
				m_height = temp;
			}
			m_dirty = true;
		}

		public void FlipHorizontal()
		{
			ApplyRotateFlip(0, false);
		}

		public void FlipVertical()
		{
			ApplyRotateFlip(1, false);
		}

		public void Rotate180()
		{
			ApplyRotateFlip(2, false);
		}

		public void Rotate90()
		{
			ApplyRotateFlip(3, true);
		}

		public void Rotate270()
		{
			ApplyRotateFlip(4, true);
		}

		public void CropToSelection()
		{
			if (!m_selection.IsActive())
			{
				return;
			}
			CropToRect(m_selection.Bounds());
		}

		public void CropToRect(SKRectI rect)
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			int left = rect.Left;
			int top = rect.Top;
			int right = rect.Right;
			int bottom = rect.Bottom;
			if (left < 0)
			{
				left = 0;
			}
			if (top < 0)
			{
				top = 0;
			}
			if (right > m_width)
			{
				right = m_width;
			}
			if (bottom > m_height)
			{
				bottom = m_height;
			}
			int cropWidth = right - left;
			int cropHeight = bottom - top;
			if (cropWidth <= 0 || cropHeight <= 0)
			{
				return;
			}
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap destination = new SKBitmap(cropWidth, cropHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				destination.Erase(SKColors.Transparent);
				SKCanvas canvas = new SKCanvas(destination);
				SKPaint paint = new SKPaint();
				paint.BlendMode = SKBlendMode.Src;
				SKPixmap pixmap = baked.PeekPixels();
				SKImage image = SKImage.FromPixels(pixmap);
				canvas.DrawImage(image, -left, -top, sampling, paint);
				image.Dispose();
				pixmap.Dispose();
				paint.Dispose();
				canvas.Dispose();
				baked.Dispose();
				layer.SetBitmap(destination);
				layer.SetOffset(0, 0);
			}
			m_width = cropWidth;
			m_height = cropHeight;
			m_selection.Clear();
			m_dirty = true;
		}

		public void RotateArbitrary(double angleDegrees, int interpolation)
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			int newWidth = m_width;
			int newHeight = m_height;
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap rotated = RotateTransform.Rotate(baked, angleDegrees, interpolation);
				baked.Dispose();
				layer.SetBitmap(rotated);
				layer.SetOffset(0, 0);
				newWidth = rotated.Width;
				newHeight = rotated.Height;
			}
			m_width = newWidth;
			m_height = newHeight;
			m_dirty = true;
		}

		public void Trim()
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			int unionLeft = m_width;
			int unionTop = m_height;
			int unionRight = -1;
			int unionBottom = -1;
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKRectI contentBounds = PixelRegion.ComputeContentBounds(layer.Bitmap());
				if (contentBounds.Width <= 0)
				{
					continue;
				}
				int canvasLeft = contentBounds.Left + layer.OffsetX();
				int canvasTop = contentBounds.Top + layer.OffsetY();
				int canvasRight = contentBounds.Right + layer.OffsetX();
				int canvasBottom = contentBounds.Bottom + layer.OffsetY();
				if (canvasLeft < unionLeft)
				{
					unionLeft = canvasLeft;
				}
				if (canvasTop < unionTop)
				{
					unionTop = canvasTop;
				}
				if (canvasRight > unionRight)
				{
					unionRight = canvasRight;
				}
				if (canvasBottom > unionBottom)
				{
					unionBottom = canvasBottom;
				}
			}
			if (unionRight < 0)
			{
				return;
			}
			if (unionLeft < 0)
			{
				unionLeft = 0;
			}
			if (unionTop < 0)
			{
				unionTop = 0;
			}
			if (unionRight > m_width)
			{
				unionRight = m_width;
			}
			if (unionBottom > m_height)
			{
				unionBottom = m_height;
			}
			int trimWidth = unionRight - unionLeft;
			int trimHeight = unionBottom - unionTop;
			if (trimWidth <= 0 || trimHeight <= 0)
			{
				return;
			}
			if (unionLeft == 0 && unionTop == 0 && trimWidth == m_width && trimHeight == m_height)
			{
				return;
			}
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap destination = PixelRegion.ExtractRegion(baked, new SKRectI(unionLeft, unionTop, unionRight, unionBottom));
				layer.SetBitmap(destination);
				layer.SetOffset(0, 0);
			}
			m_width = trimWidth;
			m_height = trimHeight;
			m_dirty = true;
		}

		public void ResizeCanvas(int newWidth, int newHeight, int anchorX, int anchorY)
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			if (newWidth <= 0 || newHeight <= 0)
			{
				return;
			}
			int dx = 0;
			if (anchorX == 1)
			{
				dx = newWidth - m_width;
			}
			else if (anchorX == 0)
			{
				dx = (newWidth - m_width) / 2;
			}
			int dy = 0;
			if (anchorY == 1)
			{
				dy = newHeight - m_height;
			}
			else if (anchorY == 0)
			{
				dy = (newHeight - m_height) / 2;
			}
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap destination = new SKBitmap(newWidth, newHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				destination.Erase(SKColors.Transparent);
				PixelRegion.ApplyRegion(destination, baked, dx, dy);
				layer.SetBitmap(destination);
				layer.SetOffset(0, 0);
			}
			m_width = newWidth;
			m_height = newHeight;
			m_dirty = true;
		}

		public void ScaleImage(int newWidth, int newHeight, int interpolation)
		{
			if (m_layers.Count == 0)
			{
				return;
			}
			if (newWidth <= 0 || newHeight <= 0)
			{
				return;
			}
			SKFilterMode filterMode = SKFilterMode.Linear;
			if (interpolation == 0)
			{
				filterMode = SKFilterMode.Nearest;
			}
			SKSamplingOptions sampling = new SKSamplingOptions(filterMode, SKMipmapMode.None);
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				SKBitmap baked = BakeLayerToCanvas(layer);
				SKBitmap destination = new SKBitmap(newWidth, newHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
				destination.Erase(SKColors.Transparent);
				SKCanvas canvas = new SKCanvas(destination);
				SKImage image = SKImage.FromBitmap(baked);
				SKRect destinationRect = new SKRect(0.0f, 0.0f, newWidth, newHeight);
				SKPaint paint = new SKPaint();
				paint.BlendMode = SKBlendMode.Src;
				canvas.DrawImage(image, destinationRect, sampling, paint);
				paint.Dispose();
				image.Dispose();
				canvas.Dispose();
				layer.SetBitmap(destination);
				layer.SetOffset(0, 0);
			}
			m_width = newWidth;
			m_height = newHeight;
			m_dirty = true;
		}
	}
}
