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
            
            // Rule 2: If opening day is on the weekend, move it to the next Monday.
            var startDate = DateTime.Parse(eventCalendar.StartDayDate);
            if (startDate.IsWeekend())
            {
                while (startDate.IsWeekend())
                {
                    startDate = startDate.AddDays(1);
                }
                eventCalendar.SetStartDayDate(startDate.ToString("yyyy-MM-dd"));
            }

            // Rule 4: Move events from weekends to weekdays.
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                var eventDate = startDate.AddDays(evt.GetEventDay() - 1);
                
                if (eventDate.IsWeekend())
                {
                    // Find next weekday
                    var daysToAdd = 0;
                    while (eventDate.AddDays(daysToAdd).IsWeekend())
                    {
                        daysToAdd++;
                    }
                    var newEventDay = evt.GetEventDay() + daysToAdd;
                    evt.SetEventDay(newEventDay);
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


