using Calendar;
using Calendar.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

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

            var startDate = DateTime.Parse(eventCalendar.StartDayDate);
            if (startDate.IsWeekend())
            {
                while (startDate.IsWeekend())
                {
                    startDate = startDate.AddDays(1);
                }
                eventCalendar.SetStartDayDate(startDate.ToString("yyyy-MM-dd"));
            }

            var eventsByOriginalDay = new Dictionary<int, List<int>>();
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                int originalDay = evt.GetEventDay();
                
                if (!eventsByOriginalDay.ContainsKey(originalDay))
                {
                    eventsByOriginalDay[originalDay] = new List<int>();
                }
                eventsByOriginalDay[originalDay].Add(i);
            }

            int currentSchedulingDay = 1;
            
            // each original day in order
            foreach (var originalDay in eventsByOriginalDay.Keys.OrderBy(k => k))
            {
                var eventIndices = eventsByOriginalDay[originalDay];
                
                eventIndices.Sort((a, b) =>
                {
                    var evtA = eventCalendar.GetEvent(a);
                    var evtB = eventCalendar.GetEvent(b);
                    var timeA = evtA.GetStartDateTime(eventCalendar.StartDayDate);
                    var timeB = evtB.GetStartDateTime(eventCalendar.StartDayDate);
                    return timeA.CompareTo(timeB);
                });

                var targetDate = startDate.AddDays(currentSchedulingDay - 1);
                while (targetDate.IsWeekend())
                {
                    currentSchedulingDay++;
                    targetDate = startDate.AddDays(currentSchedulingDay - 1);
                }

                DateTime currentTime = DateTime.Parse($"{targetDate:yyyy-MM-dd} 09:00");
                DateTime lunchStart = DateTime.Parse($"{targetDate:yyyy-MM-dd} 12:00");
                DateTime lunchEnd = lunchStart.AddHours(1);
                DateTime dayEnd = DateTime.Parse($"{targetDate:yyyy-MM-dd} 17:00");
                bool lunchScheduled = false;

                foreach (var eventIndex in eventIndices)
                {
                    var evt = eventCalendar.GetEvent(eventIndex);
                    var duration = evt.GetDuration(eventCalendar.StartDayDate);

                    if (!lunchScheduled && currentTime.AddHours(duration) > lunchStart && currentTime < lunchStart)
                    {
                        currentTime = lunchEnd;
                        lunchScheduled = true;
                    }

                    if (currentTime.AddHours(duration) > dayEnd)
                    {
                        currentSchedulingDay++;
                        targetDate = startDate.AddDays(currentSchedulingDay - 1);
                        
                        while (targetDate.IsWeekend())
                        {
                            currentSchedulingDay++;
                            targetDate = startDate.AddDays(currentSchedulingDay - 1);
                        }
                        
                        currentTime = DateTime.Parse($"{targetDate:yyyy-MM-dd} 09:00");
                        lunchStart = DateTime.Parse($"{targetDate:yyyy-MM-dd} 12:00");
                        lunchEnd = lunchStart.AddHours(1);
                        dayEnd = DateTime.Parse($"{targetDate:yyyy-MM-dd} 17:00");
                        lunchScheduled = false;
                    }

                    evt.SetEventDay(currentSchedulingDay);
                    evt.SetStartTime(currentTime.ToString("HH:mm"));
                    
                    currentTime = currentTime.AddHours(duration);

                    if (!lunchScheduled && currentTime >= lunchStart && currentTime < dayEnd)
                    {
                        if (currentTime < lunchEnd)
                        {
                            currentTime = lunchEnd;
                        }
                        lunchScheduled = true;
                    }
                }

                currentSchedulingDay++;
            }

            int maxDay = 0;
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                if (evt.GetEventDay() > maxDay)
                {
                    maxDay = evt.GetEventDay();
                }
            }

            var finalDate = startDate.AddDays(maxDay - 1);
            if (finalDate.IsWeekend())
            {
                int daysToShift = 0;
                while (finalDate.AddDays(daysToShift).IsWeekend())
                {
                    daysToShift++;
                }

                for (int i = 0; i < eventCalendar.EventCount(); i++)
                {
                    var evt = eventCalendar.GetEvent(i);
                    evt.SetEventDay(evt.GetEventDay() + daysToShift);
                }
            }

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