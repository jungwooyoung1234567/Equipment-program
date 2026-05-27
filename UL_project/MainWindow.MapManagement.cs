using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UL_project
{
    public partial class MainWindow
    {
        // 앱은 최대 10개의 맵 캔버스를 가질 수 있다.
        private const int MaxMapCount = 10;

        // 각 맵은 하나의 Canvas 인스턴스로 관리된다.
        private readonly List<Canvas> _mapCanvases = [];

        // 각 맵 탭에 표시할 이름을 별도로 저장한다.
        private readonly List<string> _mapNames = [];

        // 현재 UI에 표시 중인 맵의 인덱스이다.
        private int _activeMapIndex;

        // 현재 표시 중인 맵 캔버스에 바로 접근하기 위한 속성이다.
        private Canvas ActiveMapCanvas => _mapCanvases[_activeMapIndex];

        // 새 맵을 추가하고 방금 만든 맵을 활성 맵으로 전환한다.
        private void AddMapButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mapCanvases.Count >= MaxMapCount)
            {
                return;
            }

            // 다른 캔버스로 전환하기 전에 드래그를 취소한다.
            CancelSeatDrag();

            _mapCanvases.Add(CreateMapCanvas());
            _mapNames.Add(BuildDefaultMapName(_mapCanvases.Count));
            _activeMapIndex = _mapCanvases.Count - 1;
            UpdateMapUi();
        }

        // 현재 활성 맵을 삭제하고 남은 맵 중 하나를 다시 활성화한다.
        private void RemoveMapButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mapCanvases.Count <= 1)
            {
                return;
            }

            var confirmationResult = MessageBox.Show(
                $"{_mapNames[_activeMapIndex]} 맵을 삭제할까요? 이 맵의 모든 기구가 함께 제거됩니다.",
                "맵 삭제",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmationResult != MessageBoxResult.Yes)
            {
                return;
            }

            CancelSeatDrag();
            _mapCanvases.RemoveAt(_activeMapIndex);
            _mapNames.RemoveAt(_activeMapIndex);
            _activeMapIndex = Math.Min(_activeMapIndex, _mapCanvases.Count - 1);
            UpdateMapUi();
        }

        // 사용자가 클릭한 맵 탭으로 활성 맵을 변경한다.
        private void MapTabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button || button.Tag is not int mapIndex)
            {
                return;
            }

            CancelSeatDrag();
            _activeMapIndex = mapIndex;
            UpdateMapUi();
        }

        // 더블클릭한 맵 탭의 이름을 바꾼다.
        private void MapTabButton_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Button button || button.Tag is not int mapIndex)
            {
                return;
            }

            var currentName = _mapNames[mapIndex];
            var renamedMap = PromptForMapName(currentName);
            if (string.IsNullOrWhiteSpace(renamedMap))
            {
                return;
            }

            _mapNames[mapIndex] = renamedMap.Trim();
            UpdateMapUi();
            e.Handled = true;
        }

        // 현재 활성 맵과 맵 탭 UI를 다시 그린다.
        private void UpdateMapUi()
        {
            // 호스트 영역에는 현재 활성 맵만 표시한다.
            MapHost.Children.Clear();
            MapHost.Children.Add(ActiveMapCanvas);

            // 현재 맵 개수와 선택 상태에 맞춰 탭 영역을 다시 만든다.
            MapTabsPanel.Children.Clear();
            for (var i = 0; i < _mapCanvases.Count; i++)
            {
                MapTabsPanel.Children.Add(BuildMapTabButton(i));
            }

            AddMapButton.IsEnabled = _mapCanvases.Count < MaxMapCount;
            AddMapButton.Opacity = AddMapButton.IsEnabled ? 1.0 : 0.5;
            RemoveMapButton.IsEnabled = _mapCanvases.Count > 1;
            RemoveMapButton.Opacity = RemoveMapButton.IsEnabled ? 1.0 : 0.5;
        }

        // 지정한 인덱스에 대응하는 맵 탭 버튼을 생성한다.
        private Button BuildMapTabButton(int mapIndex)
        {
            var isActive = mapIndex == _activeMapIndex;
            var button = new Button
            {
                Width = 120,
                Height = 34,
                Margin = new Thickness(0, 0, 8, 8),
                Content = _mapNames[mapIndex],
                Tag = mapIndex,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isActive ? "#24313F" : "#D7DCE8")),
                Foreground = isActive ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24313F")),
                BorderBrush = Brushes.Transparent
            };

            button.Click += MapTabButton_Click;
            button.MouseDoubleClick += MapTabButton_MouseDoubleClick;
            return button;
        }

        // 새 맵 캔버스를 만들고 공통 드래그/드롭 이벤트를 연결한다.
        private Canvas CreateMapCanvas()
        {
            // 모든 맵은 같은 크기, 배경, 드래그/드롭 동작을 사용한다.
            var mapCanvas = new Canvas
            {
                Width = 740,
                Height = 800,
                AllowDrop = true,
                Background = BuildMapBackground()
            };

            mapCanvas.DragOver += MapCanvas_DragOver;
            mapCanvas.Drop += MapCanvas_Drop;
            mapCanvas.MouseLeftButtonDown += MapCanvas_MouseLeftButtonDown;
            return mapCanvas;
        }

        // 기본 맵 이름을 생성한다.
        private static string BuildDefaultMapName(int mapNumber)
        {
            return $"맵 {mapNumber}";
        }

        // 맵 이름을 입력받는 간단한 대화상자를 연다.
        private string? PromptForMapName(string currentName)
        {
            var dialog = new Window
            {
                Title = "맵 이름 변경",
                Width = 340,
                Height = 160,
                MinWidth = 340,
                MinHeight = 160,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = this,
                Background = Brushes.White
            };

            var root = new Grid
            {
                Margin = new Thickness(16)
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            root.Children.Add(new TextBlock
            {
                Text = "맵 이름",
                FontWeight = FontWeights.SemiBold
            });

            var nameTextBox = new TextBox
            {
                Margin = new Thickness(0, 8, 0, 0),
                Text = currentName
            };
            Grid.SetRow(nameTextBox, 1);
            root.Children.Add(nameTextBox);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            Grid.SetRow(buttonPanel, 2);

            var saveButton = new Button
            {
                Width = 80,
                Margin = new Thickness(0, 0, 8, 0),
                Content = "저장",
                IsDefault = true
            };

            var cancelButton = new Button
            {
                Width = 80,
                Content = "취소",
                IsCancel = true
            };

            saveButton.Click += (_, _) =>
            {
                dialog.DialogResult = true;
                dialog.Close();
            };
            cancelButton.Click += (_, _) => dialog.Close();

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            root.Children.Add(buttonPanel);

            dialog.Content = root;
            dialog.Loaded += (_, _) =>
            {
                nameTextBox.Focus();
                nameTextBox.SelectAll();
            };

            return dialog.ShowDialog() == true ? nameTextBox.Text : null;
        }

        // 맵 배경에 사용할 반복 격자 브러시를 생성한다.
        private static Brush BuildMapBackground()
        {
            // 배치 위치를 보기 쉽게 반복 격자 배경을 그린다.
            return new DrawingBrush
            {
                TileMode = TileMode.Tile,
                Viewport = new Rect(0, 0, 40, 40),
                ViewportUnits = BrushMappingMode.Absolute,
                Drawing = new GeometryDrawing
                {
                    Brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FCFBF8")),
                    Pen = new Pen(new SolidColorBrush((Color)ColorConverter.ConvertFromString("#DDD7C9")), 1),
                    Geometry = new GeometryGroup
                    {
                        Children =
                        {
                            new LineGeometry(new Point(0, 0), new Point(40, 0)),
                            new LineGeometry(new Point(0, 0), new Point(0, 40))
                        }
                    }
                }
            };
        }
    }
}
