using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace UL_project
{
    public partial class MainWindow
    {
        // 편집기는 배치 모드와 편집 모드, 두 가지 모드를 가진다.
        private enum EditorMode
        {
            Place,
            Edit
        }

        // 앱은 배치 모드에서 시작한다.
        private EditorMode _currentMode = EditorMode.Place;

        // 배치 모드로 전환하고 관련 UI를 갱신한다.
        private void PlaceModeButton_Click(object sender, RoutedEventArgs e)
        {
            _currentMode = EditorMode.Place;
            UpdateModeUi();
        }

        // 편집 모드로 전환하고 진행 중인 드래그를 정리한다.
        private void EditModeButton_Click(object sender, RoutedEventArgs e)
        {
            // 모드를 바꾸기 전에 진행 중인 드래그를 정리한다.
            CancelSeatDrag();
            _currentMode = EditorMode.Edit;
            UpdateModeUi();
        }

        // 현재 모드에 맞게 버튼, 팔레트, 안내 문구를 갱신한다.
        private void UpdateModeUi()
        {
            var isPlaceMode = _currentMode == EditorMode.Place;

            // 현재 선택된 모드 버튼을 강조 표시한다.
            PlaceModeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isPlaceMode ? "#F6B73C" : "#D7DCE8"));
            EditModeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isPlaceMode ? "#D7DCE8" : "#F6B73C"));
            PlaceModeButton.BorderBrush = Brushes.Transparent;
            EditModeButton.BorderBrush = Brushes.Transparent;

            // 배치 모드에서만 팔레트에서 새 항목을 끌어다 놓을 수 있다.
            SeatTemplate.Opacity = isPlaceMode ? 1.0 : 0.45;
            SeatTemplate.Cursor = isPlaceMode ? Cursors.Hand : Cursors.No;
            LackTemplate.Opacity = isPlaceMode ? 1.0 : 0.45;
            LackTemplate.Cursor = isPlaceMode ? Cursors.Hand : Cursors.No;
            CartTemplate.Opacity = isPlaceMode ? 1.0 : 0.45;
            CartTemplate.Cursor = isPlaceMode ? Cursors.Hand : Cursors.No;

            ModeDescriptionText.Text = isPlaceMode
                ? "Place mode: create and move seats, lacks, and carts. Click an item to open actions such as delete."
                : "Edit mode: click a seat to edit properties. Moving is disabled.";
        }
    }
}
