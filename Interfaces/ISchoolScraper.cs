using System.Collections.Generic;
using System.Threading.Tasks;
using GroovyCalendar.Models; 

namespace GroovyCalendar.Interfaces
{
    public interface ISchoolScraper
    {
        // Rule 1: Every scraper must tell us which school it's scraping
        string SchoolName { get; }

        // Rule 2: Every scraper must have a method that goes to the web,
        // grabs the data, and returns a List of our SwingEvent models.
        Task<List<SwingEvent>> ScrapeEventsAsync();
    }
}