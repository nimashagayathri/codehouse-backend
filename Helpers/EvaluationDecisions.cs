namespace RecruitmentPlatform.API.Helpers
{
    public static class EvaluationDecisions
    {
        public const string Pending = "Pending";
        public const string Recommended = "Recommended";
        public const string Rejected = "Rejected";
        public const string Hired = "Hired";

        public static readonly string[] AllDecisions =
        {
            Pending,
            Recommended,
            Rejected,
            Hired
        };

        public static string? Normalize(string decision)
        {
            if (string.IsNullOrWhiteSpace(decision))
                return null;

            string cleanedDecision = decision
                .Trim()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");

            return AllDecisions.FirstOrDefault(d =>
                string.Equals(d, cleanedDecision, StringComparison.OrdinalIgnoreCase));
        }
    }
}