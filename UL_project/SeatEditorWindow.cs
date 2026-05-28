using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;

namespace UL_project
{
    internal sealed class SeatEditorWindow : Window
    {
        private const int MaxEquipmentCount = 100;
        private const string DefaultEquipmentType = "주장비";
        private const string SecondaryEquipmentType = "보조장비";
        private static readonly string[] EquipmentTypeOptions = [DefaultEquipmentType, SecondaryEquipmentType];

        private readonly TextBox _nameTextBox;
        private readonly TextBox _teamTextBox;
        private readonly ObservableCollection<EquipmentDraft> _equipmentItems;
        private readonly List<EquipmentDraft> _copiedEquipmentRows = new();
        private readonly DataGrid _equipmentGrid;
        private readonly TextBlock _equipmentCountText;
        private readonly int _initialEquipmentIndex;
        private CheckBox? _selectAllCheckBox;
        private Point _dragSelectionStartPoint;
        private bool _hasPendingDragSelection;
        private bool _isDraggingSelection;
        private bool _isUpdatingSelectAllCheckBox;
        private int _dragSelectionStartIndex = -1;

        public string SeatName => _nameTextBox.Text.Trim();

        public string TeamName => _teamTextBox.Text.Trim();

        public List<EquipmentInfo> Equipments =>
            _equipmentItems
                .Where(item => !item.IsEmpty)
                .Select(item => item.ToEquipmentInfo())
                .ToList();

