using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace RecruitmentPlatform.API.Services
{
    public class GoogleCalendarService : IGoogleCalendarService
    {
        private readonly string _credentialsPath = "google-credentials.json";

        public async Task<CalendarEventResult> CreateInterviewCalendarEventAsync(
            string summary, string description, string location,
            DateTime startDateTime, int durationMinutes,
            string candidateEmail, string recruiterEmail = "")
        {
            DateTime endDateTime = startDateTime.AddMinutes(durationMinutes > 0 ? durationMinutes : 60);

            // 1. google-credentials.json ෆයිල් එකෙන් Google එකට Auth වෙනවා
            GoogleCredential credential;
            using (var stream = new FileStream(_credentialsPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(CalendarService.Scope.Calendar);
            }

            var service = new CalendarService(new BaseClientService.Initializer() {
                HttpClientInitializer = credential,
                ApplicationName = "CodeHouse Recruitment Platform"
            });

            // 2. Google Calendar Event එක හදනවා + Hangouts/Meet Link එකක් Request කරනවා
            var newEvent = new Event
            {
                Summary = summary,
                Description = description,
                Location = location,
                Start = new EventDateTime { DateTimeDateTimeOffset = startDateTime, TimeZone = "Asia/Colombo" },
                End = new EventDateTime { DateTimeDateTimeOffset = endDateTime, TimeZone = "Asia/Colombo" },
                Attendees = new List<EventAttendee> { new EventAttendee { Email = candidateEmail } },
                ConferenceData = new ConferenceData {
                    CreateRequest = new CreateConferenceRequest {
                        RequestId = Guid.NewGuid().ToString(),
                        ConferenceSolutionKey = new ConferenceSolutionKey { Type = "hangoutsMeet" } // Auto Google Meet Generator
                    }
                }
            };

            var request = service.Events.Insert(newEvent, "primary");
            request.ConferenceDataVersion = 1;
            var createdEvent = await request.ExecuteAsync();

            return new CalendarEventResult
            {
                Success = true,
                EventId = createdEvent.Id,
                HtmlLink = createdEvent.HtmlLink ?? string.Empty,
                MeetLink = createdEvent.HangoutLink ?? createdEvent.HtmlLink ?? location
            };
        }
    }
}