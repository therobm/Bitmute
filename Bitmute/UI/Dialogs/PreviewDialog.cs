using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Bitmute.UI.Components;

namespace Bitmute.UI.Dialogs
{
	public abstract class PreviewDialog : FieldDialog
	{
		private CheckField m_previewField;
		private SKCanvasView m_previewPane;
		private bool m_applied;

		protected abstract void RunPreview();

		protected abstract void RestorePreview();

		protected abstract void AbandonPreview();

		private void OnPreviewToggled(bool value)
		{
			if (value)
			{
				RunPreview();
			}
			else
			{
				RestorePreview();
			}
		}

		private void OnPanePaint(object sender, SKPaintSurfaceEventArgs eventArgs)
		{
			DrawPreviewPane(eventArgs.Surface.Canvas, eventArgs.Info);
		}

		protected virtual void DrawPreviewPane(SKCanvas canvas, SKImageInfo info)
		{
		}

		protected void AddPreviewField()
		{
			m_previewField = new CheckField("Preview", true, OnPreviewToggled);
			AddField(m_previewField);
		}

		protected void AddPreviewPane(double width, double height)
		{
			m_previewPane = new SKCanvasView();
			m_previewPane.WidthRequest = width;
			m_previewPane.HeightRequest = height;
			m_previewPane.PaintSurface += OnPanePaint;
			AddField(m_previewPane);
		}

		protected void RefreshPreviewPane()
		{
			if (m_previewPane != null)
			{
				m_previewPane.InvalidateSurface();
			}
		}

		protected bool PreviewEnabled()
		{
			if (m_previewField == null)
			{
				return false;
			}
			return m_previewField.Checked();
		}

		protected void NotifyFieldChanged()
		{
			if (!PreviewEnabled())
			{
				return;
			}
			RunPreview();
		}

		protected void MarkApplied()
		{
			m_applied = true;
		}

		public void CancelPreview()
		{
			if (m_applied)
			{
				return;
			}
			if (m_previewField == null)
			{
				return;
			}
			AbandonPreview();
		}
	}
}
