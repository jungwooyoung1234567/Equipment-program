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
        // 검색으로 강조 중인 좌석을 기억해 중복 애니메이션을 정리한다.
        private Border? _highlightedSeat;

        // 장비 검색창을 열고 선택된 결과가 있으면 해당 좌석으로 이동한다.
        private void FindEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
            // 검색창을 열기 전에 모든 맵의 장비를 하나의 검색 목록으로 만든다.
            var searchIndex = BuildEquipmentSearchIndex();
            if (searchIndex.Count == 0)
            {
                MessageBox.Show(
                    "There is no equipment to search yet.",
                    "Find Equipment",
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

        // 지정한 Seat 또는 Lack의 편집창을 열고 저장 결과를 화면에 반영한다.
        private void OpenSeatEditor(Border seat)
        {
            // Border.Tag에 저장된 SeatInfo를 편집 대상으로 사용한다.
            if (seat.Tag is not SeatInfo seatInfo)
            {
                return;
            }

            var editorWindow = new SeatEditorWindow(seatInfo)
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

        // 모든 맵과 모든 좌석의 장비를 순회해 검색용 목록을 만든다.
        private List<EquipmentSearchResult> BuildEquipmentSearchIndex()
        {
            var results = new List<EquipmentSearchResult>();

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

                    foreach (var equipment in seatInfo.Equipments)
                    {
                        results.Add(new EquipmentSearchResult(mapIndex, seat, seatInfo, equipment));
                    }
                }
            }

            return results;
        }

        // 검색 결과가 가리키는 맵으로 이동한 뒤 해당 좌석 위치를 바로 확인할 수 있게 한다.
        private void NavigateToEquipment(EquipmentSearchResult result)
        {
            CancelSeatDrag();
            _activeMapIndex = result.MapIndex;
            UpdateMapUi();
            BringSeatIntoView(result.Seat);
            StartSeatHighlight(result.Seat);
        }

        // Seat 또는 Lack에 표시되는 이름, 팀, 장비 개수 문구를 갱신한다.
        private void UpdateSeatDisplay(Border seat, SeatInfo seatInfo)
        {
            // 표시용 텍스트는 StackPanel 안의 두 TextBlock을 직접 수정한다.
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
                var teamLabel = string.IsNullOrWhiteSpace(seatInfo.TeamName) ? "Unassigned" : seatInfo.TeamName;
                var equipmentCount = seatInfo.Equipments.Count;
                teamText.Text = equipmentCount > 0
                    ? $"{teamLabel} | Eq {equipmentCount}"
                    : teamLabel;
            }

            seat.ToolTip = $"{seatInfo.Equipments.Count} equipment item(s)";
        }

        // 검색으로 이동한 좌석이 스크롤 영역 안에 들어오도록 위치를 맞춘다.
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

        // 검색으로 찾은 좌석 강조를 시작하고 기존 강조가 있으면 먼저 정리한다.
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

        // 현재 강조 중인 좌석 애니메이션을 멈추고 기본 상태로 되돌린다.
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

        // 강조 애니메이션에 사용할 ScaleTransform을 가져오거나 없으면 새로 만든다.
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

        // 현재 항목에 ScaleTransform이 있으면 찾아서 반환한다.
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
    }
}
