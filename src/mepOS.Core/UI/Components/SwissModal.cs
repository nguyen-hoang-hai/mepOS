using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace mepOS.Core.UI.Components
{
    public static class SwissModal
    {
        public static void ShowInfo(string message, string title = "mepOS // THÔNG BÁO", IntPtr? ownerHandle = null)
        {
            ShowDialogInternal(message, title, "INFO", "#3B82F6", "#EFF6FF", false, ownerHandle);
        }

        public static void ShowSuccess(string message, string title = "mepOS // THÀNH CÔNG", IntPtr? ownerHandle = null)
        {
            ShowDialogInternal(message, title, "SUCCESS", "#10B981", "#ECFDF5", false, ownerHandle);
        }

        public static void ShowWarning(string message, string title = "mepOS // CHÚ Ý", IntPtr? ownerHandle = null)
        {
            ShowDialogInternal(message, title, "WARNING", "#D97706", "#FFFBEB", false, ownerHandle);
        }

        public static void ShowError(string message, string title = "mepOS // LỖI", IntPtr? ownerHandle = null)
        {
            ShowDialogInternal(message, title, "ERROR", "#DC2626", "#FEF2F2", false, ownerHandle);
        }

        public static bool ShowConfirm(string message, string title = "mepOS // XÁC NHẬN", IntPtr? ownerHandle = null)
        {
            return ShowDialogInternal(message, title, "CONFIRM", "#0A0A0A", "#FFDD00", true, ownerHandle);
        }

        private static bool ShowDialogInternal(string message, string title, string statusType, string accentColorHex, string accentBgHex, bool isConfirm, IntPtr? ownerHandle)
        {
            bool result = false;

            var win = new Window
            {
                Title = title,
                Width = 460,
                SizeToContent = SizeToContent.Height,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                FontFamily = new FontFamily("Segoe UI")
            };

            if (ownerHandle.HasValue && ownerHandle.Value != IntPtr.Zero)
            {
                new WindowInteropHelper(win).Owner = ownerHandle.Value;
            }

            // Outer drop shadow wrapper
            var outerWrapper = new Border
            {
                Margin = new Thickness(12),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(4),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 270,
                    ShadowDepth = 4,
                    Opacity = 0.18,
                    BlurRadius = 12
                }
            };

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Body
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Footer

            // 1. Header Bar
            var header = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E4E4E7")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(14, 10, 10, 10),
                CornerRadius = new CornerRadius(4, 4, 0, 0)
            };

            var headerDock = new DockPanel();

            // Close button (X)
            var btnClose = new Button
            {
                Content = "✕",
                Width = 26,
                Height = 26,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#71717A")),
                Cursor = Cursors.Hand
            };
            btnClose.Click += (s, e) => { result = false; win.Close(); };
            DockPanel.SetDock(btnClose, Dock.Right);
            headerDock.Children.Add(btnClose);

            // Left brand + title
            var titlePanel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            
            var brandMep = new TextBlock
            {
                Text = "mep",
                FontWeight = FontWeights.ExtraBold,
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A"))
            };
            var brandOsBadge = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDD00")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(2),
                Padding = new Thickness(3, 0, 3, 0),
                Margin = new Thickness(1, 0, 8, 0)
            };
            brandOsBadge.Child = new TextBlock
            {
                Text = "OS",
                FontWeight = FontWeights.ExtraBold,
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A"))
            };

            var sep = new TextBlock
            {
                Text = "//",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#A1A1AA")),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var lblTitle = new TextBlock
            {
                Text = title.Replace("mepOS // ", ""),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#27272A")),
                VerticalAlignment = VerticalAlignment.Center
            };

            titlePanel.Children.Add(brandMep);
            titlePanel.Children.Add(brandOsBadge);
            titlePanel.Children.Add(sep);
            titlePanel.Children.Add(lblTitle);

            headerDock.Children.Add(titlePanel);
            header.Child = headerDock;
            Grid.SetRow(header, 0);
            rootGrid.Children.Add(header);

            // Drag support on header
            header.MouseDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed) win.DragMove();
            };

            // 2. Body Content
            var body = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF")),
                Padding = new Thickness(18, 16, 18, 16)
            };

            var bodyGrid = new Grid();
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Icon
            bodyGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Message

            // Status Icon Badge
            var iconBorder = new Border
            {
                Width = 36,
                Height = 36,
                CornerRadius = new CornerRadius(18),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentBgHex)),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColorHex)),
                BorderThickness = new Thickness(1.5),
                Margin = new Thickness(0, 0, 14, 0),
                VerticalAlignment = VerticalAlignment.Top
            };

            string iconChar = "!";
            if (statusType == "SUCCESS") iconChar = "✓";
            else if (statusType == "ERROR") iconChar = "✕";
            else if (statusType == "INFO") iconChar = "i";

            var iconText = new TextBlock
            {
                Text = iconChar,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(accentColorHex)),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            iconBorder.Child = iconText;
            Grid.SetColumn(iconBorder, 0);
            bodyGrid.Children.Add(iconBorder);

            // Message text block
            var msgPanel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var msgText = new TextBlock
            {
                Text = message,
                FontSize = 13,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#18181B")),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };
            msgPanel.Children.Add(msgText);
            Grid.SetColumn(msgPanel, 1);
            bodyGrid.Children.Add(msgPanel);

            body.Child = bodyGrid;
            Grid.SetRow(body, 1);
            rootGrid.Children.Add(body);

            // 3. Footer Bar
            var footer = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F8F6")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E4E4E7")),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(14, 10, 14, 10),
                CornerRadius = new CornerRadius(0, 0, 4, 4)
            };

            var btnPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            if (isConfirm)
            {
                var btnCancel = new Button
                {
                    Content = "Hủy bỏ",
                    Height = 32,
                    MinWidth = 80,
                    Margin = new Thickness(0, 0, 8, 0),
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                    BorderThickness = new Thickness(1.5),
                    FontWeight = FontWeights.SemiBold,
                    Cursor = Cursors.Hand
                };
                btnCancel.Click += (s, e) => { result = false; win.Close(); };
                btnPanel.Children.Add(btnCancel);
            }

            var btnOk = new Button
            {
                Content = isConfirm ? "Xác nhận" : "Đã hiểu",
                Height = 32,
                MinWidth = 84,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDD00")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                BorderThickness = new Thickness(1.5),
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0A0A0A")),
                Cursor = Cursors.Hand
            };
            btnOk.Click += (s, e) => { result = true; win.Close(); };
            btnPanel.Children.Add(btnOk);

            footer.Child = btnPanel;
            Grid.SetRow(footer, 2);
            rootGrid.Children.Add(footer);

            outerWrapper.Child = rootGrid;
            win.Content = outerWrapper;

            win.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) { result = false; win.Close(); }
                else if (e.Key == Key.Enter) { result = true; win.Close(); }
            };

            win.ShowDialog();
            return result;
        }
    }
}
