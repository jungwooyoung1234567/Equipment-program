using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace UL_project
{
    internal sealed class SeatSizeWindow : Window
    {
        private readonly TextBox _widthTextBox;
        private readonly TextBox _heightTextBox;

        public SeatSizeWindow(double currentWidth, double currentHeight)
        {
            Title = "Resize Item";
            Width = 320;
            Height = 210;
            MinWidth = 320;
            MinHeight = 210;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var root = new Grid
            {
                Margin = new Thickness(18)
            };

            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var formGrid = new Grid();
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var widthLabel = new TextBlock
            {
                Text = "Width",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 8)
            };
            Grid.SetRow(widthLabel, 0);
            Grid.SetColumn(widthLabel, 0);
            formGrid.Children.Add(widthLabel);

            _widthTextBox = new TextBox
            {
                Text = currentWidth.ToString("0.##", CultureInfo.InvariantCulture),
                Margin = new Thickness(0, 0, 0, 8)
            };
            Grid.SetRow(_widthTextBox, 0);
            Grid.SetColumn(_widthTextBox, 1);
            formGrid.Children.Add(_widthTextBox);

            var heightLabel = new TextBlock
            {
                Text = "Height",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetRow(heightLabel, 1);
            Grid.SetColumn(heightLabel, 0);
            formGrid.Children.Add(heightLabel);

            _heightTextBox = new TextBox
            {
                Text = currentHeight.ToString("0.##", CultureInfo.InvariantCulture)
            };
            Grid.SetRow(_heightTextBox, 1);
            Grid.SetColumn(_heightTextBox, 1);
            formGrid.Children.Add(_heightTextBox);

            Grid.SetRow(formGrid, 0);
            root.Children.Add(formGrid);

            var hintText = new TextBlock
            {
                Text = "Enter visible width and height in pixels.",
                Margin = new Thickness(0, 12, 0, 18)
            };
            Grid.SetRow(hintText, 1);
            root.Children.Add(hintText);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Margin = new Thickness(0, 0, 8, 0),
                IsDefault = true
            };
            okButton.Click += OkButton_Click;
            buttonPanel.Children.Add(okButton);

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                IsCancel = true
            };
            buttonPanel.Children.Add(cancelButton);

            Grid.SetRow(buttonPanel, 2);
            root.Children.Add(buttonPanel);

            Content = root;
        }

        public double ItemWidth { get; private set; }

        public double ItemHeight { get; private set; }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseDimension(_widthTextBox.Text, out var width) ||
                !TryParseDimension(_heightTextBox.Text, out var height))
            {
                MessageBox.Show(
                    "Enter valid numbers greater than 0.",
                    "Resize Item",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ItemWidth = width;
            ItemHeight = height;
            DialogResult = true;
        }

        private static bool TryParseDimension(string text, out double value)
        {
            var trimmed = text.Trim();

            if (double.TryParse(trimmed, NumberStyles.Float, CultureInfo.CurrentCulture, out value) ||
                double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return value > 0;
            }

            value = 0;
            return false;
        }
    }
}
