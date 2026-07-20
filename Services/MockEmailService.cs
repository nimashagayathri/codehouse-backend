namespace RecruitmentPlatform.API.Services
{
    public class MockEmailService : IEmailService
    {
        public Task SendEmailAsync(string toEmail, string subject, string body, string? icsCalendarEvent = null)
        {
            // Simulate network delay
            Task.Delay(500).Wait();

            Console.WriteLine("\n=======================================================");
            Console.WriteLine("📧 MOCK EMAIL DISPATCHED (Communication Services Active)");
            Console.WriteLine("=======================================================");
            Console.WriteLine($"TO:      {toEmail}");
            Console.WriteLine($"SUBJECT: {subject}");
            Console.WriteLine("-------------------------------------------------------");
            Console.WriteLine(body);
            Console.WriteLine("=======================================================\n");

            return Task.CompletedTask;
        }
    }
}
