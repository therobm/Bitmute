using System.Collections.Generic;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Layouts;

namespace Bitmute.UI
{
	public class MenuBar
	{
		public const double MenuItemHeight = 18.0;
		public const double DropdownWidth = 190.0;
		public const double MenuSeparatorHeight = 9.0;

		private MainView m_main;
		private AbsoluteLayout m_overlay;
		private string[] m_titles;
		private Border[] m_menuButtons;
		private List<Border> m_openItemButtons;
		private List<MenuBarItem> m_openItems;
		private int m_openMenuIndex;
		private double m_openDropdownX;
		private List<Border> m_submenuParentRows;
		private List<MenuBarItem> m_submenuParentItems;
		private List<Border> m_submenuChildRows;
		private Border m_submenuBorder;
		private View m_root;

		public static Border BuildMenuSeparator()
		{
			Border line = new Border();
			line.HeightRequest = 1.0;
			line.StrokeThickness = 0.0;
			line.HorizontalOptions = LayoutOptions.Fill;
			line.VerticalOptions = LayoutOptions.Center;
			line.ThemeBg(UiConstants.DividerLight, UiConstants.DividerDark);

			Border separatorRow = new Border();
			separatorRow.HeightRequest = MenuSeparatorHeight;
			separatorRow.Padding = new Thickness(8.0, 4.0, 8.0, 4.0);
			separatorRow.StrokeThickness = 0.0;
			separatorRow.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			separatorRow.Content = line;
			return separatorRow;
		}

		private static double MenuListHeight(List<MenuBarItem> items)
		{
			double total = 8.0;
			for (int index = 0; index < items.Count; index++)
			{
				if (items[index].m_separator)
				{
					total = total + MenuSeparatorHeight;
				}
				else
				{
					total = total + MenuItemHeight;
				}
			}
			return total;
		}

		private Border BuildMenuButton(int index)
		{
			Label label = new Label();
			label.Text = m_titles[index];
			label.FontSize = UiConstants.PanelFontSize;
			label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			label.VerticalOptions = LayoutOptions.Center;

			Border button = new Border();
			button.Padding = new Thickness(10.0, 0.0, 10.0, 0.0);
			button.ThemeBg(UiConstants.ChromeMenubarLight, UiConstants.ChromeMenubarDark);
			button.StrokeThickness = 0.0;
			button.Content = label;

			TapGestureRecognizer tap = new TapGestureRecognizer();
			tap.Tapped += OnMenuButtonTapped;
			button.GestureRecognizers.Add(tap);

			PointerGestureRecognizer pointer = new PointerGestureRecognizer();
			pointer.PointerEntered += OnMenuButtonPointerEntered;
			pointer.PointerExited += OnMenuButtonPointerExited;
			button.GestureRecognizers.Add(pointer);

			return button;
		}

		private int FindMenuButtonIndex(object sender)
		{
			for (int index = 0; index < m_menuButtons.Length; index++)
			{
				if (ReferenceEquals(m_menuButtons[index], sender))
				{
					return index;
				}
			}
			return -1;
		}

		private void OnMenuButtonPointerEntered(object sender, PointerEventArgs eventArgs)
		{
			int index = FindMenuButtonIndex(sender);
			if (index < 0)
			{
				return;
			}
			if (m_openMenuIndex < 0)
			{
				m_menuButtons[index].ThemeBg(UiConstants.MenuHoverLight, UiConstants.MenuHoverDark);
				return;
			}
			if (index != m_openMenuIndex)
			{
				OpenMenu(index);
			}
		}

		private void OnMenuButtonPointerExited(object sender, PointerEventArgs eventArgs)
		{
			int index = FindMenuButtonIndex(sender);
			if (index < 0)
			{
				return;
			}
			if (index == m_openMenuIndex)
			{
				return;
			}
			m_menuButtons[index].ThemeBg(UiConstants.ChromeMenubarLight, UiConstants.ChromeMenubarDark);
		}

		private void OnMenuButtonTapped(object sender, TappedEventArgs eventArgs)
		{
			int index = FindMenuButtonIndex(sender);
			if (index < 0)
			{
				return;
			}
			if (m_openMenuIndex == index)
			{
				CloseOpenMenu();
				return;
			}
			OpenMenu(index);
		}

