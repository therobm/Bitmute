using Microsoft.Maui.Storage;

namespace Bitmute.UI
{
	public enum ePanelId
	{
		Navigator,
		Swatches,
		Layers,
		Info
	}

	public class WorkspaceState
	{
		private bool m_rulersEnabled;
		private bool m_gridEnabled;
		private bool m_snapEnabled;
		private bool m_snapTargetGuides;
		private bool m_snapTargetGrid;
		private bool m_snapTargetEdges;
		private bool m_snapTargetLayerBounds;
		private int m_channelViewMode;
		private bool[] m_channelVisible;
		private bool m_navigatorPanelVisible;
		private bool m_swatchesPanelVisible;
		private bool m_layersPanelVisible;
		private bool m_infoPanelVisible;
		private bool m_patternPreview;

		public WorkspaceState()
		{
			m_rulersEnabled = Preferences.Default.Get("rulers_enabled", true);
			m_gridEnabled = Preferences.Default.Get("grid_enabled", false);
			m_snapEnabled = Preferences.Default.Get("snap_enabled", true);
			m_snapTargetGuides = Preferences.Default.Get("snap_target_guides", true);
			m_snapTargetGrid = Preferences.Default.Get("snap_target_grid", true);
			m_snapTargetEdges = Preferences.Default.Get("snap_target_edges", true);
			m_snapTargetLayerBounds = Preferences.Default.Get("snap_target_layer_bounds", true);
			m_channelViewMode = -1;
			m_channelVisible = new bool[] { true, true, true, true };
			m_navigatorPanelVisible = true;
			m_swatchesPanelVisible = true;
			m_layersPanelVisible = true;
			m_infoPanelVisible = true;
		}

		public bool RulersEnabled()
		{
			return m_rulersEnabled;
		}

		public void SetRulersEnabled(bool value)
		{
			m_rulersEnabled = value;
			Preferences.Default.Set("rulers_enabled", value);
		}

		public bool GridEnabled()
		{
			return m_gridEnabled;
		}

		public void SetGridEnabled(bool value)
		{
			m_gridEnabled = value;
			Preferences.Default.Set("grid_enabled", value);
		}

		public bool SnapEnabled()
		{
			return m_snapEnabled;
		}

		public void SetSnapEnabled(bool value)
		{
			m_snapEnabled = value;
			Preferences.Default.Set("snap_enabled", value);
		}

		public bool SnapTargetGuides()
		{
			return m_snapTargetGuides;
		}

		public void SetSnapTargetGuides(bool value)
		{
			m_snapTargetGuides = value;
			Preferences.Default.Set("snap_target_guides", value);
		}

		public bool SnapTargetGrid()
		{
			return m_snapTargetGrid;
		}

		public void SetSnapTargetGrid(bool value)
		{
			m_snapTargetGrid = value;
			Preferences.Default.Set("snap_target_grid", value);
		}

		public bool SnapTargetEdges()
		{
			return m_snapTargetEdges;
		}

		public void SetSnapTargetEdges(bool value)
		{
			m_snapTargetEdges = value;
			Preferences.Default.Set("snap_target_edges", value);
		}

		public bool SnapTargetLayerBounds()
		{
			return m_snapTargetLayerBounds;
		}

		public void SetSnapTargetLayerBounds(bool value)
		{
			m_snapTargetLayerBounds = value;
			Preferences.Default.Set("snap_target_layer_bounds", value);
		}

		public int ChannelViewMode()
		{
			return m_channelViewMode;
		}

		public void SetChannelViewMode(int mode)
		{
			m_channelViewMode = mode;
		}

		public bool ChannelVisible(int channel)
		{
			if (channel < 0 || channel > 3)
			{
				return true;
			}
			return m_channelVisible[channel];
		}

		public void SetChannelVisible(int channel, bool value)
		{
			if (channel < 0 || channel > 3)
			{
				return;
			}
			m_channelVisible[channel] = value;
		}

		public bool AllChannelsVisible()
		{
			for (int index = 0; index < 4; index++)
			{
				if (!m_channelVisible[index])
				{
					return false;
				}
			}
			return true;
		}

		public bool RgbChannelsVisible()
		{
			return m_channelVisible[0] && m_channelVisible[1] && m_channelVisible[2];
		}

		public bool PatternPreview()
		{
			return m_patternPreview;
		}

		public void SetPatternPreview(bool value)
		{
			m_patternPreview = value;
		}

		public bool PanelVisible(ePanelId panel)
		{
			if (panel == ePanelId.Navigator)
			{
				return m_navigatorPanelVisible;
			}
			if (panel == ePanelId.Swatches)
			{
				return m_swatchesPanelVisible;
			}
			if (panel == ePanelId.Layers)
			{
				return m_layersPanelVisible;
			}
			return m_infoPanelVisible;
		}

		public void SetPanelVisible(ePanelId panel, bool value)
		{
			if (panel == ePanelId.Navigator)
			{
				m_navigatorPanelVisible = value;
			}
			if (panel == ePanelId.Swatches)
			{
				m_swatchesPanelVisible = value;
			}
			if (panel == ePanelId.Layers)
			{
				m_layersPanelVisible = value;
			}
			if (panel == ePanelId.Info)
			{
				m_infoPanelVisible = value;
			}
		}
	}
}
