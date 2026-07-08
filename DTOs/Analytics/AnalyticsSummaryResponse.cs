namespace RecruitmentPlatform.API.DTOs.Analytics
{
    public class AnalyticsSummaryResponse
    {
        public int TotalUsers { get; set; }

        public int TotalCandidates { get; set; }

        public int TotalRecruiters { get; set; }

        public int TotalHiringManagers { get; set; }

        public int TotalAdmins { get; set; }

        public int TotalJobs { get; set; }

        public int ActiveJobs { get; set; }

        public int TotalApplications { get; set; }

        public int SubmittedApplications { get; set; }

        public int ReviewedApplications { get; set; }

        public int ShortlistedApplications { get; set; }

        public int RejectedApplications { get; set; }

        public int HiredApplications { get; set; }

        public int TotalInterviews { get; set; }

        public int ScheduledInterviews { get; set; }

        public int CompletedInterviews { get; set; }

        public int CancelledInterviews { get; set; }

        public int TotalEvaluations { get; set; }

        public double AverageAiMatchScore { get; set; }

        public double AverageEvaluationScore { get; set; }
    }
}