		private void OpenMenu(int index)
		{
			m_main.ClosePulldown();
			CloseOpenMenu();
			m_openMenuIndex = index;
			m_menuButtons[index].ThemeBg(UiConstants.MenuOpenLight, UiConstants.MenuOpenDark);
			m_submenuParentRows.Clear();
			m_submenuParentItems.Clear();
			m_submenuBorder = null;

			string title = m_titles[index];
			List<MenuBarItem> items = m_main.GetMenuItems(title);

			double dropdownX = m_menuButtons[index].Bounds.X;
			double overlayWidth = m_overlay.Width;
			if (overlayWidth > 0.0 && dropdownX + DropdownWidth > overlayWidth)
			{
				dropdownX = overlayWidth - DropdownWidth;
			}
			if (dropdownX < 0.0)
			{
				dropdownX = 0.0;
			}
			m_openDropdownX = dropdownX;

			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 0.0;
			list.Padding = new Thickness(0.0, 4.0, 0.0, 4.0);

			for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
			{
				list.Add(BuildMenuItem(items[itemIndex]));
			}

			Border dropdown = new Border();
			dropdown.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			dropdown.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			dropdown.StrokeThickness = 1.0;
			dropdown.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			dropdown.Content = list;

			BoxView catcher = new BoxView();
			catcher.Color = Colors.Transparent;
			TapGestureRecognizer catcherTap = new TapGestureRecognizer();
			catcherTap.Tapped += OnCatcherTapped;
			catcher.GestureRecognizers.Add(catcherTap);
			AbsoluteLayout.SetLayoutFlags(catcher, AbsoluteLayoutFlags.WidthProportional | AbsoluteLayoutFlags.HeightProportional);
			AbsoluteLayout.SetLayoutBounds(catcher, new Rect(0.0, UiConstants.MenuBarHeight, 1.0, 1.0));
			m_overlay.Add(catcher);

			double dropdownHeight = MenuListHeight(items);
			AbsoluteLayout.SetLayoutFlags(dropdown, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(dropdown, new Rect(dropdownX, UiConstants.MenuBarHeight, DropdownWidth, dropdownHeight));
			m_overlay.Add(dropdown);
		}

		private void OpenSubmenu(MenuBarItem parentItem, Border parentRow)
		{
			CloseSubmenu();
			List<MenuBarItem> children = m_main.GetSubmenuItems(parentItem.m_action);
			if (children.Count == 0)
			{
				return;
			}
			VerticalStackLayout list = new VerticalStackLayout();
			list.Spacing = 0.0;
			list.Padding = new Thickness(0.0, 4.0, 0.0, 4.0);
			m_submenuChildRows.Clear();
			for (int index = 0; index < children.Count; index++)
			{
				Border childRow = BuildMenuItem(children[index]);
				m_submenuChildRows.Add(childRow);
				list.Add(childRow);
			}
			Border submenu = new Border();
			submenu.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			submenu.ThemeStroke(UiConstants.DividerLight, UiConstants.DividerDark);
			submenu.StrokeThickness = 1.0;
			submenu.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(3.0) };
			submenu.Content = list;
			double submenuX = m_openDropdownX + DropdownWidth - 2.0;
			double overlayWidth = m_overlay.Width;
			if (overlayWidth > 0.0 && submenuX + DropdownWidth > overlayWidth)
			{
				submenuX = m_openDropdownX - DropdownWidth + 2.0;
			}
			if (submenuX < 0.0)
			{
				submenuX = 0.0;
			}
			double submenuY = UiConstants.MenuBarHeight + parentRow.Y;
			double submenuHeight = MenuListHeight(children);
			AbsoluteLayout.SetLayoutFlags(submenu, AbsoluteLayoutFlags.None);
			AbsoluteLayout.SetLayoutBounds(submenu, new Rect(submenuX, submenuY, DropdownWidth, submenuHeight));
			m_overlay.Add(submenu);
			m_submenuBorder = submenu;
		}

		private void CloseSubmenu()
		{
			if (m_submenuBorder != null)
			{
				m_overlay.Remove(m_submenuBorder);
				m_submenuBorder = null;
			}
		}

		private void OnSubmenuParentEntered(object sender, PointerEventArgs eventArgs)
		{
			Border row = sender as Border;
			if (row == null)
			{
				return;
			}
			row.ThemeBg(UiConstants.AccentLight, UiConstants.AccentDark);
			for (int index = 0; index < m_submenuParentRows.Count; index++)
			{
				if (ReferenceEquals(m_submenuParentRows[index], sender))
				{
					OpenSubmenu(m_submenuParentItems[index], m_submenuParentRows[index]);
					return;
				}
			}
		}

