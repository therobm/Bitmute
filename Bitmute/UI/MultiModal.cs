using System;
using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace Bitmute.UI
{
	public abstract class MultiModal : ModalDialog
	{
		private List<View> m_panels;
		private List<Label> m_labels;
		private List<CheckBox> m_checks;
		private List<Action<bool>> m_checkedHandlers;
		private VerticalStackLayout m_sectionList;
		private ContentView m_panelHost;

		private void EnsureSections()
		{
			if (m_panels == null)
			{
				m_panels = new List<View>();
				m_labels = new List<Label>();
				m_checks = new List<CheckBox>();
				m_checkedHandlers = new List<Action<bool>>();
				m_sectionList = new VerticalStackLayout();
				m_sectionList.Spacing = UiConstants.DialogRowSpacing;
				m_panelHost = new ContentView();
			}
		}

		private void OnSectionTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_labels.Count; index++)
			{
				if (ReferenceEquals(m_labels[index], sender))
				{
					SelectSection(index);
					return;
				}
			}
		}

		private void OnSectionChecked(object sender, CheckedChangedEventArgs eventArgs)
		{
			for (int index = 0; index < m_checks.Count; index++)
			{
				if (ReferenceEquals(m_checks[index], sender))
				{
					Action<bool> handler = m_checkedHandlers[index];
					if (handler != null)
					{
						handler(eventArgs.Value);
					}
					return;
				}
			}
		}

		private void AddSectionRow(string name, View panel, CheckBox check, Action<bool> onChecked)
		{
			EnsureSections();

			Label nameLabel = new Label();
			nameLabel.Text = name;
			nameLabel.FontSize = UiConstants.PanelFontSize;
			nameLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			nameLabel.VerticalOptions = LayoutOptions.Center;
			TapGestureRecognizer recognizer = new TapGestureRecognizer();
			recognizer.Tapped += OnSectionTapped;
			nameLabel.GestureRecognizers.Add(recognizer);

			HorizontalStackLayout row = new HorizontalStackLayout();
			row.Spacing = UiConstants.DialogRowSpacing;
			if (check != null)
			{
				check.VerticalOptions = LayoutOptions.Center;
				check.CheckedChanged += OnSectionChecked;
				row.Add(check);
			}
			row.Add(nameLabel);

			m_panels.Add(panel);
			m_labels.Add(nameLabel);
			m_checks.Add(check);
			m_checkedHandlers.Add(onChecked);
			m_sectionList.Add(row);
		}

		protected void AddSection(string name, View panel)
		{
			AddSectionRow(name, panel, null, null);
		}

		protected void AddSection(string name, View panel, bool initialChecked, Action<bool> onChecked)
		{
			CheckBox check = new CheckBox();
			check.IsChecked = initialChecked;
			check.SetAppThemeColor(CheckBox.ColorProperty, UiConstants.AccentLight, UiConstants.AccentDark);
			AddSectionRow(name, panel, check, onChecked);
		}

		protected void SelectSection(int index)
		{
			if (index < 0 || index >= m_panels.Count)
			{
				return;
			}
			m_panelHost.Content = m_panels[index];
		}

		protected void ComposeSections(string title, View buttonRow, double listWidth, double panelWidth)
		{
			EnsureSections();
			m_sectionList.WidthRequest = listWidth;
			m_panelHost.WidthRequest = panelWidth;

			HorizontalStackLayout body = new HorizontalStackLayout();
			body.Spacing = UiConstants.DialogRowSpacing * 2.0;
			body.Add(m_sectionList);
			body.Add(m_panelHost);
			ComposeDialog(title, body, buttonRow);
		}
	}
}
