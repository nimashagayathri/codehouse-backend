namespace RecruitmentPlatform.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body, string? icsCalendarEvent = null);
    }
}
