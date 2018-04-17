namespace PlebBot.Services.Chart
{
    public enum ListType
    {
        Artists,
        Albums,
        Tracks
    }

    public enum ChartType
    {
        Artists,
        Albums
    }

    public enum ChartSize
    {
        Small = 3,
        Medium = 4,
        Large = 5
    }

    public struct ChartSpan
    {
        public const string Overall = "overall";
        public const string Week = "7day";
        public const string Month = "1month";
        public const string Quarter = "3month";
        public const string Half = "6month";
        public const string Year = "12month";
    }
}