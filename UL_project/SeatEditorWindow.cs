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

        private readonly TextBox _nameTextBox;
        private readonly TextBox _teamTextBox;
        private readonly ObservableCollection<EquipmentDraft> _equipmentItems;
        private readonly List<EquipmentDraft> _copiedEquipmentRows = new();
        private readonly DataGrid _equipmentGrid;
        private readonly TextBlock _equipmentCountText;
        private readonly int _initialEquipmentIndex;
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
                Content = "선택 삭제"
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

            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "번호",
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
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "UL 번호",
                Binding = new Binding(nameof(EquipmentDraft.UlNumber)),
                Width = new DataGridLength(0.95, DataGridLengthUnitType.Star)
            });
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Global 번호",
                Binding = new Binding(nameof(EquipmentDraft.GlobalNumber)),
                Width = new DataGridLength(0.95, DataGridLengthUnitType.Star)
            });
            grid.Columns.Add(BuildNotesColumn());
            grid.Columns.Add(BuildPhotoAddColumn());
            grid.Columns.Add(BuildPhotoViewColumn());
            grid.Columns.Add(BuildPhotoDeleteColumn());

            return grid;
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
                Width = new DataGridLength(1.3, DataGridLengthUnitType.Star),
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

        private DataGridTemplateColumn BuildPhotoAddColumn()
        {
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.WidthProperty, 50d);
            buttonFactory.SetValue(Button.HeightProperty, 28d);
            buttonFactory.SetValue(Button.MarginProperty, new Thickness(0));
            buttonFactory.SetValue(Button.ContentProperty, "추가");
            buttonFactory.SetBinding(FrameworkElement.TagProperty, new Binding());
            buttonFactory.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(AddPhotoButton_Click));

            return new DataGridTemplateColumn
            {
                Header = "추가",
                Width = new DataGridLength(62),
                CellTemplate = new DataTemplate
                {
                    VisualTree = buttonFactory
                }
            };
        }

        private DataGridTemplateColumn BuildPhotoViewColumn()
        {
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.WidthProperty, 50d);
            buttonFactory.SetValue(Button.HeightProperty, 28d);
            buttonFactory.SetBinding(FrameworkElement.TagProperty, new Binding());
            buttonFactory.SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(EquipmentDraft.HasPhoto)));
            buttonFactory.SetValue(Button.ContentProperty, "보기");
            buttonFactory.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(ViewPhotoButton_Click));

            return new DataGridTemplateColumn
            {
                Header = "보기",
                Width = new DataGridLength(62),
                CellTemplate = new DataTemplate
                {
                    VisualTree = buttonFactory
                }
            };
        }

        private DataGridTemplateColumn BuildPhotoDeleteColumn()
        {
            var buttonFactory = new FrameworkElementFactory(typeof(Button));
            buttonFactory.SetValue(Button.WidthProperty, 50d);
            buttonFactory.SetValue(Button.HeightProperty, 28d);
            buttonFactory.SetBinding(FrameworkElement.TagProperty, new Binding());
            buttonFactory.SetBinding(UIElement.IsEnabledProperty, new Binding(nameof(EquipmentDraft.HasPhoto)));
            buttonFactory.SetValue(Button.ContentProperty, "삭제");
            buttonFactory.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(DeletePhotoButton_Click));

            return new DataGridTemplateColumn
            {
                Header = "삭제",
                Width = new DataGridLength(62),
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

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not EquipmentDraft draft)
            {
                return;
            }

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

        private void ViewPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not EquipmentDraft draft)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(draft.PhotoPath) || !File.Exists(draft.PhotoPath))
            {
                MessageBox.Show(
                    "등록된 사진이 없거나 파일을 찾을 수 없습니다.",
                    "사진 보기",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            ShowPhotoPreviewWindow(draft);
        }

        private void DeletePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement element || element.Tag is not EquipmentDraft draft)
            {
                return;
            }

            draft.PhotoPath = string.Empty;
        }

        private void ShowPhotoPreviewWindow(EquipmentDraft draft)
        {
            var image = new Image
            {
                Source = EquipmentDraft.LoadPreview(draft.PhotoPath),
                Stretch = Stretch.Uniform,
                Margin = new Thickness(16)
            };

            var pathText = new TextBlock
            {
                Text = draft.PhotoPath,
                Margin = new Thickness(16, 0, 16, 16),
                Foreground = Brushes.DimGray,
                TextWrapping = TextWrapping.Wrap
            };

            var layout = new Grid();
            layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            layout.Children.Add(image);
            Grid.SetRow(pathText, 1);
            layout.Children.Add(pathText);

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

            foreach (var selectedRow in selectedRows)
            {
                _equipmentItems.Remove(selectedRow);
            }

            UpdateEquipmentCount();
        }

        private void EquipmentItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshEquipmentNumbers();
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
        }

        private sealed class EquipmentDraft : INotifyPropertyChanged
        {
            private string _displayNumber = string.Empty;
            private string _name = string.Empty;
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

            public string UlNumber
            {
                get => _ulNumber;
                set => SetField(ref _ulNumber, value);
            }

            public string GlobalNumber
            {
                get => _globalNumber;
                set => SetField(ref _globalNumber, value);
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
                }
            }

            public bool HasPhoto => !string.IsNullOrWhiteSpace(PhotoPath) && File.Exists(PhotoPath);

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
                    UlNumber = UlNumber.Trim(),
                    GlobalNumber = GlobalNumber.Trim(),
                    Notes = Notes.Trim(),
                    PhotoPath = PhotoPath.Trim()
                };
            }

            public static EquipmentDraft FromEquipmentInfo(EquipmentInfo equipment)
            {
                return new EquipmentDraft
                {
                    Name = equipment.Name,
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
