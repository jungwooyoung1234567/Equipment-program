using System.Windows;

namespace UL_project
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadOrInitializeLayout();
            UpdateMapUi();
            UpdateModeUi();
            Closing += MainWindow_Closing;
        }
    }
}
