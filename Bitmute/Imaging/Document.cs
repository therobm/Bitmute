using System.Collections.Generic;
using SkiaSharp;

namespace Bitmute.Imaging
{
	public class Document
	{
		private int m_width;
		private int m_height;
		private string m_title;
		private List<Layer> m_layers;
		private int m_activeLayerIndex;

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
			m_layers = new List<Layer>();
			Layer background = new Layer("Background", width, height);
			background.FillWhite();
			m_layers.Add(background);
			m_activeLayerIndex = 0;
		}

		public int Width()
		{
			return m_width;
		}

		public int Height()
		{
			return m_height;
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

		public Layer AddLayer(string name)
		{
			Layer layer = new Layer(name, m_width, m_height);
			m_layers.Add(layer);
			m_activeLayerIndex = m_layers.Count - 1;
			return layer;
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
		}

		public void CompositeInto(SKBitmap target)
		{
			SKCanvas canvas = new SKCanvas(target);
			canvas.Clear(SKColors.Transparent);
			SKPaint paint = new SKPaint();
			SKSamplingOptions sampling = new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None);
			for (int index = 0; index < m_layers.Count; index++)
			{
				Layer layer = m_layers[index];
				if (!layer.IsVisible())
				{
					continue;
				}
				paint.Color = SKColors.White.WithAlpha(layer.Opacity());
				paint.BlendMode = Layer.ToSkBlendMode(layer.BlendMode());
				SKImage image = SKImage.FromBitmap(layer.Bitmap());
				canvas.DrawImage(image, 0.0f, 0.0f, sampling, paint);
				image.Dispose();
			}
			paint.Dispose();
			canvas.Dispose();
		}
	}
}
