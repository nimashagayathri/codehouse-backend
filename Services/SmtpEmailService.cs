using System.Net;
using System.Net.Mail;
using System.Text;

namespace RecruitmentPlatform.API.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, string? icsCalendarEvent = null)
        {
            var host = _configuration["EmailSettings:Host"] ?? "smtp.gmail.com";
            var portString = _configuration["EmailSettings:Port"] ?? "587";
            var email = _configuration["EmailSettings:Email"];
            var password = _configuration["EmailSettings:Password"];

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                // Fallback to avoid breaking if not configured
                Console.WriteLine($"[Mock Email] To: {toEmail}, Subject: {subject}");
                return;
            }

            int port = int.Parse(portString);

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(email, password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(email, "CodeHouse Recruitment"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false,
            };
            mailMessage.To.Add(toEmail);

            if (!string.IsNullOrEmpty(icsCalendarEvent))
            {
                byte[] icsBytes = Encoding.UTF8.GetBytes(icsCalendarEvent);
                var ms = new MemoryStream(icsBytes);
                var attachment = new Attachment(ms, "interview.ics", "text/calendar");
                mailMessage.Attachments.Add(attachment);
            }

            try
            {
                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Email Error] Failed to send email to {toEmail}: {ex.Message}");
            }
        }
    }
}
