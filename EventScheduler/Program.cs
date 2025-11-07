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
                    int daysToAdd = 1;
                    while (eventDate.AddDays(daysToAdd).IsWeekend())
                    {
                        daysToAdd++;
                    }
                    evt.SetEventDay(evt.GetEventDay() + daysToAdd);
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

            // Rule 5: Events must be scheduled to start and finish between 9am and 5pm in local time
                for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                
                var durationFromOpening = eventCalendar.DurationFromOpening(evt);
                if (durationFromOpening < 0)
                {
                    // if before 9am, move to 9
                    evt.SetStartTime("09:00");
                }
                
                var durationFromClosing = eventCalendar.DurationFromClosing(evt);
                if (durationFromClosing < 0)
                {
                    // try to start event earlier by checking latest start time 
                    var duration = evt.GetDuration(eventCalendar.StartDayDate);
                    var latestStart = DateTime.Parse($"{eventCalendar.StartDayDate} {eventCalendar.DailyEndTime}");
                    latestStart = latestStart.AddHours(-duration);
                    evt.SetStartTime(latestStart.ToStartTimeFormat());
                }
            }

              // Rule 1: No two events shall overlap
var eventsByDayForRule1 = new Dictionary<int, List<int>>();
for (int i = 0; i < eventCalendar.EventCount(); i++)
{
    var evt = eventCalendar.GetEvent(i);
    int day = evt.GetEventDay();
    
    if (!eventsByDayForRule1.ContainsKey(day))
    {
        eventsByDayForRule1[day] = new List<int>();
    }
    eventsByDayForRule1[day].Add(i);
}

// process day seperately
foreach (var day in eventsByDayForRule1.Keys.OrderBy(k => k).ToList())
{
    bool changesMade = true;
    
    while (changesMade)
    {
        changesMade = false;
        
        var eventIndices = eventsByDayForRule1[day];
        
        // sort by real start time
        eventIndices.Sort((a, b) =>
        {
            var evtA = eventCalendar.GetEvent(a);
            var evtB = eventCalendar.GetEvent(b);
            var actualDateA = startDate.AddDays(evtA.GetEventDay() - 1);
            var actualDateB = startDate.AddDays(evtB.GetEventDay() - 1);
            var timeA = evtA.GetStartDateTime(actualDateA.ToString("yyyy-MM-dd"));
            var timeB = evtB.GetStartDateTime(actualDateB.ToString("yyyy-MM-dd"));
            return timeA.CompareTo(timeB);
        });
        
        for (int i = 0; i < eventIndices.Count - 1; i++)
        {
            var idx1 = eventIndices[i];
            var idx2 = eventIndices[i + 1];
            
            var evt1 = eventCalendar.GetEvent(idx1);
            var evt2 = eventCalendar.GetEvent(idx2);
            
            var actualDate1 = startDate.AddDays(evt1.GetEventDay() - 1);
            var actualDate2 = startDate.AddDays(evt2.GetEventDay() - 1);

            var evt1End = evt1.GetEndDateTime(actualDate1.ToString("yyyy-MM-dd"));
            var evt2Start = evt2.GetStartDateTime(actualDate2.ToString("yyyy-MM-dd"));
            
            if (evt2Start < evt1End)
            {
                changesMade = true;
                
                var evt2Duration = evt2.GetDuration(actualDate2.ToString("yyyy-MM-dd"));
                var newEndTime = evt1End.AddHours(evt2Duration);
                
                var dailyEnd = DateTime.Parse($"{actualDate2:yyyy-MM-dd} {eventCalendar.DailyEndTime}");
                
                if (newEndTime <= dailyEnd)
                {
                    evt2.SetStartTime(evt1End.ToStartTimeFormat());
                }
                else
                {
                    evt2.SetEventDay(evt2.GetEventDay() + 1);
                    evt2.SetStartTime("09:00");
                    
                    eventIndices.RemoveAt(i + 1);
                    
                    int nextDay = evt2.GetEventDay();
                    if (!eventsByDayForRule1.ContainsKey(nextDay))
                    {
                        eventsByDayForRule1[nextDay] = new List<int>();
                    }
                    eventsByDayForRule1[nextDay].Add(idx2);
                }
                
                break;
            }
        }
    }
}

            // Rule 6: Must be a 1 hour lunch break during the day between 12 noon to 2 pm
            var eventsByDay = new Dictionary<int, List<Event>>();
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                var evt = eventCalendar.GetEvent(i);
                int day = evt.GetEventDay();
                
                if (!eventsByDay.ContainsKey(day))
                {
                    eventsByDay[day] = new List<Event>();
                }
                eventsByDay[day].Add(evt);
            }

            foreach (var day in eventsByDay.Keys.ToList())
            {
                var eventsOnDay = eventsByDay[day];
                
                eventsOnDay.Sort((a, b) =>
                {
                    var timeA = a.GetStartDateTime(eventCalendar.StartDayDate);
                    var timeB = b.GetStartDateTime(eventCalendar.StartDayDate);
                    return timeA.CompareTo(timeB);
                });

                bool hasLunchBreak = false;
                
                for (int i = 0; i < eventsOnDay.Count - 1; i++)
                {
                    var currentEvent = eventsOnDay[i];
                    var nextEvent = eventsOnDay[i + 1];
                    
                    var currentEnd = currentEvent.GetEndDateTime(eventCalendar.StartDayDate);
                    var nextStart = nextEvent.GetStartDateTime(eventCalendar.StartDayDate);
                    
                    var gapDuration = (nextStart - currentEnd).TotalHours;
                    
                    var lunchEarliestStart = DateTime.Parse($"{eventCalendar.StartDayDate} {eventCalendar.LunchTimeStartLowerBound}").AddDays(day - 1);
                    var lunchLatestEnd = DateTime.Parse($"{eventCalendar.StartDayDate} {eventCalendar.LunchTimeStartUpperBound}").AddHours(eventCalendar.LunchDuration).AddDays(day - 1);
                    
                    if (gapDuration >= eventCalendar.LunchDuration &&
                        nextStart > lunchEarliestStart &&
                        currentEnd < lunchLatestEnd)
                    {
                        hasLunchBreak = true;
                        break;
                    }
                }
                
                if (!hasLunchBreak && eventsOnDay.Count > 0)
                {
                    // Find best place to insert lunch break
                    for (int i = 0; i < eventsOnDay.Count; i++)
                    {
                        var evt = eventsOnDay[i];
                        var evtStart = evt.GetStartDateTime(eventCalendar.StartDayDate);
                        var evtEnd = evt.GetEndDateTime(eventCalendar.StartDayDate);
                        
                        var lunchStart = DateTime.Parse($"{eventCalendar.StartDayDate} 12:00").AddDays(day - 1);
                        
                        // If event overlaps with lunch time, move it
                        if (evtStart < lunchStart.AddHours(1) && evtEnd > lunchStart)
                        {
                            var newStart = lunchStart.AddHours(1);
                            evt.SetStartTime(newStart.ToStartTimeFormat());
                        }
                    }
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

