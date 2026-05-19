using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace UL_project
{
    public partial class MainWindow
    {
        // 이 파일은 팔레트 드래그/드롭, 좌석 이동, 클릭 메뉴 삭제를 담당한다.
        private const string SeatDragFormat = "UL_project.SeatTemplate";
        private const string LackDragFormat = "UL_project.LackTemplate";
        private const string CartDragFormat = "UL_project.CartTemplate";

        // 배치 가능한 두 항목의 기본 크기이다.
        private const double SeatWidth = 120;
        private const double SeatHeight = 70;
        private const double LackWidth = SeatWidth * 3;
        private const double LackHeight = 70;
        private const double CartWidth = SeatWidth / 2;
        private const double CartHeight = SeatHeight;

        // 현재 맵 캔버스 안에서 실제로 드래그 중인 요소이다.
        private Border? _draggingSeat;

        // 클릭되었지만 아직 드래그인지 메뉴 호출인지 결정되지 않은 요소이다.
        private Border? _pressedSeat;

        // 드래그 시작 지점의 마우스 오프셋이다. 이동 시 튀는 현상을 막는다.
        private Point _seatDragOffset;
        
        // 마우스를 누른 좌표를 기억해 클릭과 드래그를 구분한다.
        private Point _seatPointerDownPosition;

        // 기본 이름을 자동 생성할 때 사용하는 카운터이다.
        private int _seatCounter = 1;
        private int _lackCounter = 1;
        private int _cartCounter = 1;

        // Seat 팔레트 항목의 드래그를 시작한다.
        private void SeatTemplate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentMode != EditorMode.Place)
            {
                return;
            }

            var dragData = new DataObject();
            dragData.SetData(SeatDragFormat, "Employee Seat");
            DragDrop.DoDragDrop(SeatTemplate, dragData, DragDropEffects.Copy);
        }

        // Lack 팔레트 항목의 드래그를 시작한다.
        private void LackTemplate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentMode != EditorMode.Place)
            {
                return;
            }

            var dragData = new DataObject();
            dragData.SetData(LackDragFormat, "Lack");
            DragDrop.DoDragDrop(LackTemplate, dragData, DragDropEffects.Copy);
        }

        // Cart 팔레트 항목의 드래그를 시작한다.
        private void CartTemplate_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentMode != EditorMode.Place)
            {
                return;
            }

            var dragData = new DataObject();
            dragData.SetData(CartDragFormat, "Cart");
            DragDrop.DoDragDrop(CartTemplate, dragData, DragDropEffects.Copy);
        }

        // 현재 드래그 데이터가 맵에 놓을 수 있는 항목인지 판별한다.
        private void MapCanvas_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = _currentMode == EditorMode.Place && (e.Data.GetDataPresent(SeatDragFormat) || e.Data.GetDataPresent(LackDragFormat) || e.Data.GetDataPresent(CartDragFormat))
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

        // 팔레트 항목을 맵 위에 놓았을 때 새 Seat 또는 Lack를 생성한다.
        private void MapCanvas_Drop(object sender, DragEventArgs e)
        {
            if (_currentMode != EditorMode.Place)
            {
                return;
            }

            var mapCanvas = sender as Canvas ?? ActiveMapCanvas;
            var dropPoint = e.GetPosition(mapCanvas);

            if (e.Data.GetDataPresent(SeatDragFormat))
            {
                AddSeat(mapCanvas, dropPoint);
                return;
            }

            if (e.Data.GetDataPresent(LackDragFormat))
            {
                AddLack(mapCanvas, dropPoint);
                return;
            }

            if (e.Data.GetDataPresent(CartDragFormat))
            {
                AddCart(mapCanvas, dropPoint);
            }
        }

        // 새 Seat 요소를 생성해 맵에 추가한다.
        private void AddSeat(Canvas mapCanvas, Point dropPoint)
        {
            var seat = BuildSeatElement($"Seat {_seatCounter++}");
            mapCanvas.Children.Add(seat);
            SetSeatPosition(mapCanvas, seat, dropPoint.X - SeatWidth / 2, dropPoint.Y - SeatHeight / 2);
        }

        // 새 Lack 요소를 생성해 맵에 추가한다.
        private void AddLack(Canvas mapCanvas, Point dropPoint)
        {
            var lack = BuildLackElement($"Lack {_lackCounter++}");
            mapCanvas.Children.Add(lack);
            SetSeatPosition(mapCanvas, lack, dropPoint.X - LackWidth / 2, dropPoint.Y - LackHeight / 2);
        }

        // 새 Cart 요소를 생성해 맵에 추가한다.
        private void AddCart(Canvas mapCanvas, Point dropPoint)
        {
            var cart = BuildCartElement($"Cart {_cartCounter++}");
            mapCanvas.Children.Add(cart);
            SetSeatPosition(mapCanvas, cart, dropPoint.X - CartWidth / 2, dropPoint.Y - CartHeight / 2);
        }

        // Seat UI 요소와 기본 SeatInfo 데이터를 함께 만든다.
        private Border BuildSeatElement(string title)
        {
            // Seat 메타데이터는 Border.Tag에 직접 저장한다.
            var seatInfo = new SeatInfo
            {
                SeatName = title
            };

            var seat = new Border
            {
                Width = SeatWidth,
                Height = SeatHeight,
                Uid = SeatItemType,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F6B73C")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8F5A00")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12),
                Cursor = Cursors.SizeAll,
                Tag = seatInfo
            };

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
            seat.MouseRightButtonUp += Seat_MouseRightButtonUp;

            return seat;
        }

        // Lack UI 요소와 기본 SeatInfo 데이터를 함께 만든다.
        private Border BuildLackElement(string title)
        {
            var lackInfo = new SeatInfo
            {
                SeatName = title,
                TeamName = "Shelf"
            };

            var lack = new Border
            {
                Width = LackWidth,
                Height = LackHeight,
                Uid = LackItemType,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C9D46A")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6F7A1E")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12),
                Cursor = Cursors.SizeAll,
                Tag = lackInfo
            };

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
            lack.MouseRightButtonUp += Seat_MouseRightButtonUp;

            return lack;
        }

        // Cart UI 요소와 기본 SeatInfo 데이터를 함께 만든다.
        private Border BuildCartElement(string title)
        {
            var cartInfo = new SeatInfo
            {
                SeatName = title,
                TeamName = "Cart"
            };

            var cart = new Border
            {
                Width = CartWidth,
                Height = CartHeight,
                Uid = CartItemType,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7FC8F8")),
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F6FA8")),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(12),
                Cursor = Cursors.SizeAll,
                Tag = cartInfo
            };

            var content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            content.Children.Add(new TextBlock
            {
                Text = cartInfo.SeatName,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#103652")),
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            });

            content.Children.Add(new TextBlock
            {
                Text = cartInfo.TeamName,
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24506F")),
                HorizontalAlignment = HorizontalAlignment.Center
            });

            cart.Child = content;
            cart.ToolTip = "0 equipment item(s)";
            cart.MouseLeftButtonDown += Seat_MouseLeftButtonDown;
            cart.MouseMove += Seat_MouseMove;
            cart.MouseLeftButtonUp += Seat_MouseLeftButtonUp;
            cart.MouseRightButtonUp += Seat_MouseRightButtonUp;

            return cart;
        }

        // Seat 또는 Lack를 좌클릭했을 때 편집 또는 드래그를 준비한다.
        private void Seat_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border seat)
            {
                return;
            }

            if (ReferenceEquals(_highlightedSeat, seat))
            {
                StopSeatHighlight();
            }

            // 편집 모드에서는 드래그 대신 한 번 클릭으로 편집창을 연다.
            if (_currentMode == EditorMode.Edit)
            {
                OpenSeatEditor(seat);
                return;
            }

            _pressedSeat = seat;
            _seatDragOffset = e.GetPosition(seat);
            _seatPointerDownPosition = _seatDragOffset;
            seat.CaptureMouse();
        }

        // 드래그 중인 Seat 또는 Lack의 위치를 계속 갱신한다.
        private void Seat_MouseMove(object sender, MouseEventArgs e)
        {
            // 마우스를 놓거나 모드가 바뀔 때까지 위치를 계속 갱신한다.
            if (_currentMode != EditorMode.Place || _pressedSeat is null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (_draggingSeat is null)
            {
                var currentPosition = e.GetPosition(_pressedSeat);
                var horizontalDistance = Math.Abs(currentPosition.X - _seatPointerDownPosition.X);
                var verticalDistance = Math.Abs(currentPosition.Y - _seatPointerDownPosition.Y);

                if (horizontalDistance < SystemParameters.MinimumHorizontalDragDistance &&
                    verticalDistance < SystemParameters.MinimumVerticalDragDistance)
                {
                    return;
                }

                _draggingSeat = _pressedSeat;

                // 드래그 중인 요소를 다른 요소 위로 올려 보이게 한다.
                Panel.SetZIndex(_draggingSeat, 1000);
            }

            var seatCanvas = GetSeatCanvas(_draggingSeat);
            if (seatCanvas is null)
            {
                return;
            }

            var position = e.GetPosition(seatCanvas);
            SetSeatPosition(seatCanvas, _draggingSeat, position.X - _seatDragOffset.X, position.Y - _seatDragOffset.Y);
        }

        // 드래그를 끝내고 마우스 캡처를 해제한다.
        private void Seat_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_pressedSeat is null)
            {
                return;
            }

            _pressedSeat.ReleaseMouseCapture();

            if (_draggingSeat is not null)
            {
                Panel.SetZIndex(_draggingSeat, 0);
            }

            _pressedSeat = null;
            _draggingSeat = null;
        }

        // 진행 중인 드래그 상태를 강제로 정리한다.
        private void CancelSeatDrag()
        {
            if (_pressedSeat is null)
            {
                return;
            }

            _pressedSeat.ReleaseMouseCapture();

            if (_draggingSeat is not null)
            {
                Panel.SetZIndex(_draggingSeat, 0);
            }

            _pressedSeat = null;
            _draggingSeat = null;
        }

        // Seat 또는 Lack의 위치를 맵 경계 안으로 제한해 배치한다.
        private void SetSeatPosition(Canvas mapCanvas, Border seat, double left, double top)
        {
            // 좌표를 제한해 항목이 맵 바깥으로 나가지 않게 한다.
            var footprint = GetSeatFootprint(seat);
            var boundedLeft = Math.Max(0, Math.Min(mapCanvas.Width - footprint.Width, left));
            var boundedTop = Math.Max(0, Math.Min(mapCanvas.Height - footprint.Height, top));

            Canvas.SetLeft(seat, boundedLeft);
            Canvas.SetTop(seat, boundedTop);
        }

        // 특정 Seat 또는 Lack가 속한 부모 Canvas를 찾는다.
        private static Canvas? GetSeatCanvas(Border seat)
        {
            return seat.Parent as Canvas;
        }

        // Place 모드에서만 우클릭 메뉴를 열어 삭제 동작을 제공한다.
        private void Seat_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_currentMode != EditorMode.Place || sender is not Border seat)
            {
                return;
            }

            if (ReferenceEquals(_highlightedSeat, seat))
            {
                StopSeatHighlight();
            }

            CancelSeatDrag();
            ShowSeatActionsMenu(seat);
            e.Handled = true;
        }

        // Place 모드에서 우클릭한 항목에 대한 동작 메뉴를 연다.
        private void ShowSeatActionsMenu(Border seat)
        {
            var contextMenu = new ContextMenu
            {
                PlacementTarget = seat,
                Placement = PlacementMode.MousePoint
            };

            var rotateMenuItem = new MenuItem
            {
                Header = "Rotate 90°",
                Tag = seat
            };
            rotateMenuItem.Click += RotateSeatMenuItem_Click;

            var deleteMenuItem = new MenuItem
            {
                Header = "Delete",
                Tag = seat
            };
            deleteMenuItem.Click += DeleteSeatMenuItem_Click;

            contextMenu.Items.Add(rotateMenuItem);
            contextMenu.Items.Add(deleteMenuItem);
            contextMenu.Closed += (_, _) =>
            {
                if (ReferenceEquals(seat.ContextMenu, contextMenu))
                {
                    seat.ContextMenu = null;
                }
            };
            seat.ContextMenu = contextMenu;
            contextMenu.IsOpen = true;
        }

        // 동작 메뉴에서 Rotate를 선택한 항목을 90도 회전한다.
        private void RotateSeatMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not Border seat)
            {
                return;
            }

            RotateSeat(seat);
        }

        // 동작 메뉴에서 Delete를 선택한 항목을 현재 맵에서 제거한다.
        private void DeleteSeatMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not Border seat)
            {
                return;
            }

            GetSeatCanvas(seat)?.Children.Remove(seat);
        }

        // 지정한 항목을 중심점 기준으로 90도 회전시키고 맵 경계 안으로 다시 맞춘다.
        private void RotateSeat(Border seat)
        {
            var mapCanvas = GetSeatCanvas(seat);
            if (mapCanvas is null)
            {
                return;
            }

            var currentFootprint = GetSeatFootprint(seat);
            var rotateTransform = GetOrCreateRotateTransform(seat);
            var currentLeft = Canvas.GetLeft(seat);
            var currentTop = Canvas.GetTop(seat);
            var centerX = currentLeft + (currentFootprint.Width / 2);
            var centerY = currentTop + (currentFootprint.Height / 2);
            rotateTransform.Angle = (rotateTransform.Angle + 90) % 360;

            var rotatedFootprint = GetSeatFootprint(seat);
            SetSeatPosition(mapCanvas, seat, centerX - (rotatedFootprint.Width / 2), centerY - (rotatedFootprint.Height / 2));
        }

        // 회전에 사용할 RotateTransform을 가져오거나 없으면 새로 만든다.
        private static RotateTransform GetOrCreateRotateTransform(Border seat)
        {
            seat.RenderTransformOrigin = new Point(0.5, 0.5);

            if (seat.RenderTransform is TransformGroup transformGroup)
            {
                foreach (var child in transformGroup.Children)
                {
                    if (child is RotateTransform rotateTransform)
                    {
                        return rotateTransform;
                    }
                }

                var createdRotateTransform = new RotateTransform(0);
                transformGroup.Children.Add(createdRotateTransform);
                return createdRotateTransform;
            }

            if (seat.RenderTransform is RotateTransform existingRotateTransform)
            {
                return existingRotateTransform;
            }

            var rotateOnlyTransform = new RotateTransform(0);
            seat.RenderTransform = rotateOnlyTransform;
            return rotateOnlyTransform;
        }

        private static Size GetSeatFootprint(Border seat)
        {
            var angle = NormalizeSeatAngle(seat);
            var isQuarterTurn = angle is 90 or 270;
            return isQuarterTurn
                ? new Size(seat.Height, seat.Width)
                : new Size(seat.Width, seat.Height);
        }

        private static int NormalizeSeatAngle(Border seat)
        {
            double angle = 0;

            if (seat.RenderTransform is RotateTransform rotateTransform)
            {
                angle = rotateTransform.Angle;
            }
            else if (seat.RenderTransform is TransformGroup transformGroup)
            {
                foreach (var child in transformGroup.Children)
                {
                    if (child is RotateTransform childRotateTransform)
                    {
                        angle = childRotateTransform.Angle;
                        break;
                    }
                }
            }

            return ((int)Math.Round(angle) % 360 + 360) % 360;
        }
    }
}
