namespace PlebBot.Modules
{
    public static class LastFmError
    {
        public static string NotFound => "User not found.";
        public static string NotLinked => "You haven't linked your last.fm profile.";
        public static string InvalidSpan => "Invalid time span provided.";
        public static string Limit => "Check the given limit and try again.";
    }
}