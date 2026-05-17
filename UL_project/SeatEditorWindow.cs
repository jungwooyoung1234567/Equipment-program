using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace UL_project
{
    internal sealed class SeatEditorWindow : Window
    {
        private const int MaxEquipmentCount = 50;

        private readonly TextBox _nameTextBox;
        private readonly TextBox _teamTextBox;
        private readonly ObservableCollection<EquipmentDraft> _equipmentItems;
        private readonly DataGrid _equipmentGrid;
        private readonly TextBlock _equipmentCountText;

        public string SeatName => _nameTextBox.Text.Trim();

        public string TeamName => _teamTextBox.Text.Trim();

        public List<EquipmentInfo> Equipments =>
            _equipmentItems
                .Where(item => !item.IsEmpty)
                .Select(item => item.ToEquipmentInfo())
                .ToList();

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

        private DataGrid BuildEquipmentGrid()
        {
            var grid = new DataGrid
            {
                AutoGenerateColumns = false,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                ItemsSource = _equipmentItems,
                SelectionMode = DataGridSelectionMode.Single,
                Margin = new Thickness(0, 0, 0, 0)
            };

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

        private void RemoveSelectedButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_equipmentGrid.SelectedItem is not EquipmentDraft selectedItem)
            {
                return;
            }

            _equipmentItems.Remove(selectedItem);
            UpdateEquipmentCount();
        }

        private void SaveButton_Click(object? sender, RoutedEventArgs e)
        {
            _equipmentGrid.CommitEdit();
            _equipmentGrid.CommitEdit(DataGridEditingUnit.Row, true);

            DialogResult = true;
            Close();
        }

        private void UpdateEquipmentCount()
        {
            _equipmentCountText.Text = $"Equipment: {_equipmentItems.Count}/{MaxEquipmentCount}";
        }

        private sealed class EquipmentDraft
        {
            public string Name { get; set; } = string.Empty;

            public string UlNumber { get; set; } = string.Empty;

            public string GlobalNumber { get; set; } = string.Empty;

            public string Notes { get; set; } = string.Empty;

            public bool IsEmpty =>
                string.IsNullOrWhiteSpace(Name) &&
                string.IsNullOrWhiteSpace(UlNumber) &&
                string.IsNullOrWhiteSpace(GlobalNumber) &&
                string.IsNullOrWhiteSpace(Notes);

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
