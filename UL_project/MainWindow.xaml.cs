using System.Windows;
using System.Windows.Input;

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
            PreviewKeyDown += MainWindow_PreviewKeyDown;
        }
    }
}
