using System;
using System.Collections.Generic;
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

            var usedUlNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var usedGlobalNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var nextUlNumber = 1;
            var nextGlobalNumber = 200000;

            foreach (var mapState in layoutState.Maps)
            {
                var mapCanvas = CreateMapCanvas();
                _mapCanvases.Add(mapCanvas);
                _mapNames.Add(string.IsNullOrWhiteSpace(mapState.Name)
                    ? BuildDefaultMapName(_mapCanvases.Count)
                    : mapState.Name);

                foreach (var itemState in mapState.Items)
                {
                    var item = BuildItemFromState(
                        itemState,
                        usedUlNumbers,
                        usedGlobalNumbers,
                        ref nextUlNumber,
                        ref nextGlobalNumber);
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

        private Border BuildItemFromState(
            MapItemState itemState,
            HashSet<string> usedUlNumbers,
            HashSet<string> usedGlobalNumbers,
            ref int nextUlNumber,
            ref int nextGlobalNumber)
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
            EnsureEquipmentIdentifiers(seatInfo, usedUlNumbers, usedGlobalNumbers, ref nextUlNumber, ref nextGlobalNumber);
            EnsureMinimumSampleEquipment(seatInfo, itemType, usedUlNumbers, usedGlobalNumbers, ref nextUlNumber, ref nextGlobalNumber);
            item.Tag = seatInfo;
            UpdateSeatDisplay(item, seatInfo);

            var rotateTransform = GetOrCreateRotateTransform(item);
            rotateTransform.Angle = ((itemState.Rotation % 360) + 360) % 360;
            return item;
        }

        private static void EnsureEquipmentIdentifiers(
            SeatInfo seatInfo,
            HashSet<string> usedUlNumbers,
            HashSet<string> usedGlobalNumbers,
            ref int nextUlNumber,
            ref int nextGlobalNumber)
        {
            foreach (var equipment in seatInfo.Equipments)
            {
                if (!TryRegisterUlNumber(equipment.UlNumber, usedUlNumbers))
                {
                    equipment.UlNumber = GetNextUlNumber(usedUlNumbers, ref nextUlNumber);
                }

                if (!TryRegisterGlobalNumber(equipment.GlobalNumber, usedGlobalNumbers))
                {
                    equipment.GlobalNumber = GetNextGlobalNumber(usedGlobalNumbers, ref nextGlobalNumber);
                }
            }
        }

        private static void EnsureMinimumSampleEquipment(
            SeatInfo seatInfo,
            string itemType,
            HashSet<string> usedUlNumbers,
            HashSet<string> usedGlobalNumbers,
            ref int nextUlNumber,
            ref int nextGlobalNumber)
        {
            const int minimumEquipmentCount = 10;

            for (var index = seatInfo.Equipments.Count; index < minimumEquipmentCount; index++)
            {
                seatInfo.Equipments.Add(CreateSampleEquipment(
                    itemType,
                    index + 1,
                    usedUlNumbers,
                    usedGlobalNumbers,
                    ref nextUlNumber,
                    ref nextGlobalNumber));
            }
        }

        private static EquipmentInfo CreateSampleEquipment(
            string itemType,
            int number,
            HashSet<string> usedUlNumbers,
            HashSet<string> usedGlobalNumbers,
            ref int nextUlNumber,
            ref int nextGlobalNumber)
        {
            var itemPrefix = itemType switch
            {
                LackItemType => "Shelf",
                CartItemType => "Cart",
                _ => "Bench"
            };

            return new EquipmentInfo
            {
                Name = GetSampleEquipmentName(itemPrefix, number),
                UlNumber = GetNextUlNumber(usedUlNumbers, ref nextUlNumber),
                GlobalNumber = GetNextGlobalNumber(usedGlobalNumbers, ref nextGlobalNumber),
                Notes = "Sample data",
                PhotoPath = string.Empty
            };
        }

        private static string GetSampleEquipmentName(string itemPrefix, int number)
        {
            var names = itemPrefix switch
            {
                "Shelf" => ShelfSampleEquipmentNames,
                "Cart" => CartSampleEquipmentNames,
                _ => BenchSampleEquipmentNames
            };

            return names[(number - 1) % names.Length];
        }

        private static readonly string[] BenchSampleEquipmentNames =
        [
            "Digital Oscilloscope",
            "Spectrum Analyzer",
            "Signal Generator",
            "DC Power Supply",
            "Digital Multimeter",
            "LCR Meter",
            "Electronic Load",
            "Function Generator",
            "Network Analyzer",
            "Thermal Chamber",
            "Power Meter",
            "Frequency Counter",
            "Data Logger",
            "Clamp Meter",
            "Insulation Tester",
            "Withstand Voltage Tester",
            "Ground Bond Tester",
            "Leakage Current Tester",
            "EMI Test Receiver",
            "Temperature Recorder"
        ];

        private static readonly string[] ShelfSampleEquipmentNames =
        [
            "BNC Cable Set",
            "Banana Lead Set",
            "Current Probe",
            "Voltage Probe",
            "Thermocouple Wire",
            "USB Data Cable",
            "Calibration Adapter",
            "Power Cord Set",
            "Terminal Block",
            "Fuse Kit",
            "Connector Kit",
            "SMA Cable",
            "RF Attenuator",
            "Test Fixture",
            "Alligator Clip Set",
            "Patch Cable",
            "Ground Strap",
            "Probe Tip Set",
            "Label Cartridge",
            "Spare Battery Pack"
        ];

        private static readonly string[] CartSampleEquipmentNames =
        [
            "Mobile Test Cart",
            "Portable Power Analyzer",
            "Laptop Docking Station",
            "Barcode Scanner",
            "Thermal Label Printer",
            "Portable Monitor",
            "Battery Charger",
            "Tool Tray",
            "Safety Interlock Box",
            "Portable Light Meter",
            "Handheld Tachometer",
            "Inspection Camera",
            "ESD Wrist Strap Tester",
            "Portable Scale",
            "Torque Driver Set",
            "Wireless Router",
            "Cable Reel",
            "Small Parts Organizer",
            "Emergency Stop Box",
            "Portable UPS"
        ];

        private static bool TryRegisterUlNumber(string ulNumber, HashSet<string> usedUlNumbers)
        {
            return IsValidUlNumber(ulNumber) && usedUlNumbers.Add(ulNumber.Trim());
        }

        private static bool TryRegisterGlobalNumber(string globalNumber, HashSet<string> usedGlobalNumbers)
        {
            return IsValidGlobalNumber(globalNumber) && usedGlobalNumbers.Add(globalNumber.Trim());
        }

        private static bool IsValidUlNumber(string ulNumber)
        {
            const string prefix = "UL-S-";

            if (string.IsNullOrWhiteSpace(ulNumber) ||
                !ulNumber.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var suffix = ulNumber.Trim()[prefix.Length..];
            return suffix.Length == 3 && suffix.All(char.IsDigit);
        }

        private static bool IsValidGlobalNumber(string globalNumber)
        {
            var trimmed = globalNumber.Trim();
            return trimmed.Length == 6 && trimmed[0] == '2' && trimmed.All(char.IsDigit);
        }

        private static string GetNextUlNumber(HashSet<string> usedUlNumbers, ref int nextUlNumber)
        {
            while (nextUlNumber <= 999)
            {
                var candidate = $"UL-S-{nextUlNumber:000}";
                nextUlNumber++;
                if (usedUlNumbers.Add(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("UL-S equipment number range is exhausted.");
        }

        private static string GetNextGlobalNumber(HashSet<string> usedGlobalNumbers, ref int nextGlobalNumber)
        {
            while (nextGlobalNumber <= 299999)
            {
                var candidate = nextGlobalNumber.ToString();
                nextGlobalNumber++;
                if (usedGlobalNumbers.Add(candidate))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("Global equipment number range is exhausted.");
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
