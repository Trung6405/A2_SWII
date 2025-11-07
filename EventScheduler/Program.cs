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

            // Rule 1: No two events shall overlap
            for (int i = 0; i < eventCalendar.EventCount(); i++)
            {
                for (int j = i + 1; j < eventCalendar.EventCount(); j++)
                {
                    var evt1 = eventCalendar.GetEvent(i);
                    var evt2 = eventCalendar.GetEvent(j);
                    
                    //check if same day
                    if (evt1.GetEventDay() == evt2.GetEventDay())
                    {
                        if (eventCalendar.DoesOverlap(i, evt2))
                        {
                            // Second event moves after firstt
                            var evt1End = evt1.GetEndDateTime(eventCalendar.StartDayDate);
                            evt2.SetStartTime(evt1End.ToStartTimeFormat());
                        }
                    }
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
                    var endTime = evt.GetEndDateTime(eventCalendar.StartDayDate);
                    var startTime = evt.GetStartDateTime(eventCalendar.StartDayDate);
                    var duration = evt.GetDuration(eventCalendar.StartDayDate);
                    
                    var latestStart = DateTime.Parse($"{eventCalendar.StartDayDate} {eventCalendar.DailyEndTime}");
                    latestStart = latestStart.AddHours(-duration);
                    
                    evt.SetStartTime(latestStart.ToStartTimeFormat());
                }
            }

            // Rule 6: Must be a 1 hour lunch break during the day between 12 noon to 2 pm
            var eventsByDay = new Dictionary<int, List<Event>>();
            for (int i = 0; i < eventCalendar.EventCount(); i++) {
                var evt = eventCalendar.GetEvent(i);
                int day = evt.GetEventDay();
                    // groups each event by day
                if (!eventsByDay.ContainsKey(day)) {
                    eventsByDay[day] = new List<Event>();
                }
                eventsByDay[day].Add(evt);

            }

            foreach (var day in eventsByDay.Keys) {
                var eventsOnDay = eventsByDay[day];

                eventsOnDay.Sort((a,b) =>
                {
                    var timeA = a.GetStartDateTime(eventCalendar.StartDayDate);
                    var timeB = b.GetStartDateTime(eventCalendar.StartDayDate);
                    return timeA.CompareTo(timeB);
                });


                 for (int i = 0; i < eventsOnDay.Count - 1; i++)
                {
                    var currentEvent = eventsOnDay[i];
                    var nextEvent = eventsOnDay[i + 1];
                    
                    var currentEnd = currentEvent.GetEndDateTime(eventCalendar.StartDayDate);
                    var nextStart = nextEvent.GetStartDateTime(eventCalendar.StartDayDate);
                    
                    var gapStart = currentEnd;
                    var gapEnd = nextStart;
                    var gapDuration = (gapEnd - gapStart).TotalHours;
                    
                    var lunchEarliestStart = DateTime.Parse($"{eventCalendar.StartDayDate} {eventCalendar.LunchTimeStartLowerBound}");
                    var lunchLatestEnd = DateTime.Parse($"{eventCalendar.StartDayDate} {eventCalendar.LunchTimeStartUpperBound}").AddHours(eventCalendar.LunchDuration);
                    
                    lunchEarliestStart = lunchEarliestStart.AddDays(day - 1);
                    lunchLatestEnd = lunchLatestEnd.AddDays(day - 1);
                    
                    //check if gap witth lunch si long enough
                    if (gapDuration >= eventCalendar.LunchDuration &&
                        gapEnd > lunchEarliestStart &&
                        gapStart < lunchLatestEnd)
                    {
                        break;
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



