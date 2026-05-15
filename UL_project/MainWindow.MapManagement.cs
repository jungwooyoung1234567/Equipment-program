using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UL_project
{
    public partial class MainWindow
    {
        // 동시에 만들 수 있는 최대 맵 수.
        private const int MaxMapCount = 10;

        // 각 맵은 Canvas 하나로 관리한다.
        private readonly List<Canvas> _mapCanvases = [];

        // 현재 화면에 보여주는 맵의 인덱스.
        private int _activeMapIndex;

        // 현재 선택된 맵 Canvas를 빠르게 가져오기 위한 속성.
        private Canvas ActiveMapCanvas => _mapCanvases[_activeMapIndex];

        private void AddMapButton_Click(object sender, RoutedEventArgs e)
        {
            // 최대 개수에 도달했다면 새 맵을 만들지 않는다.
            if (_mapCanvases.Count >= MaxMapCount)
            {
                return;
            }

            // 맵을 바꾸기 전에 진행 중이던 드래그를 정리한다.
            CancelSeatDrag();

            // 새 맵을 만든 뒤 마지막 맵을 활성 맵으로 선택한다.
            _mapCanvases.Add(CreateMapCanvas());
            _activeMapIndex = _mapCanvases.Count - 1;
            UpdateMapUi();
        }

        private void RemoveMapButton_Click(object sender, RoutedEventArgs e)
        {
            // 최소 1개의 맵은 항상 남겨둔다.
            if (_mapCanvases.Count <= 1)
            {
                return;
            }

            // 현재 보고 있는 맵을 삭제하고, 유효한 인덱스로 다시 맞춘다.
            CancelSeatDrag();
            _mapCanvases.RemoveAt(_activeMapIndex);
            _activeMapIndex = Math.Min(_activeMapIndex, _mapCanvases.Count - 1);
            UpdateMapUi();
        }

        private void MapTabButton_Click(object sender, RoutedEventArgs e)
        {
            // 버튼의 Tag에 저장해둔 맵 인덱스를 읽는다.
            if (sender is not Button button || button.Tag is not int mapIndex)
            {
                return;
            }

            // 다른 맵으로 전환하기 전에 드래그 상태를 정리한다.
            CancelSeatDrag();
            _activeMapIndex = mapIndex;
            UpdateMapUi();
        }

        private void UpdateMapUi()
        {
            // 맵 표시 영역에는 항상 현재 활성 맵 1개만 보여준다.
            MapHost.Children.Clear();
            MapHost.Children.Add(ActiveMapCanvas);

            // 맵 탭도 현재 맵 개수에 맞게 전부 다시 그린다.
            MapTabsPanel.Children.Clear();
            for (var i = 0; i < _mapCanvases.Count; i++)
            {
                MapTabsPanel.Children.Add(BuildMapTabButton(i));
            }

            // 맵 개수에 따라 + / - 버튼 활성 여부를 조정한다.
            AddMapButton.IsEnabled = _mapCanvases.Count < MaxMapCount;
            AddMapButton.Opacity = AddMapButton.IsEnabled ? 1.0 : 0.5;
            RemoveMapButton.IsEnabled = _mapCanvases.Count > 1;
            RemoveMapButton.Opacity = RemoveMapButton.IsEnabled ? 1.0 : 0.5;
        }

        private Button BuildMapTabButton(int mapIndex)
        {
            // 활성 맵 탭만 진한 색으로 보여준다.
            var isActive = mapIndex == _activeMapIndex;
            var button = new Button
            {
                Width = 88,
                Height = 34,
                Margin = new Thickness(0, 0, 8, 8),
                Content = $"Map {mapIndex + 1}",
                Tag = mapIndex,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isActive ? "#24313F" : "#D7DCE8")),
                Foreground = isActive ? Brushes.White : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24313F")),
                BorderBrush = Brushes.Transparent
            };

            // 어떤 맵 버튼이 눌렸는지 구분하기 위해 공통 클릭 핸들러를 연결한다.
            button.Click += MapTabButton_Click;
            return button;
        }

        private Canvas CreateMapCanvas()
        {
            // 새 맵은 동일한 크기와 배경 격자 패턴을 사용한다.
            var mapCanvas = new Canvas
            {
                Width = 740,
                Height = 800,
                AllowDrop = true,
                Background = BuildMapBackground()
            };

            // 새로 만든 맵도 기존 맵과 같은 드롭 이벤트를 처리하도록 연결한다.
            mapCanvas.DragOver += MapCanvas_DragOver;
            mapCanvas.Drop += MapCanvas_Drop;
            return mapCanvas;
        }

        private static Brush BuildMapBackground()
        {
            // DrawingBrush로 격자 배경을 직접 그린다.
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
                        // 가로/세로 선 2개를 반복 타일로 사용해 모눈종이처럼 보이게 만든다.
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
