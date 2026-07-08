namespace RecruitmentPlatform.API.Helpers
{
    public static class UserRoles
    {
        public const string Candidate = "Candidate";
        public const string Recruiter = "Recruiter";
        public const string HiringManager = "HiringManager";
        public const string Admin = "Admin";

        public static readonly string[] AllRoles =
        {
            Candidate,
            Recruiter,
            HiringManager,
            Admin
        };

        public static string? Normalize(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return null;

            string cleanedRole = role
                .Trim()
                .Replace(" ", "")
                .Replace("_", "")
                .Replace("-", "");

            return AllRoles.FirstOrDefault(r =>
                string.Equals(r, cleanedRole, StringComparison.OrdinalIgnoreCase));
        }
    }
}