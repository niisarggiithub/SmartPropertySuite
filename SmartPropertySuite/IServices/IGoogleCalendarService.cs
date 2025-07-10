using Google.Apis.Calendar.v3.Data;
using SmartPropertySuite.Models;

namespace SmartPropertySuite.IServices
{
    public interface IGoogleCalendarService
    {
        Task<List<TimeSlot>> GetFreeSlotsAsync(string priority);
        List<TimeSlot> CalculateAvailableSlots(DateTime start, DateTime end, IList<TimePeriod> busy, string email);
        Task BookAppointmentAsync(ChatState state, TimeSlot slot);
    }
}
