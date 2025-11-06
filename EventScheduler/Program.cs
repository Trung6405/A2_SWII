using Calendar;
using Calendar.Extensions;
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

            // Rule 3: If final day is weekend then move it to mon
            int maxDay = 0;
            for (int i=0; i < eventCalendar.EventCount(); i++){
                var evt = eventCalendar.GetEvent(i);
                if (evt.GetEventDay() > maxDay) {
                    maxDay = evt.GetEventDay();
                }
            }

            var finalDate = startDate.AddDays(maxDay - 1);
            if (finalDate.IsWeekend()){
                int daysToShift = 0;
                // Increment counter if on weekend
                while (finalDate.AddDays(daysToShift).IsWeekend())
                {
                    daysToShift++;
                }

                for (int i = 0; i < eventCalendar.EventCount(); i++){
                    var evt = eventCalendar.GetEvent(i);
                    evt.SetEventDay(evt.GetEventDay() + daysToShift);
                }
            }
            // :END:

            // Print the event calendar to the specified output in assignment
            Console.WriteLine("EventID,EventName,EventDate,StartTime");

    for (int i = 0; i < eventCalendar.EventCount(); i++)
        {
        var evt = eventCalendar.GetEvent(i);
    
            var actualDate = startDate.AddDays(evt.GetEventDay() - 1);
            var dateString = actualDate.ToString("yyyy-MM-dd");
    
            var startDateTime = evt.GetStartDateTime(eventCalendar.StartDayDate);
            var startTimeString = startDateTime.ToStartTimeFormat();
    
            Console.WriteLine($"{evt.GetEventId()},{evt.GetEventName()},{dateString},{startTimeString}");
            }
        }
    }

}



