using System;
using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Bitmute.UI.Components;

namespace Bitmute.UI
{
	public abstract class MultiModal : ModalDialog
	{
		private sealed class SectionCheck
		{
			public MultiModal m_owner;
			public int m_index;

			public void Changed(bool value)
			{
				m_owner.OnSectionChecked(m_index, value);
			}
		}

		private List<View> m_panels;
		private List<Border> m_rows;
		private List<Action<bool>> m_checkedHandlers;
		private VerticalStackLayout m_sectionList;
		private ContentView m_panelHost;
		private int m_selectedSection;

		private void EnsureSections()
		{
			if (m_panels == null)
			{
				m_panels = new List<View>();
				m_rows = new List<Border>();
				m_checkedHandlers = new List<Action<bool>>();
				m_sectionList = new VerticalStackLayout();
				m_sectionList.Spacing = 2.0;
				m_panelHost = new ContentView();
				m_selectedSection = -1;
			}
		}

		private void OnSectionChecked(int index, bool value)
		{
			Action<bool> handler = m_checkedHandlers[index];
			if (handler != null)
			{
				handler(value);
			}
		}

		private void OnSectionTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_rows.Count; index++)
			{
				if (ReferenceEquals(m_rows[index], sender))
				{
					SelectSection(index);
					return;
				}
			}
		}

		private void AddSectionRow(string name, View panel, CheckMark check, Action<bool> onChecked)
		{
			EnsureSections();

			Label nameLabel = new Label();
			nameLabel.Text = name;
			nameLabel.FontSize = UiConstants.PanelFontSize;
			nameLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			nameLabel.VerticalOptions = LayoutOptions.Center;

			HorizontalStackLayout rowContent = new HorizontalStackLayout();
			rowContent.Spacing = UiConstants.DialogRowSpacing;
			if (check != null)
			{
				check.VerticalOptions = LayoutOptions.Center;
				rowContent.Add(check);
			}
			rowContent.Add(nameLabel);

			Border row = new Border();
			row.StrokeThickness = 0.0;
			row.Padding = new Thickness(UiConstants.DialogRowSpacing, 4.0);
			row.SetAppThemeColor(BackgroundColorProperty, Colors.Transparent, Colors.Transparent);
			row.Content = rowContent;
			TapGestureRecognizer recognizer = new TapGestureRecognizer();
			recognizer.Tapped += OnSectionTapped;
			row.GestureRecognizers.Add(recognizer);

			m_panels.Add(panel);
			m_rows.Add(row);
			m_checkedHandlers.Add(onChecked);
			m_sectionList.Add(row);
		}

		protected void AddSection(string name, View panel)
		{
			AddSectionRow(name, panel, null, null);
		}

		protected void AddSection(string name, View panel, bool initialChecked, Action<bool> onChecked)
		{
			EnsureSections();
			SectionCheck adapter = new SectionCheck();
			adapter.m_owner = this;
			adapter.m_index = m_panels.Count;
			CheckMark check = new CheckMark(initialChecked, adapter.Changed);
			AddSectionRow(name, panel, check, onChecked);
		}

		protected void SelectSection(int index)
		{
			if (index < 0 || index >= m_panels.Count)
			{
				return;
			}
			if (m_selectedSection >= 0 && m_selectedSection < m_rows.Count)
			{
				m_rows[m_selectedSection].SetAppThemeColor(BackgroundColorProperty, Colors.Transparent, Colors.Transparent);
			}
			m_selectedSection = index;
			m_rows[index].SetAppThemeColor(BackgroundColorProperty, UiConstants.MenuOpenLight, UiConstants.MenuOpenDark);
			m_panelHost.Content = m_panels[index];
		}

		protected void ComposeSections(string title, View buttonRow, double listWidth, double panelWidth)
		{
			EnsureSections();
			m_sectionList.WidthRequest = listWidth;
			m_panelHost.WidthRequest = panelWidth;

			BoxView divider = new BoxView();
			divider.WidthRequest = UiConstants.PanelBorderThickness;
			divider.ThemeBg(UiConstants.DividerLight, UiConstants.DividerDark);
			divider.VerticalOptions = LayoutOptions.Fill;

			Grid body = new Grid();
			body.ColumnSpacing = UiConstants.DialogRowSpacing;
			body.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			body.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			body.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			Grid.SetColumn(m_sectionList, 0);
			Grid.SetColumn(divider, 1);
			Grid.SetColumn(m_panelHost, 2);
			body.Add(m_sectionList);
			body.Add(divider);
			body.Add(m_panelHost);
			ComposeDialog(title, body, buttonRow);
		}
	}
}
