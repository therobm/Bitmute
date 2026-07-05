using System;
using Bitmute.Imaging;
using Microsoft.Maui.Controls;
using SkiaSharp;
using Bitmute.UI.Components;

namespace Bitmute.UI.Dialogs
{
	public class LayerStyleDialog : MultiModal
	{
		private static readonly string[] s_blendModeNames = new string[]
		{
			"Normal", "Dissolve", "Darken", "Multiply", "Color Burn", "Linear Burn", "Darker Color",
			"Lighten", "Screen", "Color Dodge", "Linear Dodge (Add)", "Lighter Color",
			"Overlay", "Soft Light", "Hard Light", "Vivid Light", "Linear Light", "Pin Light", "Hard Mix",
			"Difference", "Exclusion", "Subtract", "Divide",
			"Hue", "Saturation", "Color", "Luminosity"
		};
		private static readonly string[] s_strokePositions = new string[] { "Inside", "Center", "Outside" };

		private LayerStyle m_style;

		private IntSlider m_strokeSize;
		private ListPicker m_strokePosition;
		private ColorSwatch m_strokeSwatch;
		private IntSlider m_strokeOpacity;
		private ListPicker m_strokeMode;
		private ColorSwatch m_shadowSwatch;
		private IntSlider m_shadowOpacity;
		private IntSlider m_shadowAngle;
		private IntSlider m_shadowDistance;
		private IntSlider m_shadowSize;
		private IntSlider m_shadowSpread;
		private ListPicker m_shadowMode;
		private ColorSwatch m_glowSwatch;
		private IntSlider m_glowOpacity;
		private IntSlider m_glowSize;
		private IntSlider m_glowSpread;
		private ListPicker m_glowMode;
		private ColorSwatch m_innerGlowSwatch;
		private IntSlider m_innerGlowOpacity;
		private IntSlider m_innerGlowSize;
		private IntSlider m_innerGlowSpread;
		private ListPicker m_innerGlowMode;
		private IntSlider m_bevelDepth;
		private IntSlider m_bevelSize;
		private IntSlider m_bevelAngle;
		private ColorSwatch m_bevelHighlightSwatch;
		private IntSlider m_bevelHighlightOpacity;
		private ColorSwatch m_bevelShadowSwatch;
		private IntSlider m_bevelShadowOpacity;
		private ListPicker m_bevelMode;

		private static eBlendMode BlendModeFromIndex(int index)
		{
			if (index < (int)eBlendMode.Normal || index > (int)eBlendMode.Luminosity)
			{
				return eBlendMode.Normal;
			}
			return (eBlendMode)index;
		}

		private static VerticalStackLayout BuildPanel(View[] fields)
		{
			VerticalStackLayout panel = new VerticalStackLayout();
			panel.Spacing = UiConstants.DialogRowSpacing;
			for (int index = 0; index < fields.Length; index++)
			{
				panel.Add(fields[index]);
			}
			return panel;
		}

