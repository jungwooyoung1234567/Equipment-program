using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace UL_project
{
    internal sealed class SeatEditorWindow : Window
    {
        // 이 대화상자는 하나의 좌석 정보를 편집하고 저장 결과를 반환한다.
        private const int MaxEquipmentCount = 50;

        private readonly TextBox _nameTextBox;
        private readonly TextBox _teamTextBox;
        private readonly ObservableCollection<EquipmentDraft> _equipmentItems;
        private readonly DataGrid _equipmentGrid;
        private readonly TextBlock _equipmentCountText;
        private Point _dragSelectionStartPoint;
        private bool _hasPendingDragSelection;
        private bool _isDraggingSelection;
        private int _dragSelectionStartIndex = -1;

        public string SeatName => _nameTextBox.Text.Trim();

        public string TeamName => _teamTextBox.Text.Trim();

        public List<EquipmentInfo> Equipments =>
            _equipmentItems
                .Where(item => !item.IsEmpty)
                .Select(item => item.ToEquipmentInfo())
                .ToList();

        // 편집창 UI를 만들고 기존 SeatInfo 데이터를 로드한다.
        public SeatEditorWindow(SeatInfo seatInfo)
        {
            Title = "Edit Place";
            Width = 920;
            Height = 640;
            MinWidth = 760;
            MinHeight = 520;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = System.Windows.Media.Brushes.White;

            _equipmentItems = new ObservableCollection<EquipmentDraft>(
                seatInfo.Equipments.Select(EquipmentDraft.FromEquipmentInfo));

            var root = new Grid
            {
                Margin = new Thickness(16)
            };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var headerPanel = new StackPanel();
            headerPanel.Children.Add(new TextBlock
            {
                Text = "Place Information",
                FontSize = 20,
                FontWeight = FontWeights.Bold
            });
            headerPanel.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 6, 0, 0),
                Text = "Edit the place details and manage up to 50 equipment entries.",
                Foreground = System.Windows.Media.Brushes.DimGray
            });
            root.Children.Add(headerPanel);

            var formGrid = new Grid
            {
                Margin = new Thickness(0, 16, 0, 0)
            };
            formGrid.ColumnDefinitions.Add(new ColumnDefinition());
            formGrid.ColumnDefinitions.Add(new ColumnDefinition());
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(formGrid, 1);

            formGrid.Children.Add(new TextBlock
            {
                Text = "Place Name",
                FontWeight = FontWeights.SemiBold
            });

            formGrid.Children.Add(new TextBlock
            {
                Text = "Team",
                FontWeight = FontWeights.SemiBold
            });
            Grid.SetColumn(formGrid.Children[^1], 1);

            _nameTextBox = new TextBox
            {
                Margin = new Thickness(0, 6, 10, 0),
                Text = seatInfo.SeatName
            };
            Grid.SetRow(_nameTextBox, 1);
            formGrid.Children.Add(_nameTextBox);

            _teamTextBox = new TextBox
            {
                Margin = new Thickness(0, 6, 0, 0),
                Text = seatInfo.TeamName
            };
            Grid.SetColumn(_teamTextBox, 1);
            Grid.SetRow(_teamTextBox, 1);
            formGrid.Children.Add(_teamTextBox);

            root.Children.Add(formGrid);

            var toolbar = new DockPanel
            {
                Margin = new Thickness(0, 18, 0, 10)
            };
            Grid.SetRow(toolbar, 2);

            _equipmentCountText = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.SemiBold
            };
            DockPanel.SetDock(_equipmentCountText, Dock.Left);
            toolbar.Children.Add(_equipmentCountText);

            var actionPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            DockPanel.SetDock(actionPanel, Dock.Right);

            var addButton = new Button
            {
                Width = 120,
                Margin = new Thickness(0, 0, 8, 0),
                Content = "Add Equipment"
            };
            addButton.Click += AddEquipmentButton_Click;

            var removeButton = new Button
            {
                Width = 140,
                Content = "Remove Selected"
            };
            removeButton.Click += RemoveSelectedButton_Click;

            actionPanel.Children.Add(addButton);
            actionPanel.Children.Add(removeButton);
            toolbar.Children.Add(actionPanel);
            root.Children.Add(toolbar);

            _equipmentGrid = BuildEquipmentGrid();
            Grid.SetRow(_equipmentGrid, 3);
            root.Children.Add(_equipmentGrid);

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 16, 0, 0)
            };
            Grid.SetRow(buttonPanel, 4);

            var saveButton = new Button
            {
                Width = 80,
                Margin = new Thickness(0, 0, 8, 0),
                Content = "Save",
                IsDefault = true
            };
            saveButton.Click += SaveButton_Click;

            var cancelButton = new Button
            {
                Width = 80,
                Content = "Cancel",
                IsCancel = true
            };
            cancelButton.Click += (_, _) => Close();

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            root.Children.Add(buttonPanel);

            Content = root;
            UpdateEquipmentCount();
        }

        // 장비 목록을 표시할 DataGrid를 생성한다.
        private DataGrid BuildEquipmentGrid()
        {
            // 장비 테이블은 코드 안에서 직접 생성해 이 창 안에 로직을 모아둔다.
            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                ItemsSource = _equipmentItems,
                SelectionMode = DataGridSelectionMode.Extended,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                Margin = new Thickness(0, 0, 0, 0)
            };

            grid.PreviewMouseLeftButtonDown += EquipmentGrid_PreviewMouseLeftButtonDown;
            grid.PreviewMouseMove += EquipmentGrid_PreviewMouseMove;
            grid.PreviewMouseLeftButtonUp += EquipmentGrid_PreviewMouseLeftButtonUp;
            grid.LostMouseCapture += EquipmentGrid_LostMouseCapture;

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Name",
                Binding = new Binding(nameof(EquipmentDraft.Name)),
                Width = new DataGridLength(1.3, DataGridLengthUnitType.Star)
            });
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "UL Number",
                Binding = new Binding(nameof(EquipmentDraft.UlNumber)),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Global Number",
                Binding = new Binding(nameof(EquipmentDraft.GlobalNumber)),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Notes",
                Binding = new Binding(nameof(EquipmentDraft.Notes)),
                Width = new DataGridLength(1.6, DataGridLengthUnitType.Star)
            });

            return grid;
        }

        // 장비 입력 행을 하나 추가하고 새 행을 선택 상태로 만든다.
        private void AddEquipmentButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_equipmentItems.Count >= MaxEquipmentCount)
            {
                MessageBox.Show(
                    $"Each place can contain up to {MaxEquipmentCount} equipment entries.",
                    "Equipment Limit",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var newItem = new EquipmentDraft();
            _equipmentItems.Add(newItem);
            _equipmentGrid.SelectedItem = newItem;
            _equipmentGrid.ScrollIntoView(newItem);
            UpdateEquipmentCount();
        }

        // 현재 선택된 장비 행을 목록에서 제거한다.
        private void RemoveSelectedButton_Click(object? sender, RoutedEventArgs e)
        {
            var selectedItems = _equipmentGrid.SelectedItems
                .OfType<EquipmentDraft>()
                .ToList();

            if (selectedItems.Count == 0)
            {
                return;
            }

            foreach (var selectedItem in selectedItems)
            {
                _equipmentItems.Remove(selectedItem);
            }

            UpdateEquipmentCount();
        }

        private void EquipmentGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var row = FindVisualParent<DataGridRow>(e.OriginalSource as DependencyObject);
            if (row is null)
            {
                ResetDragSelection();
                return;
            }

            _dragSelectionStartPoint = e.GetPosition(_equipmentGrid);
            _dragSelectionStartIndex = row.GetIndex();
            _hasPendingDragSelection = true;
        }

        private void EquipmentGrid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            if (!_isDraggingSelection && _hasPendingDragSelection)
            {
                var currentPoint = e.GetPosition(_equipmentGrid);
                var horizontalDistance = System.Math.Abs(currentPoint.X - _dragSelectionStartPoint.X);
                var verticalDistance = System.Math.Abs(currentPoint.Y - _dragSelectionStartPoint.Y);

                if (horizontalDistance < SystemParameters.MinimumHorizontalDragDistance &&
                    verticalDistance < SystemParameters.MinimumVerticalDragDistance)
                {
                    return;
                }

                _isDraggingSelection = true;
                _equipmentGrid.CaptureMouse();
                SelectEquipmentRange(_dragSelectionStartIndex, _dragSelectionStartIndex);
            }

            if (!_isDraggingSelection)
            {
                return;
            }

            var row = FindRowAtPosition(e.GetPosition(_equipmentGrid));
            if (row is null)
            {
                return;
            }

            SelectEquipmentRange(_dragSelectionStartIndex, row.GetIndex());
        }

        private void EquipmentGrid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ResetDragSelection();
        }

        private void EquipmentGrid_LostMouseCapture(object sender, MouseEventArgs e)
        {
            ResetDragSelection();
        }

        private void SelectEquipmentRange(int startIndex, int endIndex)
        {
            if (startIndex < 0 || endIndex < 0 || _equipmentItems.Count == 0)
            {
                return;
            }

            var rangeStart = startIndex < endIndex ? startIndex : endIndex;
            var rangeEnd = startIndex > endIndex ? startIndex : endIndex;

            _equipmentGrid.SelectedItems.Clear();
            for (var index = rangeStart; index <= rangeEnd; index++)
            {
                _equipmentGrid.SelectedItems.Add(_equipmentItems[index]);
            }

            _equipmentGrid.CurrentItem = _equipmentItems[endIndex];
        }

        private DataGridRow? FindRowAtPosition(Point position)
        {
            var hit = _equipmentGrid.InputHitTest(position) as DependencyObject;
            return FindVisualParent<DataGridRow>(hit);
        }

        private void ResetDragSelection()
        {
            _hasPendingDragSelection = false;
            _isDraggingSelection = false;
            _dragSelectionStartIndex = -1;

            if (_equipmentGrid.IsMouseCaptured)
            {
                _equipmentGrid.ReleaseMouseCapture();
            }
        }

        private static T? FindVisualParent<T>(DependencyObject? child)
            where T : DependencyObject
        {
            while (child is not null)
            {
                if (child is T parent)
                {
                    return parent;
                }

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        // 편집 중인 셀 내용을 확정한 뒤 대화상자를 저장 상태로 닫는다.
        private void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            // 그리드에 남아 있는 편집 중 값을 먼저 커밋한다.
            _equipmentGrid.CommitEdit();
            _equipmentGrid.CommitEdit(DataGridEditingUnit.Row, true);

            DialogResult = true;
            Close();
        }

        // 현재 장비 행 수를 상단 카운트 문구에 반영한다.
        private void UpdateEquipmentCount()
        {
            _equipmentCountText.Text = $"Equipment: {_equipmentItems.Count}/{MaxEquipmentCount}";
        }

        private sealed class EquipmentDraft
        {
            // 빈 입력 행도 잠시 유지할 수 있도록 중간 편집용 구조를 사용한다.
            public string Name { get; set; } = string.Empty;

            public string UlNumber { get; set; } = string.Empty;

            public string GlobalNumber { get; set; } = string.Empty;

            public string Notes { get; set; } = string.Empty;

            public bool IsEmpty =>
                string.IsNullOrWhiteSpace(Name) &&
                string.IsNullOrWhiteSpace(UlNumber) &&
                string.IsNullOrWhiteSpace(GlobalNumber) &&
                string.IsNullOrWhiteSpace(Notes);

            // 편집용 행 데이터를 실제 EquipmentInfo 객체로 변환한다.
            public EquipmentInfo ToEquipmentInfo()
            {
                return new EquipmentInfo
                {
                    Name = Name.Trim(),
                    UlNumber = UlNumber.Trim(),
                    GlobalNumber = GlobalNumber.Trim(),
                    Notes = Notes.Trim()
                };
            }

            // 기존 EquipmentInfo를 편집용 행 데이터로 복사한다.
            public static EquipmentDraft FromEquipmentInfo(EquipmentInfo equipment)
            {
                return new EquipmentDraft
                {
                    Name = equipment.Name,
                    UlNumber = equipment.UlNumber,
                    GlobalNumber = equipment.GlobalNumber,
                    Notes = equipment.Notes
                };
            }
        }
    }
}
