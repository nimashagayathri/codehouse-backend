namespace RecruitmentPlatform.API.Helpers
{
    public static class InterviewStatuses
    {
        public const string Scheduled = "Scheduled";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        public static readonly string[] AllStatuses =
        {
            Scheduled,
            Completed,
            Cancelled
        };

        public static string? Normalize(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return null;

            string cleanedStatus = status
                .Trim()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");

            return AllStatuses.FirstOrDefault(s =>
                string.Equals(s, cleanedStatus, StringComparison.OrdinalIgnoreCase));
        }
    }
}