		private void Preview()
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.PreviewLayerStyle(m_style);
			}
		}

		private void OnStrokeEnableChanged(bool value)
		{
			m_style.m_hasStroke = value;
			Preview();
		}

		private void OnShadowEnableChanged(bool value)
		{
			m_style.m_hasDropShadow = value;
			Preview();
		}

		private void OnGlowEnableChanged(bool value)
		{
			m_style.m_hasOuterGlow = value;
			Preview();
		}

		private void OnInnerGlowEnableChanged(bool value)
		{
			m_style.m_hasInnerGlow = value;
			Preview();
		}

		private void OnBevelEnableChanged(bool value)
		{
			m_style.m_hasBevel = value;
			Preview();
		}

		private void OnStrokeSizeChanged(int value)
		{
			m_style.m_strokeSize = value;
			Preview();
		}

		private void OnStrokePositionChanged(int index)
		{
			if (index < 0)
			{
				index = 2;
			}
			m_style.m_strokePosition = index;
			Preview();
		}

		private void OnStrokeOpacityChanged(int value)
		{
			m_style.m_strokeOpacity = value;
			Preview();
		}

		private void OnStrokeModeChanged(int index)
		{
			m_style.m_strokeBlendMode = BlendModeFromIndex(index);
			Preview();
		}

		private void OnStrokeColorPicked(SKColor color)
		{
			m_style.m_strokeColor = color;
			Preview();
		}

		private void OnShadowOpacityChanged(int value)
		{
			m_style.m_shadowOpacity = value;
			Preview();
		}

		private void OnShadowAngleChanged(int value)
		{
			m_style.m_shadowAngle = value;
			Preview();
		}

		private void OnShadowDistanceChanged(int value)
		{
			m_style.m_shadowDistance = value;
			Preview();
		}

		private void OnShadowSizeChanged(int value)
		{
			m_style.m_shadowSize = value;
			Preview();
		}

		private void OnShadowSpreadChanged(int value)
		{
			m_style.m_shadowSpread = value;
			Preview();
		}

		private void OnShadowModeChanged(int index)
		{
			m_style.m_shadowBlendMode = BlendModeFromIndex(index);
			Preview();
		}

		private void OnShadowColorPicked(SKColor color)
		{
			m_style.m_shadowColor = color;
			Preview();
		}

		private void OnGlowOpacityChanged(int value)
		{
			m_style.m_glowOpacity = value;
			Preview();
		}

		private void OnGlowSizeChanged(int value)
		{
			m_style.m_glowSize = value;
			Preview();
		}

		private void OnGlowSpreadChanged(int value)
		{
			m_style.m_glowSpread = value;
			Preview();
		}

		private void OnGlowModeChanged(int index)
		{
			m_style.m_glowBlendMode = BlendModeFromIndex(index);
			Preview();
		}

		private void OnGlowColorPicked(SKColor color)
		{
			m_style.m_glowColor = color;
			Preview();
		}

		private void OnInnerGlowOpacityChanged(int value)
		{
			m_style.m_innerGlowOpacity = value;
			Preview();
		}

		private void OnInnerGlowSizeChanged(int value)
		{
			m_style.m_innerGlowSize = value;
			Preview();
		}

		private void OnInnerGlowSpreadChanged(int value)
		{
			m_style.m_innerGlowSpread = value;
			Preview();
		}

		private void OnInnerGlowModeChanged(int index)
		{
			m_style.m_innerGlowBlendMode = BlendModeFromIndex(index);
			Preview();
		}

		private void OnInnerGlowColorPicked(SKColor color)
		{
			m_style.m_innerGlowColor = color;
			Preview();
		}

		private void OnBevelDepthChanged(int value)
		{
			m_style.m_bevelDepth = value;
			Preview();
		}

		private void OnBevelSizeChanged(int value)
		{
			m_style.m_bevelSize = value;
			Preview();
		}

		private void OnBevelAngleChanged(int value)
		{
			m_style.m_bevelAngle = value;
			Preview();
		}

		private void OnBevelHighlightOpacityChanged(int value)
		{
			m_style.m_bevelHighlightOpacity = value;
			Preview();
		}

		private void OnBevelHighlightColorPicked(SKColor color)
		{
			m_style.m_bevelHighlightColor = color;
			Preview();
		}

		private void OnBevelShadowOpacityChanged(int value)
		{
			m_style.m_bevelShadowOpacity = value;
			Preview();
		}

		private void OnBevelShadowColorPicked(SKColor color)
		{
			m_style.m_bevelShadowColor = color;
			Preview();
		}

		private void OnBevelModeChanged(int index)
		{
			m_style.m_bevelBlendMode = BlendModeFromIndex(index);
			Preview();
		}

		private void OnCancelClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.CancelLayerStyle();
			}
		}

		private void OnApplyClicked(object sender, EventArgs eventArgs)
		{
			MainView main = MainView.Self;
			if (main != null)
			{
				main.CommitLayerStyle(m_style);
			}
		}

		public LayerStyleDialog(LayerStyle style)
		{
			m_style = style;

			m_strokeSize = new IntSlider("Size", 1, 100, m_style.m_strokeSize, "px", OnStrokeSizeChanged);
			m_strokePosition = new ListPicker("Position", s_strokePositions, m_style.m_strokePosition, OnStrokePositionChanged);
			m_strokeSwatch = new ColorSwatch("Color", m_style.m_strokeColor, OnStrokeColorPicked);
			m_strokeOpacity = new IntSlider("Opacity", 0, 100, m_style.m_strokeOpacity, "%", OnStrokeOpacityChanged);
			m_strokeMode = new ListPicker("Mode", s_blendModeNames, (int)m_style.m_strokeBlendMode, OnStrokeModeChanged);

			m_shadowSwatch = new ColorSwatch("Color", m_style.m_shadowColor, OnShadowColorPicked);
			m_shadowOpacity = new IntSlider("Opacity", 0, 100, m_style.m_shadowOpacity, "%", OnShadowOpacityChanged);
			m_shadowAngle = new IntSlider("Angle", 0, 360, m_style.m_shadowAngle, "°", OnShadowAngleChanged);
			m_shadowDistance = new IntSlider("Distance", 0, 100, m_style.m_shadowDistance, "px", OnShadowDistanceChanged);
			m_shadowSize = new IntSlider("Size", 0, 100, m_style.m_shadowSize, "px", OnShadowSizeChanged);
			m_shadowSpread = new IntSlider("Spread", 0, 100, m_style.m_shadowSpread, "%", OnShadowSpreadChanged);
			m_shadowMode = new ListPicker("Mode", s_blendModeNames, (int)m_style.m_shadowBlendMode, OnShadowModeChanged);

			m_glowSwatch = new ColorSwatch("Color", m_style.m_glowColor, OnGlowColorPicked);
			m_glowOpacity = new IntSlider("Opacity", 0, 100, m_style.m_glowOpacity, "%", OnGlowOpacityChanged);
			m_glowSize = new IntSlider("Size", 0, 100, m_style.m_glowSize, "px", OnGlowSizeChanged);
			m_glowSpread = new IntSlider("Spread", 0, 100, m_style.m_glowSpread, "%", OnGlowSpreadChanged);
			m_glowMode = new ListPicker("Mode", s_blendModeNames, (int)m_style.m_glowBlendMode, OnGlowModeChanged);

			m_innerGlowSwatch = new ColorSwatch("Color", m_style.m_innerGlowColor, OnInnerGlowColorPicked);
			m_innerGlowOpacity = new IntSlider("Opacity", 0, 100, m_style.m_innerGlowOpacity, "%", OnInnerGlowOpacityChanged);
			m_innerGlowSize = new IntSlider("Size", 0, 100, m_style.m_innerGlowSize, "px", OnInnerGlowSizeChanged);
			m_innerGlowSpread = new IntSlider("Spread", 0, 100, m_style.m_innerGlowSpread, "%", OnInnerGlowSpreadChanged);
			m_innerGlowMode = new ListPicker("Mode", s_blendModeNames, (int)m_style.m_innerGlowBlendMode, OnInnerGlowModeChanged);

			m_bevelDepth = new IntSlider("Depth", 1, 100, m_style.m_bevelDepth, "%", OnBevelDepthChanged);
			m_bevelSize = new IntSlider("Size", 0, 100, m_style.m_bevelSize, "px", OnBevelSizeChanged);
			m_bevelAngle = new IntSlider("Angle", 0, 360, m_style.m_bevelAngle, "°", OnBevelAngleChanged);
			m_bevelHighlightSwatch = new ColorSwatch("Highlight", m_style.m_bevelHighlightColor, OnBevelHighlightColorPicked);
			m_bevelHighlightOpacity = new IntSlider("Hi Opacity", 0, 100, m_style.m_bevelHighlightOpacity, "%", OnBevelHighlightOpacityChanged);
			m_bevelShadowSwatch = new ColorSwatch("Shadow", m_style.m_bevelShadowColor, OnBevelShadowColorPicked);
			m_bevelShadowOpacity = new IntSlider("Sh Opacity", 0, 100, m_style.m_bevelShadowOpacity, "%", OnBevelShadowOpacityChanged);
			m_bevelMode = new ListPicker("Mode", s_blendModeNames, (int)m_style.m_bevelBlendMode, OnBevelModeChanged);

			VerticalStackLayout strokePanel = BuildPanel(new View[] { m_strokeSize, m_strokePosition, m_strokeSwatch, m_strokeOpacity, m_strokeMode });
			VerticalStackLayout shadowPanel = BuildPanel(new View[] { m_shadowSwatch, m_shadowOpacity, m_shadowAngle, m_shadowDistance, m_shadowSize, m_shadowSpread, m_shadowMode });
			VerticalStackLayout glowPanel = BuildPanel(new View[] { m_glowSwatch, m_glowOpacity, m_glowSize, m_glowSpread, m_glowMode });
			VerticalStackLayout innerGlowPanel = BuildPanel(new View[] { m_innerGlowSwatch, m_innerGlowOpacity, m_innerGlowSize, m_innerGlowSpread, m_innerGlowMode });
			VerticalStackLayout bevelPanel = BuildPanel(new View[] { m_bevelDepth, m_bevelSize, m_bevelAngle, m_bevelHighlightSwatch, m_bevelHighlightOpacity, m_bevelShadowSwatch, m_bevelShadowOpacity, m_bevelMode });

			AddSection("Stroke", strokePanel, m_style.m_hasStroke, OnStrokeEnableChanged);
			AddSection("Drop Shadow", shadowPanel, m_style.m_hasDropShadow, OnShadowEnableChanged);
			AddSection("Outer Glow", glowPanel, m_style.m_hasOuterGlow, OnGlowEnableChanged);
			AddSection("Inner Glow", innerGlowPanel, m_style.m_hasInnerGlow, OnInnerGlowEnableChanged);
			AddSection("Bevel & Emboss", bevelPanel, m_style.m_hasBevel, OnBevelEnableChanged);
			SelectSection(0);

			Button cancelButton = SecondaryButton("Cancel", OnCancelClicked);
			Button okButton = PrimaryButton("OK", OnApplyClicked);
			ComposeSections("Layer Style", ButtonRow(cancelButton, okButton), 160.0, 340.0);
		}
	}
}
