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