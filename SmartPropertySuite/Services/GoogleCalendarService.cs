using Google.Apis.Calendar.v3.Data;
using SmartPropertySuite.IServices;
using SmartPropertySuite.Models;
using System.Runtime.InteropServices;
using TimeZoneConverter;

namespace SmartPropertySuite.Services
{
    public class GoogleCalendarService : IGoogleCalendarService
    {
        private readonly ApplicationDbContext.ApplicationDbContext _dbContext;
        private readonly IGoogleCalendarFactory _googleCalendar;

        public GoogleCalendarService(ApplicationDbContext.ApplicationDbContext dbContext, IGoogleCalendarFactory googleCalendar)
        {
            _dbContext = dbContext;
            _googleCalendar = googleCalendar;
        }

        public async Task<List<TimeSlot>> GetFreeSlotsAsync(string priority)
        {
            try
            {
                var allSlots = new List<TimeSlot>();
                var tokens = _dbContext.CRMPropertySuiteUserInfo.ToList();

                int daysOffset = priority.ToLower() switch
                {
                    "high" => 0,
                    "medium" => 2,
                    "low" => 5,
                    _ => 0
                };

                var timeMin = DateTime.Now.Date.AddDays(daysOffset);
                var timeMax = timeMin.AddDays(3);

                foreach (var token in tokens)
                {
                    var credential = _googleCalendar.CreateCredential(token.Email, token.AccessToken, token.RefreshToken, token.ExpiryTime);
                    var calendarService = _googleCalendar.CreateCalendarService(credential);

                    var fbRequest = new FreeBusyRequest
                    {
                        TimeMin = timeMin,
                        TimeMax = timeMax,
                        TimeZone = TimeZoneInfo.Local.Id,
                        Items = new List<FreeBusyRequestItem> { new() { Id = "primary" } }
                    };

                    var fbResponse = await calendarService.Freebusy.Query(fbRequest).ExecuteAsync();
                    var busyTimes = fbResponse.Calendars["primary"].Busy;

                    var userSlots = CalculateAvailableSlots(timeMin, timeMax, busyTimes, token.Email);
                    allSlots.AddRange(userSlots);
                }

                // Sort all slots by start time and return first 15
                return allSlots.OrderBy(slot => slot.Start).ThenBy(slot => slot.TenantEmail).Take(15).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public List<TimeSlot> CalculateAvailableSlots(DateTime start, DateTime end, IList<TimePeriod> busy, string email)
        {
            try
            {
                var slots = new List<TimeSlot>();

                // Convert working hours to TimeSpans
                var workStart = new TimeSpan(9, 0, 0);     // 9:00 AM
                var lunchStart = new TimeSpan(13, 0, 0);   // 1:00 PM
                var lunchEnd = new TimeSpan(14, 0, 0);     // 2:00 PM
                var workEnd = new TimeSpan(18, 0, 0);      // 6:00 PM

                var roundedStart = new DateTime(start.Year, start.Month, start.Day, start.Hour, 0, 0).AddHours(start.Minute > 0 ? 1 : 0);
                var cursor = roundedStart;

                while (cursor < end && slots.Count < 5)
                {
                    // Skip weekends
                    if (cursor.DayOfWeek == DayOfWeek.Saturday || cursor.DayOfWeek == DayOfWeek.Sunday)
                    {
                        cursor = cursor.Date.AddDays(1).Add(workStart);
                        continue;
                    }

                    // Skip outside working hours or lunch break
                    if (cursor.TimeOfDay < workStart || cursor.TimeOfDay >= workEnd || (cursor.TimeOfDay >= lunchStart && cursor.TimeOfDay < lunchEnd))
                    {
                        // Move to next 60-min slot
                        cursor = cursor.AddMinutes(60);
                        continue;
                    }

                    var slotEnd = cursor.AddMinutes(60);

                    bool isBusy = busy.Any(b =>
                        cursor < b.End && slotEnd > b.Start
                    );

                    if (!isBusy && cursor >= DateTime.Now)
                    {
                        slots.Add(new TimeSlot
                        {
                            Start = cursor,
                            End = slotEnd,
                            TenantEmail = email
                        });
                    }

                    cursor = slotEnd;
                }

                return slots;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public async Task BookAppointmentAsync(ChatState state, TimeSlot slot)
        {
            try
            {
                var token = _dbContext.CRMPropertySuiteUserInfo.FirstOrDefault(x => x.Email == slot.TenantEmail);
                var credential = _googleCalendar.CreateCredential(token!.Email, token.AccessToken, token.RefreshToken, token.ExpiryTime);
                var calendarService = _googleCalendar.CreateCalendarService(credential);
                var ianaTimeZone = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? TZConvert.WindowsToIana(TimeZoneInfo.Local.Id) : TimeZoneInfo.Local.Id;

                var appointment = new Event
                {
                    Summary = $"Maintenance Visit: {CapitalizeFirstLetter(state.IssueType)}",
                    Description = $"""
                                Issue Type: {CapitalizeFirstLetter(state.IssueType)}
                                Priority: {CapitalizeFirstLetter(state.Priority)}
                                Requested By: {state.ContactEmail}
                                """,
                    Start = new EventDateTime
                    {
                        DateTime = slot.Start,
                        TimeZone = ianaTimeZone
                    },
                    End = new EventDateTime
                    {
                        DateTime = slot.End,
                        TimeZone = ianaTimeZone
                    },
                    GuestsCanInviteOthers = false,
                    GuestsCanModify = false,
                    GuestsCanSeeOtherGuests = false,
                    Reminders = new Event.RemindersData
                    {
                        UseDefault = false,
                        Overrides = new List<EventReminder>
                    {
                        new EventReminder { Method = "popup", Minutes = 30 },
                        new EventReminder { Method = "email", Minutes = 60 }
                    }
                    },
                    Status = "confirmed"
                };

                await calendarService.Events.Insert(appointment, "primary").ExecuteAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex);
            }
        }

        public static string CapitalizeFirstLetter(string input)
        {
            return char.ToUpperInvariant(input[0]) + input.Substring(1).ToLowerInvariant();
        }
    }
}
