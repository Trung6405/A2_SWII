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

            // Rule 2: If opening day is on the weekend, move it to the next Monday.
            var startDate = DateTime.Parse(initialStartDayDate);
            if (startDate.IsWeekend())
            {
                while (startDate.IsWeekend())
                {
                    startDate = startDate.AddDays(1);
                }
                eventCalendar.SetStartDayDate(startDate.ToString("yyyy-MM-dd"));
            }

            int maxEventDay = 0;
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                if (evt.GetEventDay() > maxEventDay)
                {
                    maxEventDay = evt.GetEventDay();
                }
            }

            // Rules 3 & 4
            var dayToDateMapping = new Dictionary<int, DateTime>();
            var currentDate = startDate;
            
            for (int day = 1; day <= maxEventDay; day++)
            {
                while (currentDate.IsWeekend())
                {
                    currentDate = currentDate.AddDays(1);
                }
                
                dayToDateMapping[day] = currentDate;
                currentDate = currentDate.AddDays(1);
            }

            var allEventIndices = new List<int>();
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                allEventIndices.Add(i);
            }
            
            allEventIndices.Sort((a, b) =>
            {
                var evtA = eventCalendar.GetEvent(a);
                var evtB = eventCalendar.GetEvent(b);
                
                int dayCompare = evtA.GetEventDay().CompareTo(evtB.GetEventDay());
                if (dayCompare != 0) return dayCompare;
                
                var timeA = evtA.GetStartDateTime(eventCalendar.StartDayDate);
                var timeB = evtB.GetStartDateTime(eventCalendar.StartDayDate);
                return timeA.CompareTo(timeB);
            });

            var scheduledSlots = new List<(DateTime date, DateTime start, DateTime end)>();

            foreach (var eventIndex in allEventIndices)
            {
                var evt = eventCalendar.GetEvent(eventIndex);
                var duration = evt.GetDuration(eventCalendar.StartDayDate);
                var originalDay = evt.GetEventDay();
                var targetDate = dayToDateMapping[originalDay];
                
                var originalStartTime = evt.GetStartDateTime(eventCalendar.StartDayDate);
                var proposedStart = new DateTime(targetDate.Year, targetDate.Month, targetDate.Day,
                    originalStartTime.Hour, originalStartTime.Minute, 0);

                var (finalDate, finalStart) = FindValidSlot(proposedStart, targetDate, duration, 
                    scheduledSlots, dayToDateMapping);

                int finalDay = GetOrCreateDayForDate(finalDate, dayToDateMapping);
                evt.SetEventDay(finalDay);
                evt.SetStartTime(finalStart.ToString("HH:mm"));

                scheduledSlots.Add((finalDate, finalStart, finalStart.AddHours(duration)));
            }

            Console.WriteLine("EventID,EventName,EventDate,StartTime");
            
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                var actualDate = dayToDateMapping[evt.GetEventDay()];
                var dateString = actualDate.ToString("yyyy-MM-dd");
                var eventStartDateTime = evt.GetStartDateTime(eventCalendar.StartDayDate);
                var startTime = eventStartDateTime.ToString("HH:mm");
                
                Console.WriteLine($"{evt.GetEventId()},{evt.GetEventName()},{dateString},{startTime}");
            }
        }

        static (DateTime date, DateTime startTime) FindValidSlot(
            DateTime proposedStart, 
            DateTime targetDate,
            double durationHours,
            List<(DateTime date, DateTime start, DateTime end)> scheduledSlots,
            Dictionary<int, DateTime> dayToDateMapping)
        {
            var currentDate = targetDate;
            
            var dayStart = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 9, 0, 0);
            var candidateStart = proposedStart < dayStart ? dayStart : proposedStart;

            int maxDaysToSearch = 30;
            int daysSearched = 0;

            while (daysSearched < maxDaysToSearch)
            {
                var dayEnd = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 17, 0, 0);
                
                var candidateTimes = GenerateCandidateTimes(currentDate, candidateStart, dayEnd);

                foreach (var candidateTime in candidateTimes)
                {
                    if (IsValidSlot(candidateTime, durationHours, currentDate, scheduledSlots))
                    {
                        return (currentDate, candidateTime);
                    }
                }

                currentDate = currentDate.AddDays(1);
                while (currentDate.IsWeekend())
                {
                    currentDate = currentDate.AddDays(1);
                }
                
                candidateStart = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, 9, 0, 0);
                daysSearched++;
            }

            return (currentDate, candidateStart);
        }

        static List<DateTime> GenerateCandidateTimes(DateTime date, DateTime startFrom, DateTime dayEnd)
        {
            var candidates = new List<DateTime>();
            var current = startFrom;
            
            while (current < dayEnd)
            {
                candidates.Add(current);
                current = current.AddMinutes(15);
            }
            
            return candidates;
        }

        static bool IsValidSlot(DateTime candidateStart, double durationHours, DateTime date, 
            List<(DateTime date, DateTime start, DateTime end)> scheduledSlots)
        {
            var candidateEnd = candidateStart.AddHours(durationHours);
            var dayEnd = new DateTime(date.Year, date.Month, date.Day, 17, 0, 0);

            // Rule 5
            if (candidateEnd > dayEnd)
            {
                return false;
            }

            // Rule 1: No two events shall overlap
            foreach (var slot in scheduledSlots.Where(s => s.date.Date == date.Date))
            {
                if (candidateStart < slot.end && candidateEnd > slot.start)
                {
                    return false;
                }
            }

            // Rule 6: Must be a 1 hour lunch break during the day between 12 noon to 2 pm
            if (!WouldAllowLunchBreak(date, candidateStart, candidateEnd, scheduledSlots))
            {
                return false;
            }

            return true;
        }

        static bool WouldAllowLunchBreak(DateTime date, DateTime newEventStart, DateTime newEventEnd,
            List<(DateTime date, DateTime start, DateTime end)> scheduledSlots)
        {
            var lunchWindowStart = new DateTime(date.Year, date.Month, date.Day, 12, 0, 0);
            var lunchWindowEnd = new DateTime(date.Year, date.Month, date.Day, 14, 0, 0);

            for (int minute = 0; minute <= 60; minute++)
            {
                var lunchStart = lunchWindowStart.AddMinutes(minute);
                var lunchEnd = lunchStart.AddHours(1);

                if (lunchEnd > lunchWindowEnd)
                {
                    break;
                }

                if (newEventStart < lunchEnd && newEventEnd > lunchStart)
                {
                    continue;
                }

                bool blocked = false;
                foreach (var slot in scheduledSlots.Where(s => s.date.Date == date.Date))
                {
                    if (slot.start < lunchEnd && slot.end > lunchStart)
                    {
                        blocked = true;
                        break;
                    }
                }

                if (!blocked)
                {
                    return true;
                }
            }

            return false;
        }

        static int GetOrCreateDayForDate(DateTime date, Dictionary<int, DateTime> dayToDateMapping)
        {
            foreach (var kvp in dayToDateMapping)
            {
                if (kvp.Value.Date == date.Date)
                {
                    return kvp.Key;
                }
            }

            int newDay = dayToDateMapping.Keys.Max() + 1;
            dayToDateMapping[newDay] = date;
            return newDay;
        }
    }
}