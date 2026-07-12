using System;

namespace GroovyCalendar.Models
{
    public class SwingEvent
    {
        public string? Title { get; set; }
        public string? Date { get; set; }
        public string? Time { get; set; }
        public string? Location { get; set; }
        public required string SchoolName { get; set; }
        public string? EventUrl { get; set; }
        public string? Price { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public string? Type { get; set; }
        public string? Dj { get; set; }
    }
}