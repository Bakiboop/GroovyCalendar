using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GroovyCalendar.Interfaces;
using GroovyCalendar.Models;
using GroovyCalendar.SchoolScrapers; // Notice we import the namespace where our scraper lives

namespace GroovyCalendar
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting GroovyCalendar Scraper...\n");

            var scrapers = new List<ISchoolScraper>
            {
                // new HoppersInAthens(),
                new GrooveInAthens()
            };

            var allEvents = new List<SwingEvent>();

            foreach (var scraper in scrapers)
            {
                var schoolEvents = await scraper.ScrapeEventsAsync();
                allEvents.AddRange(schoolEvents);
            }

            Console.WriteLine("\n=== FINAL CALENDAR ===");

            if (allEvents.Count == 0)
            {
                Console.WriteLine("No parties found today.");
            }
            else
            {
                foreach (var ev in allEvents)
                {
                    Console.WriteLine("--------------------------------------------------");
                    Console.WriteLine($"TITLE:    {ev.Title}");
                    Console.WriteLine($"TYPE:     {ev.Type}");
                    Console.WriteLine($"DATE:     {ev.Date} at {ev.Time}");
                    Console.WriteLine($"LOCATION: {ev.Location}");
                    Console.WriteLine($"PRICE:    {ev.Price}");
                    Console.WriteLine($"DJ:       {ev.Dj}");
                    Console.WriteLine($"URL:      {ev.EventUrl}");
                    Console.WriteLine($"IMAGE:    {ev.ImageUrl}");
                }
            }

            Console.WriteLine("--------------------------------------------------");
            Console.WriteLine("\nScraping finished!");
        }
    }
}