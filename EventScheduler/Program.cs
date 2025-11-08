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

            var originalStartDate = DateTime.Parse(initialStartDayDate);
            if (originalStartDate.IsWeekend())
            {
                while (originalStartDate.IsWeekend())
                {
                    originalStartDate = originalStartDate.AddDays(1);
                }
                eventCalendar.SetStartDayDate(originalStartDate.ToString("yyyy-MM-dd"));
            }

            var eventOriginalDates = new Dictionary<int, DateTime>();
            var weekdayEvents = new List<int>();
            var weekendEvents = new List<int>();

            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                var eventDay = evt.GetEventDay();
                var originalDate = DateTime.Parse(eventCalendar.StartDayDate).AddDays(eventDay - 1);
                eventOriginalDates[i] = originalDate;

                if (originalDate.IsWeekend())
                    weekendEvents.Add(i);
                else
                    weekdayEvents.Add(i);
            }

            int maxOriginalDay = 0;
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                if (evt.GetEventDay() > maxOriginalDay)
                    maxOriginalDay = evt.GetEventDay();
            }

            var finalDate = DateTime.Parse(eventCalendar.StartDayDate).AddDays(maxOriginalDay - 1);
            if (finalDate.IsWeekend())
            {
                while (finalDate.IsWeekend())
                {
                    finalDate = finalDate.AddDays(1);
                }
                var shiftedStart = finalDate.AddDays(-maxOriginalDay + 1);
                eventCalendar.SetStartDayDate(shiftedStart.ToString("yyyy-MM-dd"));
                for (int i = 0; i < eventCalendar.EventCount(); i++)
                {
                    var evt = eventCalendar.GetEvent(i);
                    eventOriginalDates[i] = shiftedStart.AddDays(evt.GetEventDay() - 1);
                }
            }

            var weekdayEventsByDate = new Dictionary<DateTime, List<int>>();
            foreach (var eventIndex in weekdayEvents)
            {
                var d = eventOriginalDates[eventIndex];
                if (!weekdayEventsByDate.ContainsKey(d))
                    weekdayEventsByDate[d] = new List<int>();
                weekdayEventsByDate[d].Add(eventIndex);
            }

            foreach (var date in weekdayEventsByDate.Keys.ToList())
            {
                weekdayEventsByDate[date].Sort((a, b) =>
                {
                    var evtA = eventCalendar.GetEvent(a);
                    var evtB = eventCalendar.GetEvent(b);
                    var timeA = evtA.GetStartDateTime(eventCalendar.StartDayDate);
                    var timeB = evtB.GetStartDateTime(eventCalendar.StartDayDate);
                    return timeA.CompareTo(timeB);
                });
            }

            var scheduledEvents = new Dictionary<int, (DateTime date, string time)>();
            var dateOccupancy = new Dictionary<DateTime, List<(TimeSpan start, TimeSpan end)>>();

            foreach (var dateEntry in weekdayEventsByDate.OrderBy(kv => kv.Key))
            {
                var fixedDate = dateEntry.Key;
                var eventIndices = dateEntry.Value;

                if (!dateOccupancy.ContainsKey(fixedDate))
                    dateOccupancy[fixedDate] = new List<(TimeSpan, TimeSpan)>();

                foreach (var eventIndex in eventIndices)
                {
                    var evt = eventCalendar.GetEvent(eventIndex);
                    var duration = evt.GetDuration(eventCalendar.StartDayDate);

                    var slot = FindAvailableSlot(dateOccupancy[fixedDate], duration);
                    if (slot.HasValue)
                    {
                        dateOccupancy[fixedDate].Add((slot.Value, slot.Value.Add(TimeSpan.FromHours(duration))));
                        scheduledEvents[eventIndex] = (fixedDate, slot.Value.ToString(@"hh\:mm"));
                    }
                    else
                    {
                        Console.Error.WriteLine($"ERROR: Cannot fit event {evt.GetEventId()} on {fixedDate:yyyy-MM-dd}");
                    }
                }
            }

            foreach (var eventIndex in weekendEvents)
            {
                var evt = eventCalendar.GetEvent(eventIndex);
                var duration = evt.GetDuration(eventCalendar.StartDayDate);

                var currentDate = eventOriginalDates[eventIndex];

                bool scheduled = false;
                while (!scheduled)
                {
                    if (currentDate.IsWeekend())
                    {
                        currentDate = currentDate.AddDays(1);
                        continue;
                    }

                    if (!dateOccupancy.ContainsKey(currentDate))
                        dateOccupancy[currentDate] = new List<(TimeSpan start, TimeSpan end)>();

                    var slot = FindAvailableSlot(dateOccupancy[currentDate], duration);
                    if (slot.HasValue)
                    {
                        dateOccupancy[currentDate].Add((slot.Value, slot.Value.Add(TimeSpan.FromHours(duration))));
                        scheduledEvents[eventIndex] = (currentDate, slot.Value.ToString(@"hh\:mm"));
                        scheduled = true;
                    }
                    else
                    {
                        currentDate = currentDate.AddDays(1);
                    }
                }
            }

            Console.WriteLine("EventID,EventName,EventDate,StartTime");
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                var (date, time) = scheduledEvents[i];
                var dateString = date.ToString("yyyy-MM-dd");
                Console.WriteLine($"{evt.GetEventId()},{evt.GetEventName()},{dateString},{time}");
            }
        }

        static TimeSpan? FindAvailableSlot(List<(TimeSpan start, TimeSpan end)> occupiedSlots, double durationHours)
        {
            var dayStart = new TimeSpan(9, 0, 0);
            var dayEnd = new TimeSpan(17, 0, 0);
            var lunchStart = new TimeSpan(12, 0, 0);
            var lunchEnd = new TimeSpan(13, 0, 0);
            var duration = TimeSpan.FromHours(durationHours);

            var blockedTimes = new List<(TimeSpan start, TimeSpan end)>(occupiedSlots);

            bool lunchScheduled = blockedTimes.Any(slot => slot.start <= lunchStart && slot.end >= lunchEnd);

            if (!lunchScheduled)
            {
                for (var lunchTime = lunchStart; lunchTime <= new TimeSpan(13, 0, 0); lunchTime = lunchTime.Add(TimeSpan.FromMinutes(1)))
                {
                    var lunchSlot = (lunchTime, lunchTime.Add(TimeSpan.FromHours(1)));
                    bool conflicts = blockedTimes.Any(slot => !(lunchSlot.Item2 <= slot.start || lunchSlot.Item1 >= slot.end));
                    if (!conflicts && lunchTime.Add(TimeSpan.FromHours(1)) <= new TimeSpan(14, 0, 0))
                    {
                        blockedTimes.Add(lunchSlot);
                        break;
                    }
                }
            }

            blockedTimes.Sort((a, b) => a.start.CompareTo(b.start));

            var currentTime = dayStart;

            foreach (var blocked in blockedTimes)
            {
                if (currentTime + duration <= blocked.start && currentTime >= dayStart && currentTime + duration <= dayEnd)
                {
                    return currentTime;
                }
                currentTime = blocked.end > currentTime ? blocked.end : currentTime;
            }

            if (currentTime + duration <= dayEnd)
            {
                return currentTime;
            }

            return null;
        }
    }
}
