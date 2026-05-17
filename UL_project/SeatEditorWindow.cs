using System.Windows;
using System.Windows.Controls;

namespace UL_project
{
    // Seat 또는 Lack의 이름/팀 정보를 수정하는 작은 팝업 창.
    public sealed class SeatEditorWindow : Window
    {
        // 입력값을 읽기 위한 텍스트 박스 참조.
        private readonly TextBox _nameTextBox;
        private readonly TextBox _teamTextBox;

        // 창이 닫힌 뒤 MainWindow에서 결과를 읽을 수 있도록 속성으로 노출한다.
        public string SeatName => _nameTextBox.Text.Trim();
        public string TeamName => _teamTextBox.Text.Trim();

        public SeatEditorWindow(string seatName, string teamName)
        {
            // 팝업 창 기본 모양 설정.
            Title = "Edit Seat";
            Width = 320;
            Height = 220;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = System.Windows.Media.Brushes.White;

            // 전체 입력 폼을 담는 루트 Grid.
            var root = new Grid
            {
                Margin = new Thickness(16)
            };

            // 라벨, 텍스트박스, 버튼 배치를 위한 행 구성.
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 첫 번째 입력 라벨: Seat Name
            root.Children.Add(new TextBlock
            {
                Text = "Seat Name",
                FontWeight = FontWeights.SemiBold
            });

            // 이름 입력 박스에 기존 값을 채워 넣는다.
            _nameTextBox = new TextBox
            {
                Margin = new Thickness(0, 6, 0, 12),
                Text = seatName
            };
            Grid.SetRow(_nameTextBox, 1);
            root.Children.Add(_nameTextBox);

            // 두 번째 입력 라벨: Team
            root.Children.Add(new TextBlock
            {
                Text = "Team",
                FontWeight = FontWeights.SemiBold
            });
            Grid.SetRow(root.Children[^1], 2);

            // 팀 입력 박스에 기존 값을 채워 넣는다.
            _teamTextBox = new TextBox
            {
                Margin = new Thickness(0, 6, 0, 0),
                Text = teamName
            };
            Grid.SetRow(_teamTextBox, 3);
            root.Children.Add(_teamTextBox);

            // 저장/취소 버튼은 오른쪽 정렬로 배치한다.
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Save를 누르면 DialogResult = true로 닫아서 저장 의사를 호출자에게 알린다.
            var saveButton = new Button
            {
                Width = 80,
                Margin = new Thickness(0, 12, 8, 0),
                Content = "Save",
                IsDefault = true
            };
            saveButton.Click += (_, _) =>
            {
                DialogResult = true;
                Close();
            };

            // Cancel은 값 반영 없이 그냥 닫는다.
            var cancelButton = new Button
            {
                Width = 80,
                Margin = new Thickness(0, 12, 0, 0),
                Content = "Cancel",
                IsCancel = true
            };
            cancelButton.Click += (_, _) => Close();

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 5);
            root.Children.Add(buttonPanel);

            // 만들어둔 루트 레이아웃을 창 내용으로 설정한다.
            Content = root;
        }
    }
}
