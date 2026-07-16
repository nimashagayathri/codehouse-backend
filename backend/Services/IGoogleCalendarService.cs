using System;
using System.Threading.Tasks;

namespace RecruitmentPlatform.API.Services
{
    public class CalendarEventResult
    {
        public bool Success { get; set; }
        public string EventId { get; set; } = string.Empty;
        public string HtmlLink { get; set; } = string.Empty;
        public string MeetLink { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public interface IGoogleCalendarService
    {
        Task<CalendarEventResult> CreateInterviewCalendarEventAsync(
            string summary,
            string description,
            string location,
            DateTime startDateTime,
            int durationMinutes,
            string candidateEmail,
            string recruiterEmail = "");
    }
}