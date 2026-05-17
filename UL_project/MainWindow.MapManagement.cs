using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UL_project
{
    public partial class MainWindow
    {
        // ?숈떆??留뚮뱾 ???덈뒗 理쒕? 留???
        private const int MaxMapCount = 10;

        // 媛?留듭? Canvas ?섎굹濡?愿由ы븳??
        private readonly List<Canvas> _mapCanvases = [];

        // ?꾩옱 ?붾㈃??蹂댁뿬二쇰뒗 留듭쓽 ?몃뜳??
        private int _activeMapIndex;

        // ?꾩옱 ?좏깮??留?Canvas瑜?鍮좊Ⅴ寃?媛?몄삤湲??꾪븳 ?띿꽦.
        private Canvas ActiveMapCanvas => _mapCanvases[_activeMapIndex];

        private void AddMapButton_Click(object sender, RoutedEventArgs e)
        {
            // 理쒕? 媛쒖닔???꾨떖?덈떎硫???留듭쓣 留뚮뱾吏 ?딅뒗??
            if (_mapCanvases.Count >= MaxMapCount)
            {
                return;
            }

            // 留듭쓣 諛붽씀湲??꾩뿉 吏꾪뻾 以묒씠???쒕옒洹몃? ?뺣━?쒕떎.
            CancelSeatDrag();

            // ??留듭쓣 留뚮뱺 ??留덉?留?留듭쓣 ?쒖꽦 留듭쑝濡??좏깮?쒕떎.
            _mapCanvases.Add(CreateMapCanvas());
            _activeMapIndex = _mapCanvases.Count - 1;
            UpdateMapUi();
        }

        private void RemoveMapButton_Click(object sender, RoutedEventArgs e)
        {
            // 理쒖냼 1媛쒖쓽 留듭? ??긽 ?④꺼?붾떎.
            if (_mapCanvases.Count <= 1)
            {
                return;
            }

            var confirmationResult = MessageBox.Show(
                $"Delete Map {_activeMapIndex + 1}? All seats on this map will be removed.",
                "Delete Map",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmationResult != MessageBoxResult.Yes)
            {
                return;
            }

            // ?꾩옱 蹂닿퀬 ?덈뒗 留듭쓣 ??젣?섍퀬, ?좏슚???몃뜳?ㅻ줈 ?ㅼ떆 留욎텣??
            CancelSeatDrag();
            _mapCanvases.RemoveAt(_activeMapIndex);
            _activeMapIndex = Math.Min(_activeMapIndex, _mapCanvases.Count - 1);
            UpdateMapUi();
        }

        private void MapTabButton_Click(object sender, RoutedEventArgs e)
        {
            // 踰꾪듉??Tag????ν빐??留??몃뜳?ㅻ? ?쎈뒗??
            if (sender is not Button button || button.Tag is not int mapIndex)
            {
                return;
            }

            // ?ㅻⅨ 留듭쑝濡??꾪솚?섍린 ?꾩뿉 ?쒕옒洹??곹깭瑜??뺣━?쒕떎.
            CancelSeatDrag();
            _activeMapIndex = mapIndex;
            UpdateMapUi();
        }

        private void UpdateMapUi()
        {
            // 留??쒖떆 ?곸뿭?먮뒗 ??긽 ?꾩옱 ?쒖꽦 留?1媛쒕쭔 蹂댁뿬以??
            MapHost.Children.Clear();
            MapHost.Children.Add(ActiveMapCanvas);

            // 留???룄 ?꾩옱 留?媛쒖닔??留욊쾶 ?꾨? ?ㅼ떆 洹몃┛??
            MapTabsPanel.Children.Clear();
            for (var i = 0; i < _mapCanvases.Count; i++)
            {
                MapTabsPanel.Children.Add(BuildMapTabButton(i));
            }

            // 留?媛쒖닔???곕씪 + / - 踰꾪듉 ?쒖꽦 ?щ?瑜?議곗젙?쒕떎.
            AddMapButton.IsEnabled = _mapCanvases.Count < MaxMapCount;
            AddMapButton.Opacity = AddMapButton.IsEnabled ? 1.0 : 0.5;
            RemoveMapButton.IsEnabled = _mapCanvases.Count > 1;
            RemoveMapButton.Opacity = RemoveMapButton.IsEnabled ? 1.0 : 0.5;
        }

        private Button BuildMapTabButton(int mapIndex)
        {
            // ?쒖꽦 留???쭔 吏꾪븳 ?됱쑝濡?蹂댁뿬以??
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

            // ?대뼡 留?踰꾪듉???뚮졇?붿? 援щ텇?섍린 ?꾪빐 怨듯넻 ?대┃ ?몃뱾?щ? ?곌껐?쒕떎.
            button.Click += MapTabButton_Click;
            return button;
        }

        private Canvas CreateMapCanvas()
        {
            // ??留듭? ?숈씪???ш린? 諛곌꼍 寃⑹옄 ?⑦꽩???ъ슜?쒕떎.
            var mapCanvas = new Canvas
            {
                Width = 740,
                Height = 800,
                AllowDrop = true,
                Background = BuildMapBackground()
            };

            // ?덈줈 留뚮뱺 留듬룄 湲곗〈 留듦낵 媛숈? ?쒕∼ ?대깽?몃? 泥섎━?섎룄濡??곌껐?쒕떎.
            mapCanvas.DragOver += MapCanvas_DragOver;
            mapCanvas.Drop += MapCanvas_Drop;
            return mapCanvas;
        }

        private static Brush BuildMapBackground()
        {
            // DrawingBrush濡?寃⑹옄 諛곌꼍??吏곸젒 洹몃┛??
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
                        // 媛濡??몃줈 ??2媛쒕? 諛섎났 ??쇰줈 ?ъ슜??紐⑤늿醫낆씠泥섎읆 蹂댁씠寃?留뚮뱺??
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