        public SeatEditorWindow(SeatInfo seatInfo, string paletteTypeName, int initialEquipmentIndex = -1)
        {
            _initialEquipmentIndex = initialEquipmentIndex;
            Title = "배치 장비 편집";
            Width = 1120;
            Height = 700;
            MinWidth = 920;
            MinHeight = 560;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Brushes.White;

            _equipmentItems = new ObservableCollection<EquipmentDraft>(
                seatInfo.Equipments.Select(EquipmentDraft.FromEquipmentInfo));
            _equipmentItems.CollectionChanged += EquipmentItems_CollectionChanged;
            RefreshEquipmentNumbers();

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
                Text = "장비 정보",
                FontSize = 20,
                FontWeight = FontWeights.Bold
            });
            headerPanel.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 6, 0, 0),
                Text = "장비 정보와 장비 목록을 수정합니다. 장비는 최대 100개까지 입력할 수 있습니다.",
                Foreground = Brushes.DimGray
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
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            Grid.SetRow(formGrid, 1);

            formGrid.Children.Add(new TextBlock
            {
                Text = "기구 종류",
                FontWeight = FontWeights.SemiBold
            });
            Grid.SetColumnSpan(formGrid.Children[^1], 2);

            formGrid.Children.Add(new TextBlock
            {
                Margin = new Thickness(0, 6, 0, 14),
                Text = paletteTypeName,
                Foreground = Brushes.DimGray
            });
            Grid.SetRow(formGrid.Children[^1], 1);
            Grid.SetColumnSpan(formGrid.Children[^1], 2);

            formGrid.Children.Add(new TextBlock
            {
                Text = "기구 이름",
                FontWeight = FontWeights.SemiBold
            });
            Grid.SetRow(formGrid.Children[^1], 2);

            formGrid.Children.Add(new TextBlock
            {
                Text = "팀",
                FontWeight = FontWeights.SemiBold
            });
            Grid.SetRow(formGrid.Children[^1], 2);
            Grid.SetColumn(formGrid.Children[^1], 1);

            _nameTextBox = new TextBox
            {
                Margin = new Thickness(0, 6, 10, 0),
                Text = seatInfo.SeatName
            };
            Grid.SetRow(_nameTextBox, 3);
            formGrid.Children.Add(_nameTextBox);

            _teamTextBox = new TextBox
            {
                Margin = new Thickness(0, 6, 0, 0),
                Text = seatInfo.TeamName
            };
            Grid.SetColumn(_teamTextBox, 1);
            Grid.SetRow(_teamTextBox, 3);
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
                Content = "장비 추가"
            };
            addButton.Click += AddEquipmentButton_Click;

            var removeButton = new Button
            {
                Width = 120,
                Content = "장비삭제"
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
                Content = "저장",
                IsDefault = true
            };
            saveButton.Click += SaveButton_Click;

            var cancelButton = new Button
            {
                Width = 80,
                Content = "취소",
                IsCancel = true
            };
            cancelButton.Click += (_, _) => Close();

            buttonPanel.Children.Add(saveButton);
            buttonPanel.Children.Add(cancelButton);
            root.Children.Add(buttonPanel);

            Content = root;
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Copy, TextEditingCommand_Executed, TextEditingCommand_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Cut, TextEditingCommand_Executed, TextEditingCommand_CanExecute));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, TextEditingCommand_Executed, TextEditingCommand_CanExecute));
            Loaded += SeatEditorWindow_Loaded;
            UpdateEquipmentCount();
        }

        private void SeatEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollToInitialEquipment();
        }

        private void ScrollToInitialEquipment()
        {
            if (_initialEquipmentIndex < 0 || _initialEquipmentIndex >= _equipmentItems.Count)
            {
                return;
            }

            var item = _equipmentItems[_initialEquipmentIndex];
            _equipmentGrid.SelectedItem = item;
            _equipmentGrid.CurrentItem = item;
            _equipmentGrid.Focus();

            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                _equipmentGrid.ScrollIntoView(item);
                _equipmentGrid.UpdateLayout();
                _equipmentGrid.ScrollIntoView(item);
            }));
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
                SelectionMode = DataGridSelectionMode.Extended,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                MinRowHeight = 44
            };

            grid.PreviewMouseLeftButtonDown += EquipmentGrid_PreviewMouseLeftButtonDown;
            grid.PreviewMouseMove += EquipmentGrid_PreviewMouseMove;
            grid.PreviewMouseLeftButtonUp += EquipmentGrid_PreviewMouseLeftButtonUp;
            grid.LostMouseCapture += EquipmentGrid_LostMouseCapture;
            grid.PreviewKeyDown += EquipmentGrid_PreviewKeyDown;
            grid.SelectionChanged += EquipmentGrid_SelectionChanged;

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = BuildSelectAllHeader(),
                Binding = new Binding(nameof(EquipmentDraft.DisplayNumber)),
                Width = new DataGridLength(58),
                IsReadOnly = true
            });
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "이름",
                Binding = new Binding(nameof(EquipmentDraft.Name)),
                Width = new DataGridLength(1.2, DataGridLengthUnitType.Star)
            });
            grid.Columns.Add(new DataGridComboBoxColumn
            {
                Header = "장비구분",
                ItemsSource = EquipmentTypeOptions,
                SelectedItemBinding = new Binding(nameof(EquipmentDraft.EquipmentType))
                {
                    Mode = BindingMode.TwoWay,
                    UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                },
                Width = new DataGridLength(110)
            });
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "UL 번호",
                Binding = new Binding(nameof(EquipmentDraft.UlNumber)),
                Width = new DataGridLength(0.7, DataGridLengthUnitType.Star)
            });
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Global 번호",
                Binding = new Binding(nameof(EquipmentDraft.GlobalNumber)),
                Width = new DataGridLength(0.7, DataGridLengthUnitType.Star)
            });
            grid.Columns.Add(BuildNotesColumn());
            grid.Columns.Add(BuildPhotoManageColumn());

            return grid;
        }

        private CheckBox BuildSelectAllHeader()
        {
            _selectAllCheckBox = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsThreeState = true,
                ToolTip = "전체 장비 선택/해제"
            };
            _selectAllCheckBox.Click += SelectAllCheckBox_Click;

            return _selectAllCheckBox;
        }

        private DataGridTemplateColumn BuildNotesColumn()
        {
            var textBoxFactory = new FrameworkElementFactory(typeof(TextBox));
            textBoxFactory.SetBinding(TextBox.TextProperty, new Binding(nameof(EquipmentDraft.Notes))
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
            textBoxFactory.SetValue(TextBox.AcceptsReturnProperty, true);
            textBoxFactory.SetValue(TextBox.TextWrappingProperty, TextWrapping.Wrap);
            textBoxFactory.SetValue(TextBox.VerticalScrollBarVisibilityProperty, ScrollBarVisibility.Auto);
            textBoxFactory.SetValue(TextBox.MinHeightProperty, 56d);
            textBoxFactory.SetValue(TextBox.PaddingProperty, new Thickness(4, 3, 4, 3));
            textBoxFactory.SetValue(TextBox.BorderThicknessProperty, new Thickness(0));
            textBoxFactory.SetValue(TextBox.BackgroundProperty, Brushes.Transparent);

            return new DataGridTemplateColumn
            {
                Header = "비고",
                Width = new DataGridLength(1.8, DataGridLengthUnitType.Star),
                CellTemplate = new DataTemplate
                {
                    VisualTree = textBoxFactory
                },
                CellEditingTemplate = new DataTemplate
                {
                    VisualTree = textBoxFactory
                }
            };
        }

        private DataGridTemplateColumn BuildPhotoManageColumn()
        {
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.WidthProperty, 48d);
            buttonFactory.SetValue(Button.HeightProperty, 28d);
            buttonFactory.SetValue(Button.MarginProperty, new Thickness(0));
            buttonFactory.SetBinding(ContentControl.ContentProperty, new Binding(nameof(EquipmentDraft.PhotoButtonText)));
            buttonFactory.SetBinding(FrameworkElement.TagProperty, new Binding());
            buttonFactory.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ManagePhotoButton_Click));

            return new DataGridTemplateColumn
            {
                Header = "사진",
                Width = new DataGridLength(64),
                CellTemplate = new DataTemplate
                {
                    VisualTree = buttonFactory
                }
            };
        }

        private void AddEquipmentButton_Click(object? sender, RoutedEventArgs e)
        {
            if (_equipmentItems.Count >= MaxEquipmentCount)
            {
                MessageBox.Show(
                    $"기구 하나에는 장비를 최대 {MaxEquipmentCount}개까지 입력할 수 있습니다.",
                    "장비 개수 제한",
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
            RemoveSelectedEquipmentRows();
        }

        private void ManagePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not EquipmentDraft draft)
            {
                return;
            }

            ShowPhotoManagementWindow(draft);
        }

        private void AddPhotoToDraft(EquipmentDraft draft)
        {
            var dialog = new OpenFileDialog
            {
                Title = "사진 선택",
                Filter = "이미지 파일|*.png;*.jpg;*.jpeg;*.bmp;*.gif|모든 파일|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            draft.PhotoPath = dialog.FileName;
        }

        private void DeletePhotoFromDraft(EquipmentDraft draft)
        {
            var confirmationResult = MessageBox.Show(
                $"{GetPhotoDisplayName(draft)}의 사진을 삭제하시겠습니까?",
                "사진 삭제",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmationResult != MessageBoxResult.Yes)
            {
                return;
            }

            draft.PhotoPath = string.Empty;
        }

        private void ShowPhotoManagementWindow(EquipmentDraft draft)
        {
            var preview = new Image
            {
                Source = EquipmentDraft.LoadPreview(draft.PhotoPath),
                Stretch = Stretch.Uniform,
                Margin = new Thickness(16)
            };

            var emptyText = new TextBlock
            {
                Text = "등록된 사진이 없습니다.",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.DimGray
            };

            var previewHost = new Grid
            {
                MinHeight = 260,
                Background = new SolidColorBrush(Color.FromRgb(248, 248, 248))
            };
            previewHost.Children.Add(preview);
            previewHost.Children.Add(emptyText);

            void RefreshPreview()
            {
                preview.Source = EquipmentDraft.LoadPreview(draft.PhotoPath);
                emptyText.Visibility = preview.Source is null ? Visibility.Visible : Visibility.Collapsed;
            }

            var addButton = new Button
            {
                Width = 96,
                Margin = new Thickness(0, 0, 8, 0),
                Content = "사진 추가"
            };
            addButton.Click += (_, _) =>
            {
                AddPhotoToDraft(draft);
                RefreshPreview();
            };

            var deleteButton = new Button
            {
                Width = 96,
                Margin = new Thickness(0, 0, 8, 0),
                Content = "사진 삭제"
            };
            deleteButton.SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(EquipmentDraft.HasPhoto))
            {
                Source = draft
            });
            deleteButton.Click += (_, _) =>
            {
                DeletePhotoFromDraft(draft);
                RefreshPreview();
            };

            var closeButton = new Button
            {
                Width = 80,
                Content = "닫기",
                IsCancel = true
            };

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(16)
            };
            buttonPanel.Children.Add(addButton);
            buttonPanel.Children.Add(deleteButton);
            buttonPanel.Children.Add(closeButton);

            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.Children.Add(previewHost);
            Grid.SetRow(buttonPanel, 1);
            layout.Children.Add(buttonPanel);

            var photoWindow = new Window
            {
                Title = string.IsNullOrWhiteSpace(draft.Name) ? "장비 사진" : $"{draft.Name} 사진",
                Width = 520,
                Height = 420,
                MinWidth = 420,
                MinHeight = 320,
                Background = Brushes.White,
                Content = layout,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            closeButton.Click += (_, _) => photoWindow.Close();

            RefreshPreview();
            photoWindow.ShowDialog();
        }

        private void ShowPhotoPreviewWindow(EquipmentDraft draft)
        {
            var image = new Image
            {
                Source = EquipmentDraft.LoadPreview(draft.PhotoPath),
                Stretch = Stretch.Uniform,
                Margin = new Thickness(16)
            };

            var equipmentNameText = new TextBlock
            {
                Text = GetPhotoDisplayName(draft),
                Margin = new Thickness(16, 0, 16, 16),
                Foreground = Brushes.DimGray,
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };

            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.Children.Add(image);
            Grid.SetRow(equipmentNameText, 1);
            layout.Children.Add(equipmentNameText);

            var previewWindow = new Window
            {
                Title = string.IsNullOrWhiteSpace(draft.Name) ? "장비 사진 보기" : $"{draft.Name} 사진 보기",
                Width = 720,
                Height = 560,
                MinWidth = 420,
                MinHeight = 320,
                Background = Brushes.White,
                Content = layout,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            previewWindow.ShowDialog();
        }

        private static string GetPhotoDisplayName(EquipmentDraft draft)
        {
            if (!string.IsNullOrWhiteSpace(draft.Name))
            {
                return draft.Name.Trim();
            }

            return "이름 없는 장비";
        }

        private void SelectAllCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingSelectAllCheckBox)
            {
                return;
            }

            if (_selectAllCheckBox?.IsChecked == true)
            {
                _equipmentGrid.SelectedItems.Clear();
                foreach (var item in _equipmentItems)
                {
                    _equipmentGrid.SelectedItems.Add(item);
                }

                if (_equipmentItems.Count > 0)
                {
                    _equipmentGrid.CurrentItem = _equipmentItems[0];
                }
            }
            else
            {
                _equipmentGrid.SelectedItems.Clear();
            }

            UpdateSelectAllCheckBox();
            e.Handled = true;
        }

        private void EquipmentGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectAllCheckBox();
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

        private void EquipmentGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (TryGetFocusedTextEditor(out _))
            {
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.C)
            {
                CopySelectedEquipmentRows();
                e.Handled = true;
                return;
            }

            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0 && e.Key == Key.V)
            {
                PasteEquipmentRows();
                e.Handled = true;
                return;
            }

            if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Delete)
            {
                RemoveSelectedEquipmentRows();
                e.Handled = true;
            }
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

        private void CopySelectedEquipmentRows()
        {
            _equipmentGrid.CommitEdit();
            _equipmentGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var selectedRows = GetSelectedEquipmentRowsInDisplayOrder();
            if (selectedRows.Count == 0)
            {
                return;
            }

            _copiedEquipmentRows.Clear();
            _copiedEquipmentRows.AddRange(selectedRows.Select(item => item.Clone()));
        }

        private void PasteEquipmentRows()
        {
            if (_copiedEquipmentRows.Count == 0 || _equipmentItems.Count >= MaxEquipmentCount)
            {
                return;
            }

            _equipmentGrid.CommitEdit();
            _equipmentGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var insertIndex = GetEquipmentPasteIndex();
            var availableSlots = MaxEquipmentCount - _equipmentItems.Count;
            var rowsToInsert = _copiedEquipmentRows
                .Take(availableSlots)
                .Select(item => item.Clone())
                .ToList();

            if (rowsToInsert.Count == 0)
            {
                return;
            }

            for (var index = 0; index < rowsToInsert.Count; index++)
            {
                _equipmentItems.Insert(insertIndex + index, rowsToInsert[index]);
            }

            _equipmentGrid.SelectedItems.Clear();
            foreach (var insertedRow in rowsToInsert)
            {
                _equipmentGrid.SelectedItems.Add(insertedRow);
            }

            _equipmentGrid.CurrentItem = rowsToInsert[^1];
            _equipmentGrid.ScrollIntoView(rowsToInsert[^1]);
            UpdateEquipmentCount();

            if (rowsToInsert.Count < _copiedEquipmentRows.Count)
            {
                MessageBox.Show(
                    $"기구 하나에는 최대 {MaxEquipmentCount}개까지만 입력할 수 있어 {rowsToInsert.Count}개만 붙여넣었습니다.",
                    "장비 개수 제한",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void RemoveSelectedEquipmentRows()
        {
            _equipmentGrid.CommitEdit();
            _equipmentGrid.CommitEdit(DataGridEditingUnit.Row, true);

            var selectedRows = GetSelectedEquipmentRowsInDisplayOrder();
            if (selectedRows.Count == 0)
            {
                return;
            }

            var confirmationResult = MessageBox.Show(
                $"선택한 장비 {selectedRows.Count}개를 삭제하시겠습니까?",
                "장비 삭제",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmationResult != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (var selectedRow in selectedRows)
            {
                _equipmentItems.Remove(selectedRow);
            }

            UpdateEquipmentCount();
        }

        private void EquipmentItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshEquipmentNumbers();
            UpdateSelectAllCheckBox();
        }

        private void RefreshEquipmentNumbers()
        {
            for (var index = 0; index < _equipmentItems.Count; index++)
            {
                _equipmentItems[index].DisplayNumber = (index + 1).ToString();
            }
        }

        private List<EquipmentDraft> GetSelectedEquipmentRowsInDisplayOrder()
        {
            var selectedRows = _equipmentGrid.SelectedItems
                .OfType<EquipmentDraft>()
                .ToHashSet();

            return _equipmentItems
                .Where(selectedRows.Contains)
                .ToList();
        }

        private int GetEquipmentPasteIndex()
        {
            var selectedRows = GetSelectedEquipmentRowsInDisplayOrder();
            if (selectedRows.Count == 0)
            {
                return _equipmentItems.Count;
            }

            var lastSelectedRow = selectedRows[^1];
            return _equipmentItems.IndexOf(lastSelectedRow) + 1;
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

        private void TextEditingCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = TryGetFocusedTextEditor(out _);
            e.Handled = true;
        }

        private void TextEditingCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (!TryGetFocusedTextEditor(out var textEditor))
            {
                return;
            }

            var activeTextEditor = textEditor!;

            if (e.Command == ApplicationCommands.Copy)
            {
                activeTextEditor.Copy();
            }
            else if (e.Command == ApplicationCommands.Cut)
            {
                activeTextEditor.Cut();
            }
            else if (e.Command == ApplicationCommands.Paste)
            {
                activeTextEditor.Paste();
            }

            e.Handled = true;
        }

        private static bool TryGetFocusedTextEditor(out TextBoxBase? textEditor)
        {
            textEditor = null;

            if (Keyboard.FocusedElement is not DependencyObject focusedElement)
            {
                return false;
            }

            for (DependencyObject? current = focusedElement; current is not null; current = VisualTreeHelper.GetParent(current))
            {
                if (current is TextBoxBase focusedTextEditor)
                {
                    textEditor = focusedTextEditor;
                    return true;
                }
            }

            return false;
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
            _equipmentCountText.Text = $"장비: {_equipmentItems.Count}/{MaxEquipmentCount}";
            UpdateSelectAllCheckBox();
        }

        private void UpdateSelectAllCheckBox()
        {
            if (_selectAllCheckBox is null)
            {
                return;
            }

            _isUpdatingSelectAllCheckBox = true;
            var itemCount = _equipmentItems.Count;
            _selectAllCheckBox.IsEnabled = itemCount > 0;
            _selectAllCheckBox.IsChecked = itemCount == 0
                ? false
                : _equipmentGrid.SelectedItems.Count == itemCount
                    ? true
                    : _equipmentGrid.SelectedItems.Count == 0
                        ? false
                        : null;
            _isUpdatingSelectAllCheckBox = false;
        }

        private sealed class EquipmentDraft : INotifyPropertyChanged
        {
            private string _displayNumber = string.Empty;
            private string _name = string.Empty;
            private string _equipmentType = DefaultEquipmentType;
            private string _ulNumber = string.Empty;
            private string _globalNumber = string.Empty;
            private string _notes = string.Empty;
            private string _photoPath = string.Empty;
            public event PropertyChangedEventHandler? PropertyChanged;

            public string DisplayNumber
            {
                get => _displayNumber;
                set => SetField(ref _displayNumber, value);
            }

            public string Name
            {
                get => _name;
                set => SetField(ref _name, value);
            }

            public string EquipmentType
            {
                get => string.IsNullOrWhiteSpace(_equipmentType) ? DefaultEquipmentType : _equipmentType;
                set
                {
                    if (!SetField(ref _equipmentType, string.IsNullOrWhiteSpace(value) ? DefaultEquipmentType : value))
                    {
                        return;
                    }

                    if (IsSecondaryEquipment)
                    {
                        UlNumber = string.Empty;
                        GlobalNumber = string.Empty;
                    }
                }
            }

            public string UlNumber
            {
                get => _ulNumber;
                set => SetField(ref _ulNumber, IsSecondaryEquipment ? string.Empty : value);
            }

            public string GlobalNumber
            {
                get => _globalNumber;
                set => SetField(ref _globalNumber, IsSecondaryEquipment ? string.Empty : value);
            }

            public string Notes
            {
                get => _notes;
                set => SetField(ref _notes, value);
            }

            public string PhotoPath
            {
                get => _photoPath;
                set
                {
                    if (!SetField(ref _photoPath, value))
                    {
                        return;
                    }

                    OnPropertyChanged(nameof(HasPhoto));
                    OnPropertyChanged(nameof(PhotoButtonText));
                }
            }

            public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoPath) && File.Exists(PhotoPath);

            public string PhotoButtonText => HasPhoto ? "있음" : "+";

            private bool IsSecondaryEquipment => EquipmentType == SecondaryEquipmentType;

            public bool IsEmpty =>
                string.IsNullOrWhiteSpace(Name) &&
                string.IsNullOrWhiteSpace(UlNumber) &&
                string.IsNullOrWhiteSpace(GlobalNumber) &&
                string.IsNullOrWhiteSpace(Notes) &&
                string.IsNullOrWhiteSpace(PhotoPath);

            public EquipmentInfo ToEquipmentInfo()
            {
                return new EquipmentInfo
                {
                    Name = Name.Trim(),
                    EquipmentType = EquipmentType.Trim(),
                    UlNumber = IsSecondaryEquipment ? string.Empty : UlNumber.Trim(),
                    GlobalNumber = IsSecondaryEquipment ? string.Empty : GlobalNumber.Trim(),
                    Notes = Notes.Trim(),
                    PhotoPath = PhotoPath.Trim()
                };
            }

            public static EquipmentDraft FromEquipmentInfo(EquipmentInfo equipment)
            {
                return new EquipmentDraft
                {
                    Name = equipment.Name,
                    EquipmentType = equipment.EquipmentType,
                    UlNumber = equipment.UlNumber,
                    GlobalNumber = equipment.GlobalNumber,
                    Notes = equipment.Notes,
                    PhotoPath = equipment.PhotoPath
                };
            }

            public EquipmentDraft Clone()
            {
                return new EquipmentDraft
                {
                    Name = Name,
                    EquipmentType = EquipmentType,
                    UlNumber = UlNumber,
                    GlobalNumber = GlobalNumber,
                    Notes = Notes,
                    PhotoPath = PhotoPath
                };
            }

            public static ImageSource? LoadPreview(string path)
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    return null;
                }

                try
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new System.Uri(path, System.UriKind.Absolute);
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
                catch
                {
                    return null;
                }
            }

            private bool SetField(ref string field, string value, [CallerMemberName] string propertyName = "")
            {
                if (field == value)
                {
                    return false;
                }

                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }

            private void OnPropertyChanged([CallerMemberName] string propertyName = "")
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
