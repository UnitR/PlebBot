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

    public static class ChartSpan
    {
        public static string Overall => "overall";
        public static string Week => "7day";
        public static string Month => "1month";
        public static string Quarter => "3month";
        public static string Half => "6month";
        public static string Year => "12month";
    }
}