namespace RecruitmentPlatform.API.DTOs.Analytics
{
    public class HiringTrendResponse
    {
        public int Year { get; set; }

        public int Month { get; set; }

        public string MonthName { get; set; } = string.Empty;

        public int ApplicationsCount { get; set; }

        public int HiredCount { get; set; }
    }
}