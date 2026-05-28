using System.Collections.Generic;

namespace UL_project
{
    internal sealed class SeatInfo
    {
        public string SeatName { get; set; } = string.Empty;

        public string TeamName { get; set; } = string.Empty;

        public List<EquipmentInfo> Equipments { get; set; } = [];
    }
}
