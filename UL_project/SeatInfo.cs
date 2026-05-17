namespace UL_project
{
    // 맵 위에 놓인 Seat/Lack 하나가 가지는 기본 데이터 모델.
    internal sealed class SeatInfo
    {
        // 화면 첫 줄에 표시되는 이름.
        public string SeatName { get; set; } = string.Empty;

        // 화면 둘째 줄에 표시되는 팀 또는 분류 이름.
        public string TeamName { get; set; } = string.Empty;
    }
}
