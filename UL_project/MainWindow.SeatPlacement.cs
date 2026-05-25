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
        private const string SeatDragFormat = "UL_project.SeatTemplate";
        private const string LackDragFormat = "UL_project.LackTemplate";
        private const string CartDragFormat = "UL_project.CartTemplate";

        private const double SeatWidth = 120;
        private const double SeatHeight = 70;
        private const double LackWidth = SeatWidth * 3;
        private const double LackHeight = 70;
        private const double CartWidth = SeatWidth / 2;
        private const double CartHeight = SeatHeight;
        private const double MinimumItemWidth = 40;
        private const double MinimumItemHeight = 40;
        private const double ResizeEdgeThreshold = 12;

        private Border? _draggingSeat;
        private Border? _pressedSeat;
        private Border? _selectedSeat;
        private Canvas? _pasteTargetCanvas;
        private bool _isResizingSeat;
        private Point _seatDragOffset;
        private Point _seatPointerDownPosition;
        private Point _pasteTargetPoint;
        private Size _resizeStartFootprint;

        private string? _copiedSeatName;
        private string? _copiedTeamName;
        private string? _copiedItemType;

        private int _seatCounter = 1;
        private int _lackCounter = 1;
        private int _cartCounter = 1;

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

        private void MapCanvas_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = _currentMode == EditorMode.Place &&
                (e.Data.GetDataPresent(SeatDragFormat) || e.Data.GetDataPresent(LackDragFormat) || e.Data.GetDataPresent(CartDragFormat))
                ? DragDropEffects.Copy
                : DragDropEffects.None;
            e.Handled = true;
        }

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

        private void AddSeat(Canvas mapCanvas, Point dropPoint)
        {
            var seat = BuildSeatElement($"Seat {_seatCounter++}");
            mapCanvas.Children.Add(seat);
            SetSeatPosition(mapCanvas, seat, dropPoint.X - SeatWidth / 2, dropPoint.Y - SeatHeight / 2);
        }

        private void AddLack(Canvas mapCanvas, Point dropPoint)
        {
            var lack = BuildLackElement($"Lack {_lackCounter++}");
            mapCanvas.Children.Add(lack);
            SetSeatPosition(mapCanvas, lack, dropPoint.X - LackWidth / 2, dropPoint.Y - LackHeight / 2);
        }

        private void AddCart(Canvas mapCanvas, Point dropPoint)
        {
            var cart = BuildCartElement($"Cart {_cartCounter++}");
            mapCanvas.Children.Add(cart);
            SetSeatPosition(mapCanvas, cart, dropPoint.X - CartWidth / 2, dropPoint.Y - CartHeight / 2);
        }

        private Border CreateNewItemByType(string itemType)
        {
            return itemType switch
            {
                LackItemType => BuildLackElement($"Lack {_lackCounter++}"),
                CartItemType => BuildCartElement($"Cart {_cartCounter++}"),
                _ => BuildSeatElement($"Seat {_seatCounter++}")
            };
        }

        private Border BuildSeatElement(string title)
        {
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

            seat.Child = BuildItemContent(
                seat,
                seatInfo.SeatName,
                "Unassigned",
                16,
                11,
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2B2112")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#5C4622")),
                wrapTitle: false,
                titleAlignment: TextAlignment.Center);

            AttachSeatEvents(seat);
            return seat;
        }

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

            lack.Child = BuildItemContent(
                lack,
                lackInfo.SeatName,
                lackInfo.TeamName,
                16,
                11,
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#28310F")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4B561B")),
                wrapTitle: false,
                titleAlignment: TextAlignment.Center);

            AttachSeatEvents(lack);
            return lack;
        }

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

            cart.Child = BuildItemContent(
                cart,
                cartInfo.SeatName,
                cartInfo.TeamName,
                12,
                10,
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#103652")),
                new SolidColorBrush((Color)ColorConverter.ConvertFromString("#24506F")),
                wrapTitle: true,
                titleAlignment: TextAlignment.Center);

            AttachSeatEvents(cart);
            return cart;
        }

        private void AttachSeatEvents(Border seat)
        {
            seat.ToolTip = "0 equipment item(s)";
            seat.MouseLeftButtonDown += Seat_MouseLeftButtonDown;
            seat.MouseMove += Seat_MouseMove;
            seat.MouseLeftButtonUp += Seat_MouseLeftButtonUp;
            seat.MouseRightButtonUp += Seat_MouseRightButtonUp;
        }

        private Grid BuildItemContent(
            Border seat,
            string title,
            string subtitle,
            double titleFontSize,
            double subtitleFontSize,
            Brush titleForeground,
            Brush subtitleForeground,
            bool wrapTitle,
            TextAlignment titleAlignment)
        {
            var root = new Grid();

            var content = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            content.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = titleFontSize,
                FontWeight = FontWeights.Bold,
                Foreground = titleForeground,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextWrapping = wrapTitle ? TextWrapping.Wrap : TextWrapping.NoWrap,
                TextAlignment = titleAlignment
            });

            content.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = subtitleFontSize,
                Foreground = subtitleForeground,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            root.Children.Add(content);
            return root;
        }

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

            SelectSeat(seat);

            if (_currentMode == EditorMode.Edit)
            {
                OpenSeatEditor(seat);
                return;
            }

            _pressedSeat = seat;
            var seatCanvas = GetSeatCanvas(seat);
            if (seatCanvas is null)
            {
                return;
            }

            var pointerPosition = e.GetPosition(seatCanvas);
            if (IsResizeHit(seat, e.GetPosition(seat)))
            {
                _pressedSeat = seat;
                _isResizingSeat = true;
                _seatPointerDownPosition = pointerPosition;
                _resizeStartFootprint = GetSeatFootprint(seat);
                seat.CaptureMouse();
                return;
            }

            _seatDragOffset = new Point(
                pointerPosition.X - Canvas.GetLeft(seat),
                pointerPosition.Y - Canvas.GetTop(seat));
            _seatPointerDownPosition = pointerPosition;
            seat.CaptureMouse();
        }

        private void Seat_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentMode != EditorMode.Place || _pressedSeat is null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (_isResizingSeat)
            {
                var resizeSeatCanvas = GetSeatCanvas(_pressedSeat);
                if (resizeSeatCanvas is null)
                {
                    return;
                }

                var currentPosition = e.GetPosition(resizeSeatCanvas);
                ResizeSeatByDelta(
                    _pressedSeat,
                    currentPosition.X - _seatPointerDownPosition.X,
                    currentPosition.Y - _seatPointerDownPosition.Y,
                    _resizeStartFootprint);
                return;
            }

            if (_draggingSeat is null)
            {
                var pressedSeatCanvas = GetSeatCanvas(_pressedSeat);
                if (pressedSeatCanvas is null)
                {
                    return;
                }

                var currentPosition = e.GetPosition(pressedSeatCanvas);
                var horizontalDistance = Math.Abs(currentPosition.X - _seatPointerDownPosition.X);
                var verticalDistance = Math.Abs(currentPosition.Y - _seatPointerDownPosition.Y);

                if (horizontalDistance < SystemParameters.MinimumHorizontalDragDistance &&
                    verticalDistance < SystemParameters.MinimumVerticalDragDistance)
                {
                    return;
                }

                _draggingSeat = _pressedSeat;
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

        private void Seat_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_pressedSeat is null)
            {
                return;
            }

            _pressedSeat.ReleaseMouseCapture();
            _isResizingSeat = false;

            if (_draggingSeat is not null)
            {
                Panel.SetZIndex(_draggingSeat, 0);
            }

            _pressedSeat = null;
            _draggingSeat = null;
        }

        private void CancelSeatDrag()
        {
            if (_pressedSeat is null)
            {
                return;
            }

            _pressedSeat.ReleaseMouseCapture();
            _isResizingSeat = false;

            if (_draggingSeat is not null)
            {
                Panel.SetZIndex(_draggingSeat, 0);
            }

            _pressedSeat = null;
            _draggingSeat = null;
        }

        private void SetSeatPosition(Canvas mapCanvas, Border seat, double left, double top)
        {
            var footprint = GetSeatFootprint(seat);
            var boundedLeft = Math.Max(0, Math.Min(mapCanvas.Width - footprint.Width, left));
            var boundedTop = Math.Max(0, Math.Min(mapCanvas.Height - footprint.Height, top));

            Canvas.SetLeft(seat, boundedLeft);
            Canvas.SetTop(seat, boundedTop);
        }

        private static Canvas? GetSeatCanvas(Border seat)
        {
            return seat.Parent as Canvas;
        }

        private void MapCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Canvas mapCanvas || e.OriginalSource is not Canvas)
            {
                return;
            }

            _pasteTargetCanvas = mapCanvas;
            _pasteTargetPoint = e.GetPosition(mapCanvas);
            _selectedSeat = null;
        }

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

            SelectSeat(seat);
            CancelSeatDrag();
            ShowSeatActionsMenu(seat);
            e.Handled = true;
        }

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

            var resizeMenuItem = new MenuItem
            {
                Header = "Resize...",
                Tag = seat
            };
            resizeMenuItem.Click += ResizeSeatMenuItem_Click;

            contextMenu.Items.Add(rotateMenuItem);
            contextMenu.Items.Add(resizeMenuItem);
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

        private void RotateSeatMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not Border seat)
            {
                return;
            }

            RotateSeat(seat);
        }

        private void DeleteSeatMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not Border seat)
            {
                return;
            }

            if (ReferenceEquals(_selectedSeat, seat))
            {
                _selectedSeat = null;
            }

            GetSeatCanvas(seat)?.Children.Remove(seat);
        }

        private void ResizeSeatMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not MenuItem menuItem || menuItem.Tag is not Border seat)
            {
                return;
            }

            OpenResizeDialog(seat);
        }

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

        private void OpenResizeDialog(Border seat)
        {
            var currentSize = GetSeatFootprint(seat);
            var resizeWindow = new SeatSizeWindow(currentSize.Width, currentSize.Height)
            {
                Owner = this
            };

            if (resizeWindow.ShowDialog() != true)
            {
                return;
            }

            ResizeSeatTo(seat, resizeWindow.ItemWidth, resizeWindow.ItemHeight);
        }

        private void ResizeSeatTo(Border seat, double targetWidth, double targetHeight)
        {
            var mapCanvas = GetSeatCanvas(seat);
            if (mapCanvas is null)
            {
                return;
            }

            var previousFootprint = GetSeatFootprint(seat);
            var centerX = Canvas.GetLeft(seat) + (previousFootprint.Width / 2);
            var centerY = Canvas.GetTop(seat) + (previousFootprint.Height / 2);
            var angle = NormalizeSeatAngle(seat);
            var boundedWidth = Math.Max(MinimumItemWidth, targetWidth);
            var boundedHeight = Math.Max(MinimumItemHeight, targetHeight);

            if (angle is 90 or 270)
            {
                seat.Width = boundedHeight;
                seat.Height = boundedWidth;
            }
            else
            {
                seat.Width = boundedWidth;
                seat.Height = boundedHeight;
            }

            var resizedFootprint = GetSeatFootprint(seat);
            SetSeatPosition(mapCanvas, seat, centerX - (resizedFootprint.Width / 2), centerY - (resizedFootprint.Height / 2));
        }

        private void ResizeSeatByDelta(Border seat, double horizontalChange, double verticalChange, Size startFootprint)
        {
            var angle = NormalizeSeatAngle(seat);
            var resizeSensitivity = GetResizeSensitivity(startFootprint);
            var (widthDelta, heightDelta) = MapResizeDelta(
                angle,
                horizontalChange * resizeSensitivity,
                verticalChange * resizeSensitivity);
            var maxDragSize = GetMaximumDragResizeSize(seat);

            ResizeSeatTo(
                seat,
                Math.Min(maxDragSize.Width, startFootprint.Width + widthDelta),
                Math.Min(maxDragSize.Height, startFootprint.Height + heightDelta));
        }

        private static RotateTransform GetOrCreateRotateTransform(Border seat)
        {
            if (seat.LayoutTransform is RotateTransform existingRotateTransform)
            {
                return existingRotateTransform;
            }

            var rotateOnlyTransform = new RotateTransform(0);
            seat.LayoutTransform = rotateOnlyTransform;
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

            if (seat.LayoutTransform is RotateTransform rotateTransform)
            {
                angle = rotateTransform.Angle;
            }

            return ((int)Math.Round(angle) % 360 + 360) % 360;
        }

        private void UpdateResizeHandleVisibility()
        {
            // Drag resize now uses the outer edge of the shape instead of a visible handle.
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == 0 || IsTextInputFocused())
            {
                return;
            }

            if (e.Key == Key.C)
            {
                CopySelectedSeatLabel();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.V)
            {
                PasteSelectedSeatLabel();
                e.Handled = true;
            }
        }

        private void CopySelectedSeatLabel()
        {
            if (_selectedSeat?.Tag is not SeatInfo seatInfo)
            {
                return;
            }

            _copiedSeatName = seatInfo.SeatName;
            _copiedTeamName = seatInfo.TeamName;
            _copiedItemType = string.IsNullOrWhiteSpace(_selectedSeat.Uid) ? SeatItemType : _selectedSeat.Uid;
        }

        private void PasteSelectedSeatLabel()
        {
            if (_copiedSeatName is null || _copiedItemType is null || _pasteTargetCanvas is null)
            {
                return;
            }

            var newItem = CreateNewItemByType(_copiedItemType);
            if (newItem.Tag is not SeatInfo seatInfo)
            {
                return;
            }

            seatInfo.SeatName = _copiedSeatName;
            seatInfo.TeamName = _copiedTeamName ?? string.Empty;
            seatInfo.Equipments.Clear();
            UpdateSeatDisplay(newItem, seatInfo);

            _pasteTargetCanvas.Children.Add(newItem);
            SetSeatPosition(
                _pasteTargetCanvas,
                newItem,
                _pasteTargetPoint.X - (GetSeatFootprint(newItem).Width / 2),
                _pasteTargetPoint.Y - (GetSeatFootprint(newItem).Height / 2));

            SelectSeat(newItem);
        }

        private void SelectSeat(Border seat)
        {
            _selectedSeat = seat;
        }

        private bool IsTextInputFocused()
        {
            return Keyboard.FocusedElement is TextBox;
        }

        private static (double WidthDelta, double HeightDelta) MapResizeDelta(int angle, double horizontalChange, double verticalChange)
        {
            return angle switch
            {
                90 => (verticalChange, -horizontalChange),
                180 => (-horizontalChange, -verticalChange),
                270 => (-verticalChange, horizontalChange),
                _ => (horizontalChange, verticalChange)
            };
        }

        private static double GetResizeSensitivity(Size footprint)
        {
            var shorterEdge = Math.Max(1, Math.Min(footprint.Width, footprint.Height));
            var aspectRatio = Math.Max(footprint.Width, footprint.Height) / shorterEdge;

            if (aspectRatio >= 4)
            {
                return 0.08;
            }

            if (aspectRatio >= 2)
            {
                return 0.12;
            }

            return 0.18;
        }

        private static bool IsResizeHit(Border seat, Point localPosition)
        {
            var width = Math.Max(seat.ActualWidth, seat.Width);
            var height = Math.Max(seat.ActualHeight, seat.Height);
            return localPosition.X <= ResizeEdgeThreshold ||
                localPosition.X >= width - ResizeEdgeThreshold ||
                localPosition.Y <= ResizeEdgeThreshold ||
                localPosition.Y >= height - ResizeEdgeThreshold;
        }

        private static Size GetMaximumDragResizeSize(Border seat)
        {
            return seat.Uid switch
            {
                LackItemType => new Size(520, 220),
                CartItemType => new Size(180, 180),
                _ => new Size(260, 180)
            };
        }
    }
}
