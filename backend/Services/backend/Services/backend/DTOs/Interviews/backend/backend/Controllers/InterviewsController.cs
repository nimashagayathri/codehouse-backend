// Ashini's Google Calendar Integration
var calendarResult = await _calendarService.CreateInterviewCalendarEventAsync(
    summary: $"Interview: {jobTitle} - CodeHouse",
    description: $"Interview with {candidateName} for {jobTitle}.\nNotes: {request.Notes}",
    location: request.Location,
    startDateTime: request.InterviewDate,
    durationMinutes: 60,
    candidateEmail: candidateEmail,
    recruiterEmail: recruiterEmail
);

// Save generated Google Meet link into database
string finalMeetingLink = calendarResult.MeetLink;