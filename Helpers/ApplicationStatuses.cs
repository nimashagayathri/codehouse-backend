namespace RecruitmentPlatform.API.Helpers
{
    public static class ApplicationStatuses
    {
        public const string Submitted = "Submitted";
        public const string Reviewed = "Reviewed";
        public const string Shortlisted = "Shortlisted";
        public const string Rejected = "Rejected";
        public const string Hired = "Hired";

        public static readonly string[] AllStatuses =
        {
            Submitted,
            Reviewed,
            Shortlisted,
            Rejected,
            Hired
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