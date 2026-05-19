using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace UL_project
{
    public partial class MainWindow
    {
        // 현재 편집기가 어떤 동작 모드인지 구분한다.
        private enum EditorMode
        {
            Place,
            Edit
        }

        // 앱 시작 시 기본 모드는 Place이다.
        private EditorMode _currentMode = EditorMode.Place;

        private void PlaceModeButton_Click(object sender, RoutedEventArgs e)
        {
            // Place 모드로 바꾸고 UI도 함께 갱신한다.
            _currentMode = EditorMode.Place;
            UpdateModeUi();
        }

        private void EditModeButton_Click(object sender, RoutedEventArgs e)
        {
            // 모드를 바꾸기 전에 현재 드래그 중인 아이템이 있다면 정리한다.
            CancelSeatDrag();
            _currentMode = EditorMode.Edit;
            UpdateModeUi();
        }

        private void UpdateModeUi()
        {
            // 현재 모드가 Place인지 먼저 계산해두고 아래 UI 갱신에서 재사용한다.
            var isPlaceMode = _currentMode == EditorMode.Place;

            // 모드 버튼 색상을 바꿔서 현재 선택 상태를 보여준다.
            PlaceModeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isPlaceMode ? "#F6B73C" : "#D7DCE8"));
            EditModeButton.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isPlaceMode ? "#D7DCE8" : "#F6B73C"));
            PlaceModeButton.BorderBrush = Brushes.Transparent;
            EditModeButton.BorderBrush = Brushes.Transparent;

            // Place 모드에서만 새 Seat/Lack를 끌어다 놓을 수 있도록 시각 상태를 조정한다.
            SeatTemplate.Opacity = isPlaceMode ? 1.0 : 0.45;
            SeatTemplate.Cursor = isPlaceMode ? Cursors.Hand : Cursors.No;
            LackTemplate.Opacity = isPlaceMode ? 1.0 : 0.45;
            LackTemplate.Cursor = isPlaceMode ? Cursors.Hand : Cursors.No;

            // 휴지통도 Place 모드에서만 활성화되는 것처럼 보이게 만든다.
            TrashDropZone.Opacity = isPlaceMode ? 1.0 : 0.5;

            // 현재 모드 설명 문구를 바꾼다.
            ModeDescriptionText.Text = isPlaceMode
                ? "Place mode: create, move, and delete seats and lacks on the map."
                : "Edit mode: click a seat to edit properties. Moving is disabled.";
            TrashDescriptionText.Text = isPlaceMode
                ? "Drag a seat here in Place mode to delete it"
                : "Trash is disabled outside Place mode";

            if (!isPlaceMode)
            {
                // Edit 모드로 들어갈 때 휴지통 강조 상태가 남아 있지 않도록 초기화한다.
                ResetTrashDropZoneAppearance();
            }
        }
    }
}
