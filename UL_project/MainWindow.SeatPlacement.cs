using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UL_project
{
    public partial class MainWindow
    {
        // DragDrop ?곗씠???덉뿉??Seat ?쒗뵆由우쓣 援щ텇?섍린 ?꾪븳 ??
        private const string SeatDragFormat = "UL_project.SeatTemplate";

        // DragDrop ?곗씠???덉뿉??Lack ?쒗뵆由우쓣 援щ텇?섍린 ?꾪븳 ??
        private const string LackDragFormat = "UL_project.LackTemplate";

        // Seat 湲곕낯 ?ш린.
        private const double SeatWidth = 120;
        private const double SeatHeight = 70;

        // Lack??Seat蹂대떎 媛濡쒓? 3諛?湲몃떎.
        private const double LackWidth = SeatWidth * 3;
        private const double LackHeight = 70;

        // ?꾩옱 留덉슦?ㅻ줈 ?뚭퀬 ?덈뒗 ?ㅼ젣 諛곗튂 ?꾩씠??
        private Border? _draggingSeat;

        // ?꾩씠???대??먯꽌 ?대뵒瑜??≪븯?붿? ??ν빐???쒕옒洹????먰봽?섏? ?딄쾶 ?쒕떎.
        private Point _seatDragOffset;

        // ??Seat/Lack ?대쫫???먮룞 利앷??쒗궎湲??꾪븳 移댁슫??
        private int _seatCounter = 1;
        private int _lackCounter = 1;

        // ?댁????됱긽??媛뺤“ ?됱긽.
        private static readonly Brush TrashNormalBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C566F"));
        private static readonly Brush TrashHighlightBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C94C4C"));
        private static readonly Brush TrashNormalBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAB3C5"));
        private static readonly Brush TrashHighlightBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE3E3"));

        private void SeatTemplate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Place 紐⑤뱶???뚮쭔 ??Seat瑜?留뚮뱾 ???덈떎.
            if (_currentMode != EditorMode.Place)
            {
                return;
            }

            // DragDrop ?곗씠?곗뿉 Seat ??낆씠?쇰뒗 ?뺣낫瑜??댁븘 ?뚭린 ?쒖옉?쒕떎.
            var dragData = new DataObject();
            dragData.SetData(SeatDragFormat, "Employee Seat");
            DragDrop.DoDragDrop(SeatTemplate, dragData, DragDropEffects.Copy);
        }

        private void LackTemplate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Place 紐⑤뱶???뚮쭔 ??Lack瑜?留뚮뱾 ???덈떎.
            if (_currentMode != EditorMode.Place)
            {
                return;
            }

            // DragDrop ?곗씠?곗뿉 Lack ??낆씠?쇰뒗 ?뺣낫瑜??댁븘 ?뚭린 ?쒖옉?쒕떎.
            var dragData = new DataObject();
            dragData.SetData(LackDragFormat, "Lack");
            DragDrop.DoDragDrop(LackTemplate, dragData, DragDropEffects.Copy);
        }

        private void MapCanvas_DragOver(object sender, DragEventArgs e)
        {
            // ?꾩옱 ?뚭퀬 ?덈뒗 ?곗씠?곌? Seat ?먮뒗 Lack???뚮쭔 ?쒕∼ 媛???쒖떆瑜?蹂댁뿬以??
            e.Effects = _currentMode == EditorMode.Place && (e.Data.GetDataPresent(SeatDragFormat) || e.Data.GetDataPresent(LackDragFormat))
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void MapCanvas_Drop(object sender, DragEventArgs e)
        {
            // Edit 紐⑤뱶?먯꽌??留듭뿉 ???꾩씠?쒖쓣 ?볦쓣 ???녿떎.
            if (_currentMode != EditorMode.Place)
            {
                return;
            }

            // ?쒕∼ ?대깽?멸? 諛쒖깮???ㅼ젣 留?Canvas? 洹??덉쓽 醫뚰몴瑜?援ы븳??
            var mapCanvas = sender as Canvas ?? ActiveMapCanvas;
            var dropPoint = e.GetPosition(mapCanvas);

            if (e.Data.GetDataPresent(SeatDragFormat))
            {
                // Seat ?쒗뵆由우씠?덈떎硫?Seat瑜??앹꽦?쒕떎.
                AddSeat(mapCanvas, dropPoint);
                return;
            }

            if (e.Data.GetDataPresent(LackDragFormat))
            {
                // Lack ?쒗뵆由우씠?덈떎硫?Lack瑜??앹꽦?쒕떎.
                AddLack(mapCanvas, dropPoint);
            }
        }

        private void AddSeat(Canvas mapCanvas, Point dropPoint)
        {
            // ??Seat UI ?붿냼瑜?留뚮뱾怨??쒕∼??留듭뿉 異붽??쒕떎.
            var seat = BuildSeatElement($"Seat {_seatCounter++}");

            mapCanvas.Children.Add(seat);

            // ?쒕∼ 醫뚰몴瑜?以묒떖?쇰줈 ?꾩씠?쒖씠 諛곗튂?섎룄濡??꾩튂瑜?蹂댁젙?쒕떎.
            SetSeatPosition(mapCanvas, seat, dropPoint.X - SeatWidth / 2, dropPoint.Y - SeatHeight / 2);
        }

        private void AddLack(Canvas mapCanvas, Point dropPoint)
        {
            // ??Lack UI ?붿냼瑜?留뚮뱾怨??쒕∼??留듭뿉 異붽??쒕떎.
            var lack = BuildLackElement($"Lack {_lackCounter++}");

            mapCanvas.Children.Add(lack);
            SetSeatPosition(mapCanvas, lack, dropPoint.X - LackWidth / 2, dropPoint.Y - LackHeight / 2);
        }

        private Border BuildSeatElement(string title)
        {
            // ?붾㈃??蹂댁씠??Seat? ?곌껐???곗씠??媛앹껜.
            var seatInfo = new SeatInfo
            {
                SeatName = title
            };

            // Seat???명삎 Border瑜?留뚮뱺??
            var seat = new Border
            {
                Width = SeatWidth,
                Height = SeatHeight,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F6B73C")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8F5A00")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12),
                Cursor = Cursors.SizeAll,
                Tag = seatInfo
            };

            // Border ?덉뿉 ?대쫫/?뚯냽???몃줈濡??볦븘 蹂댁뿬以??
            var content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            content.Children.Add(new TextBlock
            {
                Text = seatInfo.SeatName,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B2112")),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = "Unassigned",
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C4622")),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            seat.Child = content;
            seat.ToolTip = "0 equipment item(s)";
            seat.MouseLeftButtonDown += Seat_MouseLeftButtonDown;
            seat.MouseMove += Seat_MouseMove;
            seat.MouseLeftButtonUp += Seat_MouseLeftButtonUp;

            return seat;
        }

        private Border BuildLackElement(string title)
        {
            // Lack???숈씪?섍쾶 ?곗씠??媛앹껜瑜?遺숈뿬 愿由ы븳??
            var lackInfo = new SeatInfo
            {
                SeatName = title,
                TeamName = "Shelf"
            };

            // Lack????湲멸퀬 ?ㅻⅨ ?됱긽?쇰줈 留뚮뱺??
            var lack = new Border
            {
                Width = LackWidth,
                Height = LackHeight,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C9D46A")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F7A1E")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12),
                Cursor = Cursors.SizeAll,
                Tag = lackInfo
            };

            // Lack???대쫫怨?遺꾨쪟 ?띿뒪?몃? ?붾㈃???쒖떆?쒕떎.
            var content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            content.Children.Add(new TextBlock
            {
                Text = lackInfo.SeatName,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28310F")),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = lackInfo.TeamName,
                FontSize = 11,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B561B")),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            lack.Child = content;
            lack.ToolTip = "0 equipment item(s)";
            lack.MouseLeftButtonDown += Seat_MouseLeftButtonDown;
            lack.MouseMove += Seat_MouseMove;
            lack.MouseLeftButtonUp += Seat_MouseLeftButtonUp;

            return lack;
        }

        private void Seat_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // ?ㅼ젣 諛곗튂???꾩씠?쒕쭔 ?쒕옒洹??몄쭛 ??곸씠 ?쒕떎.
            if (sender is not Border seat)
            {
                return;
            }

            // Edit mode opens the editor on a single click instead of dragging.
            if (_currentMode == EditorMode.Edit)
            {
                OpenSeatEditor(seat);
                return;
            }

            // Place 紐⑤뱶?먯꽌???쒕옒洹몃? ?쒖옉?쒕떎.
            _draggingSeat = seat;
            _seatDragOffset = e.GetPosition(seat);
            seat.CaptureMouse();

            // ?뚭퀬 ?덈뒗 ?숈븞 媛???꾩뿉 蹂댁씠?꾨줉 ZIndex瑜??щ┛??
            Panel.SetZIndex(seat, 1000);
        }

        private void Seat_MouseMove(object sender, MouseEventArgs e)
        {
            // Place 紐⑤뱶 + 留덉슦???꾨쫫 + ?쒕옒洹???곸씠 ?덉쓣 ?뚮쭔 ?대룞?쒗궓??
            if (_currentMode != EditorMode.Place || _draggingSeat is null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            // ?꾩옱 ?꾩씠?쒖씠 ?щ씪媛 ?덈뒗 留?Canvas瑜?李얜뒗??
            var seatCanvas = GetSeatCanvas(_draggingSeat);
            if (seatCanvas is null)
            {
                return;
            }

            // ?꾩옱 留덉슦??醫뚰몴??留욎떠 ?꾩씠???꾩튂瑜?媛깆떊?쒕떎.
            var position = e.GetPosition(seatCanvas);
            SetSeatPosition(seatCanvas, _draggingSeat, position.X - _seatDragOffset.X, position.Y - _seatDragOffset.Y);

            // 留덉슦?ㅺ? ?댁????꾩씤吏 寃?ы빐 媛뺤“ ?곹깭瑜?諛붽씔??
            UpdateTrashDropZoneState(IsPointerOverTrash(e));
        }

        private void Seat_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // ?쒕옒洹?以묒씠 ?꾨땲硫??꾨Т 寃껊룄 ?섏? ?딅뒗??
            if (_draggingSeat is null)
            {
                return;
            }

            // Release ?꾩뿉 李몄“瑜??좎떆 蹂닿??대몦??
            var seatToRelease = _draggingSeat;

            // Place 紐⑤뱶?먯꽌 ?댁????꾩뿉 ?볦??붿? 癒쇱? ?먮떒?쒕떎.
            var shouldDeleteSeat = _currentMode == EditorMode.Place && IsPointerOverTrash(e);

            // ?쒕옒洹??곹깭瑜?醫낅즺?쒕떎.
            _draggingSeat.ReleaseMouseCapture();
            Panel.SetZIndex(seatToRelease, 0);
            _draggingSeat = null;
            ResetTrashDropZoneAppearance();

            if (shouldDeleteSeat)
            {
                // ?댁????꾩??ㅻ㈃ ?꾩옱 留듭뿉???꾩씠?쒖쓣 ?쒓굅?쒕떎.
                GetSeatCanvas(seatToRelease)?.Children.Remove(seatToRelease);
            }
        }

        private void CancelSeatDrag()
        {
            // ?쒕옒洹?以묒씤 ??ぉ???놁쑝硫??뺣━??寃껊룄 ?녿떎.
            if (_draggingSeat is null)
            {
                return;
            }

            // 留덉슦??罹≪쿂? 媛뺤“ ?곹깭瑜??뺣━???쒕옒洹몃? 媛뺤젣濡??앸궦??
            _draggingSeat.ReleaseMouseCapture();
            Panel.SetZIndex(_draggingSeat, 0);
            _draggingSeat = null;
            ResetTrashDropZoneAppearance();
        }

        private void SetSeatPosition(Canvas mapCanvas, Border seat, double left, double top)
        {
            // ?꾩씠?쒖씠 留?諛뽰쑝濡??섍?吏 ?딅룄濡?醫뚰몴瑜?寃쎄퀎 ?덉쑝濡??쒗븳?쒕떎.
            var boundedLeft = Math.Max(0, Math.Min(mapCanvas.Width - seat.Width, left));
            var boundedTop = Math.Max(0, Math.Min(mapCanvas.Height - seat.Height, top));

            Canvas.SetLeft(seat, boundedLeft);
            Canvas.SetTop(seat, boundedTop);
        }

        private static Canvas? GetSeatCanvas(Border seat)
        {
            // ?꾩옱 ?꾩씠?쒖씠 ?대뒓 Canvas???ㅼ뼱?덈뒗吏 李얜뒗??
            return seat.Parent as Canvas;
        }

        private bool IsPointerOverTrash(MouseEventArgs e)
        {
            // ?ъ씤???꾩튂瑜?MainWindow 醫뚰몴怨?湲곗??쇰줈 援ы븳??
            var pointerPosition = e.GetPosition(this);

            // ?댁???Border???ㅼ젣 ?붾㈃ ?곸뿭??怨꾩궛?쒕떎.
            var trashBounds = TrashDropZone.TransformToAncestor(this)
                .TransformBounds(new Rect(0, 0, TrashDropZone.ActualWidth, TrashDropZone.ActualHeight));

            // ?꾩옱 ?ъ씤?곌? ?댁????곸뿭 ?덉뿉 ?덈뒗吏 諛섑솚?쒕떎.
            return trashBounds.Contains(pointerPosition);
        }

        private void UpdateTrashDropZoneState(bool isPointerOverTrash)
        {
            // ?댁????꾩뿉 ?щ씪?붿쑝硫?媛뺤“ ?됱쑝濡? ?꾨땲硫?湲곕낯 ?됱쑝濡?蹂댁뿬以??
            TrashDropZone.Background = isPointerOverTrash ? TrashHighlightBackground : TrashNormalBackground;
            TrashDropZone.BorderBrush = isPointerOverTrash ? TrashHighlightBorder : TrashNormalBorder;
        }

        private void ResetTrashDropZoneAppearance()
        {
            // ?쒕옒洹멸? ?앸굹硫??댁????됱쓣 ??긽 湲곕낯 ?곹깭濡??섎룎由곕떎.
            TrashDropZone.Background = TrashNormalBackground;
            TrashDropZone.BorderBrush = TrashNormalBorder;
        }
    }
}
