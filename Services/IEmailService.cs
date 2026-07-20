namespace RecruitmentPlatform.API.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendInterviewInvitationEmailAsync(
            string toEmail,
            string candidateName,
            string jobTitle,
            DateTime interviewDate,
            string mode,
            string location,
            string meetingLink,
            string notes);

        Task SendApplicationStatusUpdateEmailAsync(
            string toEmail,
            string candidateName,
            string jobTitle,
            string newStatus);
    }
}