		private void OnSubmenuParentTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_submenuParentRows.Count; index++)
			{
				if (ReferenceEquals(m_submenuParentRows[index], sender))
				{
					OpenSubmenu(m_submenuParentItems[index], m_submenuParentRows[index]);
					return;
				}
			}
		}

		private Border BuildMenuItem(MenuBarItem item)
		{
			if (item.m_separator)
			{
				return BuildMenuSeparator();
			}
			bool enabled = item.m_enabled;
			bool submenu = item.m_submenu;

			string accelerator = item.m_accelerator;
			if (submenu)
			{
				accelerator = "▸";
			}

			string text = item.m_label;
			if (item.m_checked)
			{
				text = "✓ " + item.m_label;
			}

			Grid rowContent = new Grid();
			rowContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			rowContent.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

			Label label = new Label();
			label.Text = text;
			label.FontSize = UiConstants.PanelFontSize;
			label.VerticalOptions = LayoutOptions.Center;
			label.LineBreakMode = LineBreakMode.TailTruncation;
			label.MaxLines = 1;
			if (enabled)
			{
				label.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
			}
			else
			{
				label.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
			}
			Grid.SetColumn(label, 0);
			rowContent.Add(label);

			if (accelerator.Length > 0)
			{
				Label accelLabel = new Label();
				accelLabel.Text = accelerator;
				accelLabel.FontSize = UiConstants.PanelFontSize;
				accelLabel.VerticalOptions = LayoutOptions.Center;
				if (enabled)
				{
					accelLabel.ThemeText(UiConstants.OnSurfaceLight, UiConstants.OnSurfaceDark);
				}
				else
				{
					accelLabel.ThemeText(UiConstants.TextDimLight, UiConstants.TextDimDark);
				}
				Grid.SetColumn(accelLabel, 1);
				rowContent.Add(accelLabel);
			}

			Border row = new Border();
			row.HeightRequest = MenuItemHeight;
			row.Padding = new Thickness(12.0, 0.0, 12.0, 0.0);
			row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			row.StrokeThickness = 0.0;
			row.Content = rowContent;

			if (submenu)
			{
				TapGestureRecognizer submenuTap = new TapGestureRecognizer();
				submenuTap.Tapped += OnSubmenuParentTapped;
				row.GestureRecognizers.Add(submenuTap);
				PointerGestureRecognizer submenuPointer = new PointerGestureRecognizer();
				submenuPointer.PointerEntered += OnSubmenuParentEntered;
				submenuPointer.PointerExited += OnMenuItemPointerExited;
				row.GestureRecognizers.Add(submenuPointer);
				m_submenuParentRows.Add(row);
				m_submenuParentItems.Add(item);
			}
			else if (enabled)
			{
				TapGestureRecognizer tap = new TapGestureRecognizer();
				tap.Tapped += OnMenuItemTapped;
				row.GestureRecognizers.Add(tap);
				PointerGestureRecognizer pointer = new PointerGestureRecognizer();
				pointer.PointerEntered += OnMenuItemPointerEntered;
				pointer.PointerExited += OnMenuItemPointerExited;
				row.GestureRecognizers.Add(pointer);
				m_openItemButtons.Add(row);
				m_openItems.Add(item);
			}

			return row;
		}

		private void OnMenuItemPointerEntered(object sender, PointerEventArgs eventArgs)
		{
			Border row = sender as Border;
			if (row != null)
			{
				row.ThemeBg(UiConstants.AccentLight, UiConstants.AccentDark);
			}
			bool isSubmenuChild = false;
			for (int index = 0; index < m_submenuChildRows.Count; index++)
			{
				if (ReferenceEquals(m_submenuChildRows[index], sender))
				{
					isSubmenuChild = true;
					break;
				}
			}
			if (!isSubmenuChild)
			{
				CloseSubmenu();
			}
		}

		private void OnMenuItemPointerExited(object sender, PointerEventArgs eventArgs)
		{
			Border row = sender as Border;
			if (row != null)
			{
				row.ThemeBg(UiConstants.PanelSurfaceLight, UiConstants.PanelSurfaceDark);
			}
		}

		private void OnMenuItemTapped(object sender, TappedEventArgs eventArgs)
		{
			for (int index = 0; index < m_openItemButtons.Count; index++)
			{
				if (ReferenceEquals(m_openItemButtons[index], sender))
				{
					MenuBarItem item = m_openItems[index];
					CloseOpenMenu();
					m_main.InvokeMenuAction(item);
					return;
				}
			}
		}

		private void OnCatcherTapped(object sender, TappedEventArgs eventArgs)
		{
			CloseOpenMenu();
		}

		public MenuBar(MainView main, string[] titles, AbsoluteLayout overlay)
		{
			m_main = main;
			m_titles = titles;
			m_overlay = overlay;
			m_menuButtons = new Border[titles.Length];
			m_openItemButtons = new List<Border>();
			m_openItems = new List<MenuBarItem>();
			m_openMenuIndex = -1;
			m_openDropdownX = 0.0;
			m_submenuParentRows = new List<Border>();
			m_submenuParentItems = new List<MenuBarItem>();
			m_submenuChildRows = new List<Border>();
			m_submenuBorder = null;

			HorizontalStackLayout strip = new HorizontalStackLayout();
			strip.HeightRequest = UiConstants.MenuBarHeight;
			strip.ThemeBg(UiConstants.ChromeMenubarLight, UiConstants.ChromeMenubarDark);
			strip.Spacing = 0.0;
			strip.Padding = new Thickness(0.0);

			for (int index = 0; index < titles.Length; index++)
			{
				Border button = BuildMenuButton(index);
				m_menuButtons[index] = button;
				strip.Add(button);
			}

			m_root = strip;
		}

		public View Root()
		{
			return m_root;
		}

		public void CloseOpenMenu()
		{
			m_overlay.Clear();
			m_openItemButtons.Clear();
			m_openItems.Clear();
			m_submenuParentRows.Clear();
			m_submenuParentItems.Clear();
			m_submenuChildRows.Clear();
			m_submenuBorder = null;
			if (m_openMenuIndex >= 0)
			{
				m_menuButtons[m_openMenuIndex].ThemeBg(UiConstants.ChromeMenubarLight, UiConstants.ChromeMenubarDark);
			}
			m_openMenuIndex = -1;
		}
	}
}
