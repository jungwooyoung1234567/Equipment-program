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
        private static string LayoutBackupFilePath => $"{LayoutFilePath}.bak";
        private static string LayoutTempFilePath => $"{LayoutFilePath}.tmp";

        private bool _preserveExistingLayoutFile;

        private void LoadOrInitializeLayout()
        {
            if (!File.Exists(LayoutFilePath))
            {
                InitializeDefaultLayout();
                SaveLayout();
                return;
            }

            if (TryLoadLayout(out var loadError))
            {
                return;
            }

            InitializeDefaultLayout();
            _preserveExistingLayoutFile = true;
            MessageBox.Show(
                $"저장된 레이아웃을 불러오지 못했습니다.\n기존 파일은 복구할 수 있도록 그대로 유지했습니다.\n\n{loadError}",
                "레이아웃 불러오기",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        private void InitializeDefaultLayout()
        {
            _mapCanvases.Clear();
            _mapNames.Clear();
            _activeMapIndex = 0;
            LayoutTitleTextBox.Text = "연구실 레이아웃";

            _mapCanvases.Add(CreateMapCanvas());
            _mapNames.Add(BuildDefaultMapName(1));
        }

        private bool TryLoadLayout(out string? errorMessage)
        {
            try
            {
                var json = File.ReadAllText(LayoutFilePath);
                var layoutState = JsonSerializer.Deserialize<LayoutState>(json, LayoutJsonOptions);
                if (layoutState is null)
                {
                    errorMessage = "레이아웃 파일이 비어 있거나 해석할 수 없습니다.";
                    return false;
                }

                ApplyLayoutState(layoutState);
                errorMessage = null;
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }

        private void ApplyLayoutState(LayoutState layoutState)
        {
            _mapCanvases.Clear();
            _mapNames.Clear();

            LayoutTitleTextBox.Text = string.IsNullOrWhiteSpace(layoutState.LayoutTitle)
                ? "연구실 레이아웃"
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
            EnsureMinimumSampleEquipment(seatInfo, itemType);
            item.Tag = seatInfo;
            UpdateSeatDisplay(item, seatInfo);

            var rotateTransform = GetOrCreateRotateTransform(item);
            rotateTransform.Angle = ((itemState.Rotation % 360) + 360) % 360;
            return item;
        }

        private static void EnsureMinimumSampleEquipment(SeatInfo seatInfo, string itemType)
        {
            const int minimumEquipmentCount = 10;

            for (var index = seatInfo.Equipments.Count; index < minimumEquipmentCount; index++)
            {
                seatInfo.Equipments.Add(CreateSampleEquipment(seatInfo, itemType, index + 1));
            }
        }

        private static EquipmentInfo CreateSampleEquipment(SeatInfo seatInfo, string itemType, int number)
        {
            var itemPrefix = itemType switch
            {
                LackItemType => "Shelf",
                CartItemType => "Cart",
                _ => "Bench"
            };
            var seatCode = BuildSampleSeatCode(seatInfo.SeatName);

            return new EquipmentInfo
            {
                Name = $"{itemPrefix} Equipment {number:00}",
                UlNumber = $"UL-{seatCode}-{number:00}",
                GlobalNumber = $"GL-{seatCode}-{number:00}",
                Notes = "Sample data",
                PhotoPath = string.Empty
            };
        }

        private static string BuildSampleSeatCode(string seatName)
        {
            var normalized = new string((seatName ?? string.Empty)
                .Where(char.IsLetterOrDigit)
                .Take(6)
                .ToArray());

            return string.IsNullOrWhiteSpace(normalized)
                ? "ITEM"
                : normalized.ToUpperInvariant();
        }

        private void SaveLayout()
        {
            var layoutState = BuildLayoutState();
            var json = JsonSerializer.Serialize(layoutState, LayoutJsonOptions);
            File.WriteAllText(LayoutTempFilePath, json);

            if (File.Exists(LayoutFilePath))
            {
                File.Copy(LayoutFilePath, LayoutBackupFilePath, overwrite: true);
            }

            File.Move(LayoutTempFilePath, LayoutFilePath, overwrite: true);
            _preserveExistingLayoutFile = false;
        }

        private void ResetLayoutButton_Click(object sender, RoutedEventArgs e)
        {
            var confirmationResult = MessageBox.Show(
                "지금 배치한 모든 기구를 지우고 처음 상태로 되돌릴까요?\n저장된 레이아웃 파일도 함께 새로 저장됩니다.",
                "모든 배치 처음으로 되돌리기",
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
                _preserveExistingLayoutFile = false;
                SaveLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"배치를 처음 상태로 되돌리지 못했습니다.\n{ex.Message}",
                    "모든 배치 처음으로 되돌리기",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private LayoutState BuildLayoutState()
        {
            var layoutState = new LayoutState
            {
                LayoutTitle = string.IsNullOrWhiteSpace(LayoutTitleTextBox.Text)
                    ? "연구실 레이아웃"
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
            if (_preserveExistingLayoutFile)
            {
                return;
            }

            try
            {
                SaveLayout();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"레이아웃 저장에 실패했습니다.\n{ex.Message}",
                    "레이아웃 저장",
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
                        Notes = equipment.Notes,
                        PhotoPath = equipment.PhotoPath
                    })
                    .ToList()
            };
        }
    }
}
