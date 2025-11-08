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
            var eventOriginalDates = new Dictionary<int, DateTime>();
            var weekdayEvents = new List<int>();
            var weekendEvents = new List<int>();
            
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                int eventDay = evt.GetEventDay();
                var originalDate = originalStartDate.AddDays(eventDay - 1);
                eventOriginalDates[i] = originalDate;
                
                if (originalDate.IsWeekend())
                {
                    weekendEvents.Add(i);
                }
                else
                {
                    weekdayEvents.Add(i);
                }
            }

            var startDate = DateTime.Parse(eventCalendar.StartDayDate);
            int startDateShift = 0;
            if (startDate.IsWeekend())
            {
                while (startDate.AddDays(startDateShift).IsWeekend())
                {
                    startDateShift++;
                }
                startDate = startDate.AddDays(startDateShift);
                eventCalendar.SetStartDayDate(startDate.ToString("yyyy-MM-dd"));
            }

            int maxOriginalDay = 0;
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                if (evt.GetEventDay() > maxOriginalDay)
                {
                    maxOriginalDay = evt.GetEventDay();
                }
            }

            var finalDate = startDate.AddDays(maxOriginalDay - 1);
            int endDateShift = 0;
            if (finalDate.IsWeekend())
            {
                while (finalDate.AddDays(endDateShift).IsWeekend())
                {
                    endDateShift++;
                }
                startDate = startDate.AddDays(endDateShift);
                eventCalendar.SetStartDayDate(startDate.ToString("yyyy-MM-dd"));
            }

            int totalShift = startDateShift + endDateShift;

            var adjustedDates = new Dictionary<int, DateTime>();
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var adjustedDate = eventOriginalDates[i].AddDays(totalShift);
                if (!eventOriginalDates[i].IsWeekend() && adjustedDate.IsWeekend())
                {
                    while (adjustedDate.IsWeekend())
                    {
                        adjustedDate = adjustedDate.AddDays(1);
                    }
                }
                adjustedDates[i] = adjustedDate;
            }

            var weekdayEventsByDate = new Dictionary<DateTime, List<int>>();
            foreach (var eventIndex in weekdayEvents)
            {
                var adjustedDate = adjustedDates[eventIndex];
                if (!weekdayEventsByDate.ContainsKey(adjustedDate))
                {
                    weekdayEventsByDate[adjustedDate] = new List<int>();
                }
                weekdayEventsByDate[adjustedDate].Add(eventIndex);
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

            weekendEvents.Sort((a, b) =>
            {
                var evtA = eventCalendar.GetEvent(a);
                var evtB = eventCalendar.GetEvent(b);
                
                int dayCompare = evtA.GetEventDay().CompareTo(evtB.GetEventDay());
                if (dayCompare != 0) return dayCompare;
                
                var timeA = evtA.GetStartDateTime(eventCalendar.StartDayDate);
                var timeB = evtB.GetStartDateTime(eventCalendar.StartDayDate);
                return timeA.CompareTo(timeB);
            });

            var scheduledEvents = new Dictionary<int, (DateTime date, string time)>();
            var dateOccupancy = new Dictionary<DateTime, List<(TimeSpan start, TimeSpan end)>>();

            foreach (var dateEntry in weekdayEventsByDate.OrderBy(kv => kv.Key))
            {
                var targetDate = dateEntry.Key;
                var eventIndices = dateEntry.Value;

                foreach (var eventIndex in eventIndices)
                {
                    var evt = eventCalendar.GetEvent(eventIndex);
                    var duration = evt.GetDuration(eventCalendar.StartDayDate);

                    bool scheduled = false;
                    var tryDate = targetDate;
                    
                    while (!scheduled)
                    {
                        if (!dateOccupancy.ContainsKey(tryDate))
                        {
                            dateOccupancy[tryDate] = new List<(TimeSpan, TimeSpan)>();
                        }

                        var slot = FindAvailableSlot(dateOccupancy[tryDate], duration);
                        
                        if (slot.HasValue)
                        {
                            dateOccupancy[tryDate].Add((slot.Value, slot.Value.Add(TimeSpan.FromHours(duration))));
                            scheduledEvents[eventIndex] = (tryDate, slot.Value.ToString(@"hh\:mm"));
                            scheduled = true;
                        }
                        else
                        {
                            tryDate = tryDate.AddDays(7);
                        }
                    }
                }
            }

            DateTime earliestWeekendDate = startDate;
            if (weekendEvents.Count > 0)
            {
                earliestWeekendDate = weekendEvents.Select(i => adjustedDates[i]).Min();
                while (earliestWeekendDate.IsWeekend())
                {
                    earliestWeekendDate = earliestWeekendDate.AddDays(1);
                }
            }
            
            var currentSchedulingDate = earliestWeekendDate;
            
            foreach (var eventIndex in weekendEvents)
            {
                var evt = eventCalendar.GetEvent(eventIndex);
                var duration = evt.GetDuration(eventCalendar.StartDayDate);

                bool scheduled = false;
                
                while (!scheduled)
                {
                    while (currentSchedulingDate.IsWeekend())
                    {
                        currentSchedulingDate = currentSchedulingDate.AddDays(1);
                    }

                    if (!dateOccupancy.ContainsKey(currentSchedulingDate))
                    {
                        dateOccupancy[currentSchedulingDate] = new List<(TimeSpan, TimeSpan)>();
                    }

                    var slot = FindAvailableSlot(dateOccupancy[currentSchedulingDate], duration);
                    
                    if (slot.HasValue)
                    {
                        dateOccupancy[currentSchedulingDate].Add((slot.Value, slot.Value.Add(TimeSpan.FromHours(duration))));
                        scheduledEvents[eventIndex] = (currentSchedulingDate, slot.Value.ToString(@"hh\:mm"));
                        scheduled = true;
                    }
                    else
                    {
                        currentSchedulingDate = currentSchedulingDate.AddDays(1);
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
            
            bool lunchScheduled = blockedTimes.Any(slot => 
                slot.start <= lunchStart && slot.end >= lunchEnd);
            
            if (!lunchScheduled)
            {
                for (var lunchTime = lunchStart; lunchTime <= new TimeSpan(13, 0, 0); lunchTime = lunchTime.Add(TimeSpan.FromMinutes(1)))
                {
                    var lunchSlot = (lunchTime, lunchTime.Add(TimeSpan.FromHours(1)));
                    bool conflicts = blockedTimes.Any(slot => 
                        !(lunchSlot.Item2 <= slot.start || lunchSlot.Item1 >= slot.end));
                    
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
