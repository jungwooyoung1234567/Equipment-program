using System.Windows;
using System.Windows.Controls;

namespace UL_project
{
    public partial class MainWindow
    {
        private void OpenSeatEditor(Border seat)
        {
            // 배치된 UI 요소의 Tag에서 실제 데이터 객체를 꺼낸다.
            if (seat.Tag is not SeatInfo seatInfo)
            {
                return;
            }

            // 현재 값으로 편집 팝업을 연다.
            var editorWindow = new SeatEditorWindow(seatInfo.SeatName, seatInfo.TeamName)
            {
                Owner = this
            };

            // 사용자가 저장하지 않고 닫으면 아무 것도 바꾸지 않는다.
            if (editorWindow.ShowDialog() != true)
            {
                return;
            }

            // 빈 이름 입력은 무시하고, 팀 정보는 입력값으로 갱신한다.
            seatInfo.SeatName = string.IsNullOrWhiteSpace(editorWindow.SeatName) ? seatInfo.SeatName : editorWindow.SeatName;
            seatInfo.TeamName = editorWindow.TeamName;

            // 수정된 데이터를 화면 텍스트에도 반영한다.
            UpdateSeatDisplay(seat, seatInfo);
        }

        private void UpdateSeatDisplay(Border seat, SeatInfo seatInfo)
        {
            // Seat/Lack 내부에 넣어둔 StackPanel 구조를 다시 찾아온다.
            if (seat.Child is not StackPanel content || content.Children.Count < 2)
            {
                return;
            }

            if (content.Children[0] is TextBlock nameText)
            {
                // 첫 번째 줄은 이름이다.
                nameText.Text = seatInfo.SeatName;
            }

            if (content.Children[1] is TextBlock teamText)
            {
                // 두 번째 줄은 팀/분류이며, 비어 있으면 Unassigned로 보여준다.
                teamText.Text = string.IsNullOrWhiteSpace(seatInfo.TeamName) ? "Unassigned" : seatInfo.TeamName;
            }
        }
    }
}
