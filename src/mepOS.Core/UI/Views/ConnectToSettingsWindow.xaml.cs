using System;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using mepOS.Core.Engines;

namespace mepOS.Core.UI.Views
{
    public partial class ConnectToSettingsWindow : Window
    {
        private readonly ConnectToConfig _config;

        public ConnectToSettingsWindow(ConnectToConfig config)
        {
            InitializeComponent();
            _config = config ?? new ConnectToConfig();

            // Populate data
            txtMaxOffset.Text = _config.MaxAlignOffsetMm.ToString("F0", CultureInfo.InvariantCulture);
            chkAllowMove.IsChecked = _config.AllowAlignMove;

            // Wire events
            btnSave.Click += OnSave;
            btnCancel.Click += (s, e) => Close();

            btnPreset25.Click += (s, e) => txtMaxOffset.Text = "25";
            btnPreset50.Click += (s, e) => txtMaxOffset.Text = "50";
            btnPreset100.Click += (s, e) => txtMaxOffset.Text = "100";
            btnPreset200.Click += (s, e) => txtMaxOffset.Text = "200";

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape) Close();
            };

            MouseDown += (s, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed) DragMove();
            };
        }

        private void OnSave(object sender, RoutedEventArgs e)
        {
            double val;
            if (double.TryParse(txtMaxOffset.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out val))
            {
                if (val < 5.0) val = 5.0;
                if (val > 1000.0) val = 1000.0;
                _config.MaxAlignOffsetMm = val;
            }

            _config.AllowAlignMove = chkAllowMove.IsChecked == true;
            _config.Save();

            DialogResult = true;
            Close();
        }
    }
}
