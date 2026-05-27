using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace UL_project
{
    public partial class MainWindow
    {
        private enum EditorMode
        {
            Place,
            Edit
        }

        private EditorMode _currentMode = EditorMode.Place;

        private void PlaceModeButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = EditorMode.Place;
            UpdateModeUi();
        }

        private void EditModeButton_Click(object sender, RoutedEventArgs e)
        {
            CancelSeatDrag();
            _currentMode = EditorMode.Edit;
            UpdateModeUi();
        }

        private void UpdateModeUi()
        {
            var isPlaceMode = _currentMode == EditorMode.Place;

            PlaceModeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isPlaceMode ? "#F6B73C" : "#D7DCE8"));
            EditModeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isPlaceMode ? "#D7DCE8" : "#F6B73C"));
            PlaceModeButton.BorderBrush = Brushes.Transparent;
            EditModeButton.BorderBrush = Brushes.Transparent;

            SeatTemplate.Opacity = isPlaceMode ? 1.0 : 0.45;
            SeatTemplate.Cursor = isPlaceMode ? Cursors.Hand : Cursors.No;
            LackTemplate.Opacity = isPlaceMode ? 1.0 : 0.45;
            LackTemplate.Cursor = isPlaceMode ? Cursors.Hand : Cursors.No;
            CartTemplate.Opacity = isPlaceMode ? 1.0 : 0.45;
            CartTemplate.Cursor = isPlaceMode ? Cursors.Hand : Cursors.No;

            UpdateResizeHandleVisibility();

            ModeDescriptionText.Text = isPlaceMode
                ? "배치 모드: 테이블, 선반, 카트를 생성하고 이동, 크기 조절, 회전할 수 있습니다."
                : "편집 모드: 기구를 클릭해 장비관리를 실행합니다. 이동은 비활성화됩니다.";
        }
    }
}
