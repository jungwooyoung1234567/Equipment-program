using System.Collections.Generic;

namespace UL_project
{
    internal sealed class LayoutState
    {
        public string LayoutTitle { get; set; } = "Research Lab Layout";

        public int ActiveMapIndex { get; set; }

        public List<MapState> Maps { get; set; } = [];
    }

    internal sealed class MapState
    {
        public string Name { get; set; } = string.Empty;

        public List<MapItemState> Items { get; set; } = [];
    }

    internal sealed class MapItemState
    {
        public string Type { get; set; } = "Seat";

        public double Left { get; set; }

        public double Top { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public int Rotation { get; set; }

        public SeatInfo SeatInfo { get; set; } = new();
    }
}
