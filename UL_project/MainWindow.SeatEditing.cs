using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace UL_project
{
    public partial class MainWindow
    {
        private void FindEquipmentButton_Click(object sender, RoutedEventArgs e)
        {
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

        private void OpenSeatEditor(Border seat)
        {
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

        private void NavigateToEquipment(EquipmentSearchResult result)
        {
            CancelSeatDrag();
            _activeMapIndex = result.MapIndex;
            UpdateMapUi();
            OpenSeatEditor(result.Seat);
        }

        private void UpdateSeatDisplay(Border seat, SeatInfo seatInfo)
        {
            if (seat.Child is not StackPanel content || content.Children.Count < 2)
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
    }
}
