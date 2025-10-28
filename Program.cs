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
            DateTime startDate = DateTime.Parse(initialStartDayDate);
            // Move to next Monday if start date is Saturday or Sunday
            if (startDate.IsWeekend()) {
                startDate = startDate.AddDays(1);
            }
            // Update the calendar's start day date
            string newStartDayDate = startDate.ToString("yyyy-MM-dd");
            eventCalendar.SetStartDayDate(newStartDayDate);

            // Rule 4: Move events that fall on weekends to weekdays.
            for (int i = 0; i < eventCalendar.EventCount(); i++) {
                var event = eventCalendar.GetEvent(i);
                int originalDay = event.GetEventDay();
                DateTime eventDate = startDate.AddDays(originalDay);

                // Number days to skip to reach the next weekday
                int daysToAdd = 0;
                DateTime checkDate = eventDate;
                while (checkDate.IsWeekend()) {
                    daysToAdd++;
                    checkDate = checkDate.AddDays(1);
                }

                if (daysToAdd > 0) {
                    int newEventDay = originalDay + daysToAdd;
                    event.SetEventDay(newEventDay);
                }
            }

            // Rule 3: Ensure final day is not a weekend.
            // Find the maximum event day after scheduling
            int maxEventDay = 0;
            for (int i = 0; i < eventCalendar.EventCount(); i++) {
                int day = eventCalendar.GetEvent(i).GetEventDay();
                if (day > maxEventDay) {
                    maxEventDay = day;
                }
            }

            // Check if the final day is a weekend
            DateTime finalDate = startDate.AddDays(maxEventDay);
            int finalDayAdjustment = 0;
            while (finalDate.IsWeekend()) {
                finalDayAdjustment++;
                finalDate = finalDate.AddDays(1);
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


