using Calendar;
using System;

namespace EventScheduler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var pathToEventsFile = args[0];
            var initialStartDayDate = args[1];

            var eventCalendar = new EventCalendar()
            {
                DailyStartTime = "09:00",
                DailyEndTime = "17:00",
                LunchDuration = 1,
                LunchTimeStartLowerBound = "12:00",
                LunchTimeStartUpperBound = "13:00",
                StartDayDate = initialStartDayDate,
            };

            eventCalendar.Parse(pathToEventsFile);

            // :TODO:
            // Your scheduling code starts here.
            // Rule 2: If the opening day falls on a Saturday or Sunday, the convention shall be rescheduled to start on the following Monday.
            DateTime startDate = DateTime.Parse(eventCalendar.StartDayDate);
            
            while (startDate.DayOfWeek == DayOfWeek.Saturday || startDate.DayOfWeek == DayOfWeek.Sunday)
            {
                startDate = startDate.AddDays(1);
            }
            
            eventCalendar.SetStartDayDate(startDate.ToString("yyyy-MM-dd"));

            // Rule 3: Ensure final day is not a weekend.
            int maxDay = 0;
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                int d = eventCalendar.GetEvent(i).GetEventDay();
                if (d > maxDay) maxDay = d;
            }
            
            DateTime finalDate = DateTime.Parse(eventCalendar.StartDayDate).AddDays(maxDay);
            if (finalDate.DayOfWeek == DayOfWeek.Saturday || finalDate.DayOfWeek == DayOfWeek.Sunday)
            {
                for (int i = 0; i < eventCalendar.EventCount(); i++)
                {
                    var e = eventCalendar.GetEvent(i);
                    e.SetEventDay(e.GetEventDay() + 2); // shift forward 2 days
                }
            }

            // :END:

            // Print the event calendar to standard output.
            var calendarStartDayDate = eventCalendar.StartDayDate;
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                Console.WriteLine(eventCalendar.GetEvent(i).ToDebugString(calendarStartDayDate));
            }
        }
    }

}



