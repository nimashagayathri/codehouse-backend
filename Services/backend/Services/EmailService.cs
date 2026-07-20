using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace RecruitmentPlatform.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(toEmail)) return;

            try
            {
                var email = new MimeMessage();
                email.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                email.To.Add(MailboxAddress.Parse(toEmail));
                email.Subject = subject;

                var builder = new BodyBuilder { HtmlBody = htmlBody };
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                // Gmail Port 587 හරහා Encrypted STARTTLS Connection එකක් සාදයි
                await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.Port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.Password);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email යැවීම අසාර්ථක විය: {ToEmail}", toEmail);
            }
        }

        // Candidate ට යවන ලස්සන HTML Interview Invite Template එක
        public async Task SendInterviewInvitationEmailAsync(
            string toEmail, string candidateName, string jobTitle, 
            DateTime interviewDate, string mode, string location, string meetingLink, string notes)
        {
            string subject = $"Interview Invitation for {jobTitle} - CodeHouse";
            
            string htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 12px; overflow: hidden;'>
                    <div style='background-color: #1e293b; padding: 20px; text-align: center; color: white;'>
                        <h2 style='margin: 0;'>CodeHouse Talent Acquisition</h2>
                    </div>
                    <div style='padding: 24px; color: #334155;'>
                        <p>Dear <strong>{candidateName}</strong>,</p>
                        <p>Your application for <strong>{jobTitle}</strong> has been shortlisted!</p>
                        <div style='background-color: #f8fafc; border-left: 4px solid #2563eb; padding: 16px; margin: 20px 0;'>
                            <p><strong>Date & Time:</strong> {interviewDate:f}</p>
                            <p><strong>Interview Mode:</strong> {mode}</p>
                            <p><strong>Location/Link:</strong> {location}</p>
                        </div>
                        {(!string.IsNullOrEmpty(meetingLink) ? $"<a href='{meetingLink}' style='background:#2563eb;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;display:inline-block;'>Join Google Meet Call</a>" : "")}
                        <br/><br/>
                        <p>Best regards,<br/><strong>CodeHouse Hiring Team</strong></p>
                    </div>
                </div>";

            await SendEmailAsync(toEmail, subject, htmlBody);
        }

        public async Task SendApplicationStatusUpdateEmailAsync(string toEmail, string candidateName, string jobTitle, string newStatus)
        {
            string subject = $"Application Status Update: {jobTitle}";
            string htmlBody = $"<h2>Hello {candidateName}</h2><p>Your application status for <strong>{jobTitle}</strong> is updated to <strong>{newStatus}</strong>.</p>";
            await SendEmailAsync(toEmail, subject, htmlBody);
        }
    }
}