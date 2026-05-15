using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace UL_project
{
    public partial class MainWindow
    {
        // DragDrop 데이터 안에서 Seat 템플릿을 구분하기 위한 키.
        private const string SeatDragFormat = "UL_project.SeatTemplate";

        // DragDrop 데이터 안에서 Lack 템플릿을 구분하기 위한 키.
        private const string LackDragFormat = "UL_project.LackTemplate";

        // Seat 기본 크기.
        private const double SeatWidth = 120;
        private const double SeatHeight = 70;

        // Lack는 Seat보다 가로가 3배 길다.
        private const double LackWidth = SeatWidth * 3;
        private const double LackHeight = 70;

        // 현재 마우스로 끌고 있는 실제 배치 아이템.
        private Border? _draggingSeat;

        // 아이템 내부에서 어디를 잡았는지 저장해서 드래그 시 점프하지 않게 한다.
        private Point _seatDragOffset;

        // 새 Seat/Lack 이름을 자동 증가시키기 위한 카운터.
        private int _seatCounter = 1;
        private int _lackCounter = 1;

        // 휴지통 평상시/강조 색상.
        private static readonly Brush TrashNormalBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4C566F"));
        private static readonly Brush TrashHighlightBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C94C4C"));
        private static readonly Brush TrashNormalBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AAB3C5"));
        private static readonly Brush TrashHighlightBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFE3E3"));

        private void SeatTemplate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Place 모드일 때만 새 Seat를 만들 수 있다.
            if (_currentMode != EditorMode.Place)
            {
                return;
            }

            // DragDrop 데이터에 Seat 타입이라는 정보를 담아 끌기 시작한다.
            var dragData = new DataObject();
            dragData.SetData(SeatDragFormat, "Employee Seat");
            DragDrop.DoDragDrop(SeatTemplate, dragData, DragDropEffects.Copy);
        }

        private void LackTemplate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Place 모드일 때만 새 Lack를 만들 수 있다.
            if (_currentMode != EditorMode.Place)
            {
                return;
            }

            // DragDrop 데이터에 Lack 타입이라는 정보를 담아 끌기 시작한다.
            var dragData = new DataObject();
            dragData.SetData(LackDragFormat, "Lack");
            DragDrop.DoDragDrop(LackTemplate, dragData, DragDropEffects.Copy);
        }

        private void MapCanvas_DragOver(object sender, DragEventArgs e)
        {
            // 현재 끌고 있는 데이터가 Seat 또는 Lack일 때만 드롭 가능 표시를 보여준다.
            e.Effects = _currentMode == EditorMode.Place && (e.Data.GetDataPresent(SeatDragFormat) || e.Data.GetDataPresent(LackDragFormat))
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        private void MapCanvas_Drop(object sender, DragEventArgs e)
        {
            // Edit 모드에서는 맵에 새 아이템을 놓을 수 없다.
            if (_currentMode != EditorMode.Place)
            {
                return;
            }

            // 드롭 이벤트가 발생한 실제 맵 Canvas와 그 안의 좌표를 구한다.
            var mapCanvas = sender as Canvas ?? ActiveMapCanvas;
            var dropPoint = e.GetPosition(mapCanvas);

            if (e.Data.GetDataPresent(SeatDragFormat))
            {
                // Seat 템플릿이었다면 Seat를 생성한다.
                AddSeat(mapCanvas, dropPoint);
                return;
            }

            if (e.Data.GetDataPresent(LackDragFormat))
            {
                // Lack 템플릿이었다면 Lack를 생성한다.
                AddLack(mapCanvas, dropPoint);
            }
        }

        private void AddSeat(Canvas mapCanvas, Point dropPoint)
        {
            // 새 Seat UI 요소를 만들고 드롭된 맵에 추가한다.
            var seat = BuildSeatElement($"Seat {_seatCounter++}");

            mapCanvas.Children.Add(seat);

            // 드롭 좌표를 중심으로 아이템이 배치되도록 위치를 보정한다.
            SetSeatPosition(mapCanvas, seat, dropPoint.X - SeatWidth / 2, dropPoint.Y - SeatHeight / 2);
        }

        private void AddLack(Canvas mapCanvas, Point dropPoint)
        {
            // 새 Lack UI 요소를 만들고 드롭된 맵에 추가한다.
            var lack = BuildLackElement($"Lack {_lackCounter++}");

            mapCanvas.Children.Add(lack);
            SetSeatPosition(mapCanvas, lack, dropPoint.X - LackWidth / 2, dropPoint.Y - LackHeight / 2);
        }

        private Border BuildSeatElement(string title)
        {
            // 화면에 보이는 Seat와 연결될 데이터 객체.
            var seatInfo = new SeatInfo
            {
                SeatName = title
            };

            // Seat의 외형 Border를 만든다.
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

            // Border 안에 이름/소속을 세로로 쌓아 보여준다.
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
            seat.MouseLeftButtonDown += Seat_MouseLeftButtonDown;
            seat.MouseMove += Seat_MouseMove;
            seat.MouseLeftButtonUp += Seat_MouseLeftButtonUp;

            return seat;
        }

        private Border BuildLackElement(string title)
        {
            // Lack도 동일하게 데이터 객체를 붙여 관리한다.
            var lackInfo = new SeatInfo
            {
                SeatName = title,
                TeamName = "Shelf"
            };

            // Lack는 더 길고 다른 색상으로 만든다.
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

            // Lack도 이름과 분류 텍스트를 화면에 표시한다.
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
            lack.MouseLeftButtonDown += Seat_MouseLeftButtonDown;
            lack.MouseMove += Seat_MouseMove;
            lack.MouseLeftButtonUp += Seat_MouseLeftButtonUp;

            return lack;
        }

        private void Seat_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 실제 배치된 아이템만 드래그/편집 대상이 된다.
            if (sender is not Border seat)
            {
                return;
            }

            if (_currentMode == EditorMode.Edit)
            {
                // Edit 모드에서는 더블클릭 시 편집 창을 연다.
                if (e.ClickCount == 2)
                {
                    OpenSeatEditor(seat);
                }

                return;
            }

            // Place 모드에서는 드래그를 시작한다.
            _draggingSeat = seat;
            _seatDragOffset = e.GetPosition(seat);
            seat.CaptureMouse();

            // 끌고 있는 동안 가장 위에 보이도록 ZIndex를 올린다.
            Panel.SetZIndex(seat, 1000);
        }

        private void Seat_MouseMove(object sender, MouseEventArgs e)
        {
            // Place 모드 + 마우스 누름 + 드래그 대상이 있을 때만 이동시킨다.
            if (_currentMode != EditorMode.Place || _draggingSeat is null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            // 현재 아이템이 올라가 있는 맵 Canvas를 찾는다.
            var seatCanvas = GetSeatCanvas(_draggingSeat);
            if (seatCanvas is null)
            {
                return;
            }

            // 현재 마우스 좌표에 맞춰 아이템 위치를 갱신한다.
            var position = e.GetPosition(seatCanvas);
            SetSeatPosition(seatCanvas, _draggingSeat, position.X - _seatDragOffset.X, position.Y - _seatDragOffset.Y);

            // 마우스가 휴지통 위인지 검사해 강조 상태를 바꾼다.
            UpdateTrashDropZoneState(IsPointerOverTrash(e));
        }

        private void Seat_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 드래그 중이 아니면 아무 것도 하지 않는다.
            if (_draggingSeat is null)
            {
                return;
            }

            // Release 전에 참조를 잠시 보관해둔다.
            var seatToRelease = _draggingSeat;

            // Place 모드에서 휴지통 위에 놓였는지 먼저 판단한다.
            var shouldDeleteSeat = _currentMode == EditorMode.Place && IsPointerOverTrash(e);

            // 드래그 상태를 종료한다.
            _draggingSeat.ReleaseMouseCapture();
            Panel.SetZIndex(seatToRelease, 0);
            _draggingSeat = null;
            ResetTrashDropZoneAppearance();

            if (shouldDeleteSeat)
            {
                // 휴지통 위였다면 현재 맵에서 아이템을 제거한다.
                GetSeatCanvas(seatToRelease)?.Children.Remove(seatToRelease);
            }
        }

        private void CancelSeatDrag()
        {
            // 드래그 중인 항목이 없으면 정리할 것도 없다.
            if (_draggingSeat is null)
            {
                return;
            }

            // 마우스 캡처와 강조 상태를 정리해 드래그를 강제로 끝낸다.
            _draggingSeat.ReleaseMouseCapture();
            Panel.SetZIndex(_draggingSeat, 0);
            _draggingSeat = null;
            ResetTrashDropZoneAppearance();
        }

        private void SetSeatPosition(Canvas mapCanvas, Border seat, double left, double top)
        {
            // 아이템이 맵 밖으로 나가지 않도록 좌표를 경계 안으로 제한한다.
            var boundedLeft = Math.Max(0, Math.Min(mapCanvas.Width - seat.Width, left));
            var boundedTop = Math.Max(0, Math.Min(mapCanvas.Height - seat.Height, top));

            Canvas.SetLeft(seat, boundedLeft);
            Canvas.SetTop(seat, boundedTop);
        }

        private static Canvas? GetSeatCanvas(Border seat)
        {
            // 현재 아이템이 어느 Canvas에 들어있는지 찾는다.
            return seat.Parent as Canvas;
        }

        private bool IsPointerOverTrash(MouseEventArgs e)
        {
            // 포인터 위치를 MainWindow 좌표계 기준으로 구한다.
            var pointerPosition = e.GetPosition(this);

            // 휴지통 Border의 실제 화면 영역을 계산한다.
            var trashBounds = TrashDropZone.TransformToAncestor(this)
                .TransformBounds(new Rect(0, 0, TrashDropZone.ActualWidth, TrashDropZone.ActualHeight));

            // 현재 포인터가 휴지통 영역 안에 있는지 반환한다.
            return trashBounds.Contains(pointerPosition);
        }

        private void UpdateTrashDropZoneState(bool isPointerOverTrash)
        {
            // 휴지통 위에 올라왔으면 강조 색으로, 아니면 기본 색으로 보여준다.
            TrashDropZone.Background = isPointerOverTrash ? TrashHighlightBackground : TrashNormalBackground;
            TrashDropZone.BorderBrush = isPointerOverTrash ? TrashHighlightBorder : TrashNormalBorder;
        }

        private void ResetTrashDropZoneAppearance()
        {
            // 드래그가 끝나면 휴지통 색을 항상 기본 상태로 되돌린다.
            TrashDropZone.Background = TrashNormalBackground;
            TrashDropZone.BorderBrush = TrashNormalBorder;
        }
    }
}
