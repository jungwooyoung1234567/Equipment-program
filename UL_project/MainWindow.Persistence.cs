using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace UL_project
{
    public partial class MainWindow
    {
        private const string SeatItemType = "Seat";
        private const string LackItemType = "Lack";
        private const string CartItemType = "Cart";

        private static readonly JsonSerializerOptions LayoutJsonOptions = new()
        {
            WriteIndented = true
        };

        private static string LayoutFilePath => Path.Combine(AppContext.BaseDirectory, "layout.json");

        private void LoadOrInitializeLayout()
        {
            if (TryLoadLayout())
            {
                return;
            }

            InitializeDefaultLayout();
            SaveLayout();
        }

        private void InitializeDefaultLayout()
        {
            _mapCanvases.Clear();
            _mapNames.Clear();
            _activeMapIndex = 0;
            LayoutTitleTextBox.Text = "Research Lab Layout";

            _mapCanvases.Add(CreateMapCanvas());
            _mapNames.Add(BuildDefaultMapName(1));
        }

        private bool TryLoadLayout()
        {
            if (!File.Exists(LayoutFilePath))
            {
                return false;
            }

            try
            {
                var json = File.ReadAllText(LayoutFilePath);
                var layoutState = JsonSerializer.Deserialize<LayoutState>(json, LayoutJsonOptions);
                if (layoutState is null)
                {
                    return false;
                }

                ApplyLayoutState(layoutState);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ApplyLayoutState(LayoutState layoutState)
        {
            _mapCanvases.Clear();
            _mapNames.Clear();

            LayoutTitleTextBox.Text = string.IsNullOrWhiteSpace(layoutState.LayoutTitle)
                ? "Research Lab Layout"
                : layoutState.LayoutTitle;

            foreach (var mapState in layoutState.Maps)
            {
                var mapCanvas = CreateMapCanvas();
                _mapCanvases.Add(mapCanvas);
                _mapNames.Add(string.IsNullOrWhiteSpace(mapState.Name)
                    ? BuildDefaultMapName(_mapCanvases.Count)
                    : mapState.Name);

                foreach (var itemState in mapState.Items)
                {
                    var item = BuildItemFromState(itemState);
                    mapCanvas.Children.Add(item);
                    SetSeatPosition(mapCanvas, item, itemState.Left, itemState.Top);
                }
            }

            if (_mapCanvases.Count == 0)
            {
                InitializeDefaultLayout();
                return;
            }

            _activeMapIndex = Math.Clamp(layoutState.ActiveMapIndex, 0, _mapCanvases.Count - 1);
            SyncPlacementCounters();
        }

        private Border BuildItemFromState(MapItemState itemState)
        {
            var seatInfo = itemState.SeatInfo ?? new SeatInfo();
            var itemType = string.IsNullOrWhiteSpace(itemState.Type) ? SeatItemType : itemState.Type;
            var item = itemType switch
            {
                LackItemType => BuildLackElement(seatInfo.SeatName),
                CartItemType => BuildCartElement(seatInfo.SeatName),
                _ => BuildSeatElement(seatInfo.SeatName)
            };

            item.Width = itemState.Width > 0 ? itemState.Width : item.Width;
            item.Height = itemState.Height > 0 ? itemState.Height : item.Height;
            item.Tag = seatInfo;
            UpdateSeatDisplay(item, seatInfo);

            var rotateTransform = GetOrCreateRotateTransform(item);
            rotateTransform.Angle = ((itemState.Rotation % 360) + 360) % 360;
            return item;
        }

        private void SaveLayout()
        {
            var layoutState = BuildLayoutState();
            var json = JsonSerializer.Serialize(layoutState, LayoutJsonOptions);
            File.WriteAllText(LayoutFilePath, json);
        }

        private void ResetLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            var confirmationResult = MessageBox.Show(
                "Reset the current layout and overwrite the saved JSON file?",
                "Reset Layout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirmationResult != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                CancelSeatDrag();
                StopSeatHighlight();
                InitializeDefaultLayout();
                SyncPlacementCounters();
                UpdateMapUi();
                SaveLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to reset layout.\n{ex.Message}",
                    "Reset Layout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private LayoutState BuildLayoutState()
        {
            var layoutState = new LayoutState
            {
                LayoutTitle = string.IsNullOrWhiteSpace(LayoutTitleTextBox.Text)
                    ? "Research Lab Layout"
                    : LayoutTitleTextBox.Text.Trim(),
                ActiveMapIndex = _activeMapIndex
            };

            for (var mapIndex = 0; mapIndex < _mapCanvases.Count; mapIndex++)
            {
                var mapCanvas = _mapCanvases[mapIndex];
                var mapState = new MapState
                {
                    Name = _mapNames[mapIndex]
                };

                foreach (var seat in mapCanvas.Children.OfType<Border>())
                {
                    if (seat.Tag is not SeatInfo seatInfo)
                    {
                        continue;
                    }

                    mapState.Items.Add(new MapItemState
                    {
                        Type = string.IsNullOrWhiteSpace(seat.Uid) ? SeatItemType : seat.Uid,
                        Left = Canvas.GetLeft(seat),
                        Top = Canvas.GetTop(seat),
                        Width = seat.Width,
                        Height = seat.Height,
                        Rotation = NormalizeSeatAngle(seat),
                        SeatInfo = CloneSeatInfo(seatInfo)
                    });
                }

                layoutState.Maps.Add(mapState);
            }

            return layoutState;
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                SaveLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save layout.\n{ex.Message}",
                    "Save Layout",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SyncPlacementCounters()
        {
            _seatCounter = GetNextItemNumber(SeatItemType);
            _lackCounter = GetNextItemNumber(LackItemType);
            _cartCounter = GetNextItemNumber(CartItemType);
        }

        private int GetNextItemNumber(string itemType)
        {
            var maxNumber = 0;

            foreach (var seat in _mapCanvases.SelectMany(canvas => canvas.Children.OfType<Border>()))
            {
                if (!string.Equals(seat.Uid, itemType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (seat.Tag is not SeatInfo seatInfo)
                {
                    continue;
                }

                var prefix = $"{itemType} ";
                if (!seatInfo.SeatName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var suffix = seatInfo.SeatName[prefix.Length..];
                if (int.TryParse(suffix, out var parsedNumber))
                {
                    maxNumber = Math.Max(maxNumber, parsedNumber);
                }
            }

            return maxNumber + 1;
        }

        private static SeatInfo CloneSeatInfo(SeatInfo source)
        {
            return new SeatInfo
            {
                SeatName = source.SeatName,
                TeamName = source.TeamName,
                Equipments = source.Equipments
                    .Select(equipment => new EquipmentInfo
                    {
                        Name = equipment.Name,
                        UlNumber = equipment.UlNumber,
                        GlobalNumber = equipment.GlobalNumber,
                        Notes = equipment.Notes
                    })
                    .ToList()
            };
        }
    }
}
