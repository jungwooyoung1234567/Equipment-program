using System.Windows;

namespace UL_project
{
    // MainWindow의 나머지 로직은 partial class 파일들로 분리되어 있고,
    // 이 파일은 창의 시작 지점만 담당한다.
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            // XAML에서 정의한 화면 요소들을 먼저 생성한다.
            InitializeComponent();

            // 앱 시작 시 기본 맵 1개를 만든다.
            _mapCanvases.Add(CreateMapCanvas());

            // 현재 맵 상태와 모드 상태를 화면에 반영한다.
            UpdateMapUi();
            UpdateModeUi();
        }
    }
}
