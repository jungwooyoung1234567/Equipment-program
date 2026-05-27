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
        // 이 대화상자는 미리 만들어 둔 장비 검색 인덱스를 화면에 보여준다.
        private readonly IReadOnlyList<EquipmentSearchResult> _searchIndex;
        private readonly TextBox _queryTextBox;
        private readonly ComboBox _searchFieldComboBox;
        private readonly StackPanel _resultsPanel;
        private readonly TextBlock _statusText;

        public EquipmentSearchResult? SelectedResult { get; private set; }

        // 검색창 UI를 만들고 전달받은 검색 인덱스를 보관한다.
        public EquipmentFindWindow(IReadOnlyList<EquipmentSearchResult> searchIndex)
        {
            _searchIndex = searchIndex;

            Title = "장비 검색";
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
                Text = "장비 검색",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24313F"))
            });

            var searchPanel = new Grid
            {
                Margin = new Thickness(0, 14, 0, 0)
            };
            searchPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            searchPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            searchPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetRow(searchPanel, 1);

            _searchFieldComboBox = new ComboBox
            {
                Width = 120,
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0),
                ItemsSource = Enum.GetValues(typeof(EquipmentSearchField))
            };
            _searchFieldComboBox.SelectedItem = EquipmentSearchField.Name;
            Grid.SetColumn(_searchFieldComboBox, 0);

            var searchButton = new Button
            {
                Width = 88,
                Height = 34,
                Content = "검색",
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24313F")),
                Foreground = Brushes.White,
                BorderBrush = Brushes.Transparent
            };
            searchButton.Click += (_, _) => RenderSearchResults();
            Grid.SetColumn(searchButton, 2);

            _queryTextBox = new TextBox
            {
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0)
            };
            _queryTextBox.KeyDown += QueryTextBox_KeyDown;
            Grid.SetColumn(_queryTextBox, 1);

            searchPanel.Children.Add(_searchFieldComboBox);
            searchPanel.Children.Add(searchButton);
            searchPanel.Children.Add(_queryTextBox);
            root.Children.Add(searchPanel);

            _statusText = new TextBlock
            {
                Margin = new Thickness(0, 10, 0, 12),
                Foreground = Brushes.DimGray,
                Text = "검색 기준을 선택하고 값을 입력하세요."
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

        // 엔터 키 입력 시 현재 검색어로 검색을 실행한다.
        private void QueryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
            {
                return;
            }

            RenderSearchResults();
            e.Handled = true;
        }

        // 검색어에 맞는 결과를 필터링해 결과 목록 패널을 다시 그린다.
        private void RenderSearchResults()
        {
            // 검색은 이미 만들어 둔 인덱스를 기준으로 클라이언트 쪽에서 수행한다.
            _resultsPanel.Children.Clear();

            var query = _queryTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(query))
            {
                _statusText.Text = "검색어를 입력하세요.";
                return;
            }

            var searchField = _searchFieldComboBox.SelectedItem is EquipmentSearchField selectedField
                ? selectedField
                : EquipmentSearchField.Name;

            var matches = _searchIndex
                .Select(result => new
                {
                    Result = result,
                    Score = result.GetMatchScore(query, searchField)
                })
                .Where(entry => entry.Score > 0)
                .OrderByDescending(entry => entry.Score)
                .ThenBy(entry => entry.Result.MapIndex)
                .ThenBy(entry => entry.Result.SeatName)
                .ThenBy(entry => entry.Result.EquipmentName)
                .Select(entry => entry.Result)
                .ToList();

            _statusText.Text = matches.Count == 0
                ? "일치하는 장비를 찾지 못했습니다."
                : $"{matches.Count}개의 장비를 찾았습니다. 가장 유사한 결과부터 표시합니다.";

            foreach (var match in matches)
            {
                _resultsPanel.Children.Add(BuildResultButton(match));
            }
        }

        // 단일 검색 결과를 클릭 가능한 버튼 형태의 UI로 만든다.
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
                Text = $"기구: {result.SeatName} | {result.MapName}",
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

        // 사용자가 클릭한 검색 결과를 선택 결과로 저장하고 창을 닫는다.
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

    internal enum EquipmentSearchField
    {
        Name,
        ULName,
        GlobalName
    }

    internal sealed class EquipmentSearchResult
    {
        // 검색 결과 하나를 만들고 표시용 문자열과 참조를 함께 보관한다.
        public EquipmentSearchResult(int mapIndex, string mapName, Border seat, SeatInfo seatInfo, EquipmentInfo equipment)
        {
            MapIndex = mapIndex;
            MapName = string.IsNullOrWhiteSpace(mapName) ? $"맵 {mapIndex + 1}" : mapName;
            Seat = seat;
            SeatName = string.IsNullOrWhiteSpace(seatInfo.SeatName) ? "이름 없는 기구" : seatInfo.SeatName;
            EquipmentName = string.IsNullOrWhiteSpace(equipment.Name) ? "(이름 없음)" : equipment.Name;
            UlNumber = equipment.UlNumber;
            GlobalNumber = equipment.GlobalNumber;
        }

        public int MapIndex { get; }

        public string MapName { get; }

        public Border Seat { get; }

        public string SeatName { get; }

        public string EquipmentName { get; }

        public string UlNumber { get; }

        public string GlobalNumber { get; }

        // 현재 검색어가 이 결과와 일치하는지 판단한다.
        public int GetMatchScore(string query, EquipmentSearchField searchField)
        {
            var candidate = searchField switch
            {
                EquipmentSearchField.Name => EquipmentName,
                EquipmentSearchField.ULName => UlNumber,
                EquipmentSearchField.GlobalName => GlobalNumber,
                _ => string.Empty
            };

            return CalculateMatchScore(candidate, query);
        }

        private static int CalculateMatchScore(string candidate, string query)
        {
            if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(query))
            {
                return 0;
            }

            var normalizedCandidate = candidate.Trim();
            var normalizedQuery = query.Trim();

            if (string.Equals(normalizedCandidate, normalizedQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 1000;
            }

            if (normalizedCandidate.StartsWith(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            {
                return 800 - Math.Max(0, normalizedCandidate.Length - normalizedQuery.Length);
            }

            var containsIndex = normalizedCandidate.IndexOf(normalizedQuery, StringComparison.OrdinalIgnoreCase);
            if (containsIndex >= 0)
            {
                return 650 - containsIndex;
            }

            if (IsSubsequenceMatch(normalizedCandidate, normalizedQuery))
            {
                return 450 - Math.Max(0, normalizedCandidate.Length - normalizedQuery.Length);
            }

            var distance = GetLevenshteinDistance(
                normalizedCandidate.ToUpperInvariant(),
                normalizedQuery.ToUpperInvariant());
            var threshold = Math.Max(2, normalizedQuery.Length / 2);
            if (distance > threshold)
            {
                return 0;
            }

            return 250 - (distance * 40) - Math.Abs(normalizedCandidate.Length - normalizedQuery.Length);
        }

        private static bool IsSubsequenceMatch(string candidate, string query)
        {
            var candidateIndex = 0;

            foreach (var queryCharacter in query)
            {
                var matched = false;
                while (candidateIndex < candidate.Length)
                {
                    if (char.ToUpperInvariant(candidate[candidateIndex]) == char.ToUpperInvariant(queryCharacter))
                    {
                        matched = true;
                        candidateIndex++;
                        break;
                    }

                    candidateIndex++;
                }

                if (!matched)
                {
                    return false;
                }
            }

            return true;
        }

        private static int GetLevenshteinDistance(string source, string target)
        {
            var distances = new int[source.Length + 1, target.Length + 1];

            for (var i = 0; i <= source.Length; i++)
            {
                distances[i, 0] = i;
            }

            for (var j = 0; j <= target.Length; j++)
            {
                distances[0, j] = j;
            }

            for (var i = 1; i <= source.Length; i++)
            {
                for (var j = 1; j <= target.Length; j++)
                {
                    var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                    distances[i, j] = Math.Min(
                        Math.Min(
                            distances[i - 1, j] + 1,
                            distances[i, j - 1] + 1),
                        distances[i - 1, j - 1] + cost);
                }
            }

            return distances[source.Length, target.Length];
        }
    }
}
