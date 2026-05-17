using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UL_project
{
    internal sealed class EquipmentFindWindow : Window
    {
        private readonly IReadOnlyList<EquipmentSearchResult> _searchIndex;
        private readonly TextBox _queryTextBox;
        private readonly StackPanel _resultsPanel;
        private readonly TextBlock _statusText;

        public EquipmentSearchResult? SelectedResult { get; private set; }

        public EquipmentFindWindow(IReadOnlyList<EquipmentSearchResult> searchIndex)
        {
            _searchIndex = searchIndex;

            Title = "Find Equipment";
            Width = 560;
            Height = 520;
            MinWidth = 480;
            MinHeight = 420;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White;

            var root = new Grid
            {
                Margin = new Thickness(16)
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            root.Children.Add(new TextBlock
            {
                Text = "Find Equipment",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24313F"))
            });

            var searchPanel = new DockPanel
            {
                Margin = new Thickness(0, 14, 0, 0)
            };
            Grid.SetRow(searchPanel, 1);

            var searchButton = new Button
            {
                Width = 88,
                Height = 34,
                Content = "Search",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24313F")),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent
            };
            searchButton.Click += (_, _) => RenderSearchResults();
            DockPanel.SetDock(searchButton, Dock.Right);

            _queryTextBox = new TextBox
            {
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0)
            };
            _queryTextBox.KeyDown += QueryTextBox_KeyDown;

            searchPanel.Children.Add(searchButton);
            searchPanel.Children.Add(_queryTextBox);
            root.Children.Add(searchPanel);

            _statusText = new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 12),
                Foreground = Brushes.DimGray,
                Text = "Type a name, UL Number, or Global Number."
            };
            Grid.SetRow(_statusText, 2);
            root.Children.Add(_statusText);

            _resultsPanel = new StackPanel();

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Content = _resultsPanel
            };
            Grid.SetRow(scrollViewer, 3);
            root.Children.Add(scrollViewer);

            Content = root;
            Loaded += (_, _) => _queryTextBox.Focus();
        }

        private void QueryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            RenderSearchResults();
            e.Handled = true;
        }

        private void RenderSearchResults()
        {
            _resultsPanel.Children.Clear();

            var query = _queryTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                _statusText.Text = "Enter a value to search.";
                return;
            }

            var matches = _searchIndex
                .Where(result => result.IsMatch(query))
                .OrderBy(result => result.MapIndex)
                .ThenBy(result => result.SeatName)
                .ThenBy(result => result.EquipmentName)
                .ToList();

            _statusText.Text = matches.Count == 0
                ? "No matching equipment was found."
                : $"{matches.Count} equipment item(s) found. Click a result to open its seat.";

            foreach (var match in matches)
            {
                _resultsPanel.Children.Add(BuildResultButton(match));
            }
        }

        private Button BuildResultButton(EquipmentSearchResult result)
        {
            var button = new Button
            {
                Tag = result,
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(12),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F8F7F2")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D7DCE8")),
                BorderThickness = new Thickness(1)
            };
            button.Click += ResultButton_Click;

            var content = new StackPanel();
            content.Children.Add(new TextBlock
            {
                Text = result.EquipmentName,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24313F"))
            });
            content.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 4, 0, 0),
                Text = $"Seat: {result.SeatName} | Map {result.MapIndex + 1}",
                Foreground = Brushes.DimGray
            });
            content.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 2, 0, 0),
                Text = $"UL: {result.UlNumber} | Global: {result.GlobalNumber}",
                Foreground = Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap
            });

            button.Content = content;
            return button;
        }

        private void ResultButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not EquipmentSearchResult result)
            {
                return;
            }

            SelectedResult = result;
            DialogResult = true;
            Close();
        }
    }

    internal sealed class EquipmentSearchResult
    {
        public EquipmentSearchResult(int mapIndex, Border seat, SeatInfo seatInfo, EquipmentInfo equipment)
        {
            MapIndex = mapIndex;
            Seat = seat;
            SeatName = string.IsNullOrWhiteSpace(seatInfo.SeatName) ? "Unnamed Seat" : seatInfo.SeatName;
            EquipmentName = string.IsNullOrWhiteSpace(equipment.Name) ? "(No Name)" : equipment.Name;
            UlNumber = equipment.UlNumber;
            GlobalNumber = equipment.GlobalNumber;
        }

        public int MapIndex { get; }

        public Border Seat { get; }

        public string SeatName { get; }

        public string EquipmentName { get; }

        public string UlNumber { get; }

        public string GlobalNumber { get; }

        public bool IsMatch(string query)
        {
            return EquipmentName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(UlNumber, query, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(GlobalNumber, query, StringComparison.OrdinalIgnoreCase);
        }
    }
}
