using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace UL_project
{
    public partial class MainWindow
    {
        private Border? _highlightedSeat;

        private void FindEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            var searchIndex = BuildEquipmentSearchIndex();
            if (searchIndex.Count == 0)
            {
                MessageBox.Show(
                    "아직 검색할 장비가 없습니다.",
                    "장비 검색",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var findWindow = new EquipmentFindWindow(searchIndex)
            {
                Owner = this
            };

            if (findWindow.ShowDialog() != true || findWindow.SelectedResult is null)
            {
                return;
            }

            NavigateToEquipment(findWindow.SelectedResult);
        }

        private void OpenSeatEditor(Border seat)
        {
            if (seat.Tag is not SeatInfo seatInfo)
            {
                return;
            }

            var editorWindow = new SeatEditorWindow(seatInfo, GetItemTypeDisplayName(seat.Uid))
            {
                Owner = this
            };

            if (editorWindow.ShowDialog() != true)
            {
                return;
            }

            seatInfo.SeatName = string.IsNullOrWhiteSpace(editorWindow.SeatName) ? seatInfo.SeatName : editorWindow.SeatName;
            seatInfo.TeamName = editorWindow.TeamName;
            seatInfo.Equipments = editorWindow.Equipments;

            UpdateSeatDisplay(seat, seatInfo);
        }

        private List<EquipmentSearchResult> BuildEquipmentSearchIndex()
        {
            var results = new List<EquipmentSearchResult>();
            var resultKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var mapIndex = 0; mapIndex < _mapCanvases.Count; mapIndex++)
            {
                var mapCanvas = _mapCanvases[mapIndex];
                var seats = mapCanvas.Children.OfType<Border>();

                foreach (var seat in seats)
                {
                    if (seat.Tag is not SeatInfo seatInfo)
                    {
                        continue;
                    }

                    for (var equipmentIndex = 0; equipmentIndex < seatInfo.Equipments.Count; equipmentIndex++)
                    {
                        var equipment = seatInfo.Equipments[equipmentIndex];
                        var resultKey = BuildEquipmentResultKey(equipment);
                        if (!resultKeys.Add(resultKey))
                        {
                            continue;
                        }

                        results.Add(new EquipmentSearchResult(
                            mapIndex,
                            _mapNames[mapIndex],
                            seat,
                            seatInfo,
                            equipment,
                            equipmentIndex));
                    }
                }
            }

            return results;
        }

        private static string BuildEquipmentResultKey(EquipmentInfo equipment)
        {
            return string.Join(
                "|",
                equipment.Name.Trim(),
                equipment.UlNumber.Trim(),
                equipment.GlobalNumber.Trim());
        }

        private void NavigateToEquipment(EquipmentSearchResult result)
        {
            CancelSeatDrag();
            _activeMapIndex = result.MapIndex;
            UpdateMapUi();
            BringSeatIntoView(result.Seat);
            StartSeatHighlight(result.Seat);
            OpenSeatEditor(result.Seat, result.EquipmentIndex);
        }

        private void OpenSeatEditor(Border seat, int selectedEquipmentIndex)
        {
            if (seat.Tag is not SeatInfo seatInfo)
            {
                return;
            }

            var editorWindow = new SeatEditorWindow(seatInfo, GetItemTypeDisplayName(seat.Uid), selectedEquipmentIndex)
            {
                Owner = this
            };

            if (editorWindow.ShowDialog() != true)
            {
                return;
            }

            seatInfo.SeatName = string.IsNullOrWhiteSpace(editorWindow.SeatName) ? seatInfo.SeatName : editorWindow.SeatName;
            seatInfo.TeamName = editorWindow.TeamName;
            seatInfo.Equipments = editorWindow.Equipments;

            UpdateSeatDisplay(seat, seatInfo);
        }

        private void UpdateSeatDisplay(Border seat, SeatInfo seatInfo)
        {
            var content = GetSeatContentPanel(seat);
            if (content is null || content.Children.Count < 2)
            {
                return;
            }

            if (content.Children[0] is TextBlock nameText)
            {
                nameText.Text = seatInfo.SeatName;
            }

            if (content.Children[1] is TextBlock teamText)
            {
                var teamLabel = string.IsNullOrWhiteSpace(seatInfo.TeamName) ? "미지정" : seatInfo.TeamName;
                var equipmentCount = seatInfo.Equipments.Count;
                teamText.Text = equipmentCount > 0
                    ? $"{teamLabel} | 장비 {equipmentCount}"
                    : teamLabel;
            }

            seat.ToolTip = $"장비 {seatInfo.Equipments.Count}개";
        }

        private static StackPanel? GetSeatContentPanel(Border seat)
        {
            if (seat.Child is StackPanel directPanel)
            {
                return directPanel;
            }

            if (seat.Child is Grid root)
            {
                foreach (var child in root.Children)
                {
                    if (child is StackPanel nestedPanel)
                    {
                        return nestedPanel;
                    }
                }
            }

            return null;
        }

        private void BringSeatIntoView(Border seat)
        {
            if (GetSeatCanvas(seat) is null)
            {
                return;
            }

            MapScrollViewer.UpdateLayout();

            var left = Canvas.GetLeft(seat);
            var top = Canvas.GetTop(seat);
            var targetHorizontalOffset = Math.Max(0, left - ((MapScrollViewer.ViewportWidth - seat.ActualWidth) / 2));
            var targetVerticalOffset = Math.Max(0, top - ((MapScrollViewer.ViewportHeight - seat.ActualHeight) / 2));

            MapScrollViewer.ScrollToHorizontalOffset(targetHorizontalOffset);
            MapScrollViewer.ScrollToVerticalOffset(targetVerticalOffset);
        }

        private void StartSeatHighlight(Border seat)
        {
            StopSeatHighlight();
            _highlightedSeat = seat;

            var scaleTransform = GetOrCreateScaleTransform(seat);

            var scaleAnimation = new DoubleAnimation
            {
                From = 1,
                To = 1.14,
                Duration = TimeSpan.FromMilliseconds(180),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            var opacityAnimation = new DoubleAnimation
            {
                From = 1,
                To = 0.55,
                Duration = TimeSpan.FromMilliseconds(180),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };

            scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnimation);
            scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnimation);
            seat.BeginAnimation(UIElement.OpacityProperty, opacityAnimation);
        }

        private void StopSeatHighlight()
        {
            if (_highlightedSeat is null)
            {
                return;
            }

            if (TryGetScaleTransform(_highlightedSeat, out var scaleTransform) &&
                scaleTransform is not null)
            {
                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                scaleTransform.ScaleX = 1;
                scaleTransform.ScaleY = 1;
            }

            _highlightedSeat.BeginAnimation(UIElement.OpacityProperty, null);
            _highlightedSeat.Opacity = 1;
            _highlightedSeat = null;
        }

        private static ScaleTransform GetOrCreateScaleTransform(Border seat)
        {
            seat.RenderTransformOrigin = new Point(0.5, 0.5);

            if (seat.RenderTransform is TransformGroup transformGroup)
            {
                foreach (var child in transformGroup.Children)
                {
                    if (child is ScaleTransform scaleTransform)
                    {
                        return scaleTransform;
                    }
                }

                var createdScaleTransform = new ScaleTransform(1, 1);
                transformGroup.Children.Insert(0, createdScaleTransform);
                return createdScaleTransform;
            }

            if (seat.RenderTransform is ScaleTransform existingScaleTransform)
            {
                return existingScaleTransform;
            }

            if (seat.RenderTransform is RotateTransform rotateTransform)
            {
                var combinedTransformGroup = new TransformGroup();
                var createdScaleTransform = new ScaleTransform(1, 1);
                combinedTransformGroup.Children.Add(createdScaleTransform);
                combinedTransformGroup.Children.Add(rotateTransform);
                seat.RenderTransform = combinedTransformGroup;
                return createdScaleTransform;
            }

            var scaleOnlyTransform = new ScaleTransform(1, 1);
            seat.RenderTransform = scaleOnlyTransform;
            return scaleOnlyTransform;
        }

        private static bool TryGetScaleTransform(Border seat, out ScaleTransform? scaleTransform)
        {
            if (seat.RenderTransform is ScaleTransform directScaleTransform)
            {
                scaleTransform = directScaleTransform;
                return true;
            }

            if (seat.RenderTransform is TransformGroup transformGroup)
            {
                foreach (var child in transformGroup.Children)
                {
                    if (child is ScaleTransform childScaleTransform)
                    {
                        scaleTransform = childScaleTransform;
                        return true;
                    }
                }
            }

            scaleTransform = null;
            return false;
        }

        private static string GetItemTypeDisplayName(string? typeId)
        {
            if (string.IsNullOrWhiteSpace(typeId) || string.Equals(typeId, SeatItemType, StringComparison.OrdinalIgnoreCase))
            {
                return "테이블";
            }

            if (string.Equals(typeId, LackItemType, StringComparison.OrdinalIgnoreCase))
            {
                return "선반";
            }

            if (string.Equals(typeId, CartItemType, StringComparison.OrdinalIgnoreCase))
            {
                return "카트";
            }

            return typeId ?? string.Empty;
        }
    }
}
