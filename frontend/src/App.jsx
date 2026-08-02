import React, { useState } from 'react';

// Αυτή είναι η δομή δεδομένων. 
// Μπορείς να αντικαταστήσεις το περιεχόμενο αυτού του πίνακα 
// με το περιεχόμενο του events.json αρχείου σου!
const eventsData = [
  {
    "Title": "Groovy nights | Summer Closing Party with Shows",
    "Type": "Swing",
    "Date": "2026-06-20",
    "Time": "21:00",
    "Location": "Salaminos 7, Aigaleo, Athens",
    "Price": "10€",
    "EventUrl": "#",
    "ImageUrl": "https://images.unsplash.com/photo-1541532735798-05180cf0656a?q=80&w=600&auto=format&fit=crop",
    "SchoolName": "Groove in Athens"
  },
  {
    "Title": "The Grand Opening Party",
    "Type": "Swing",
    "Date": "2026-09-13",
    "Time": "21:00",
    "Location": "Athens Lindy Hop Hub",
    "Price": "Free",
    "EventUrl": "#",
    "ImageUrl": "https://images.unsplash.com/photo-1516450360452-9312f5e86fc7?q=80&w=600&auto=format&fit=crop",
    "SchoolName": "Athens Lindy Hop"
  }
];

const getDaysInMonth = (month, year) => new Date(year, month, 0).getDate();
const getFirstDayOfMonth = (month, year) => new Date(year, month - 1, 1).getDay();

export default function App() {
  const [currentDate, setCurrentDate] = useState(new Date(2026, 5, 1));
  const [selectedEvent, setSelectedEvent] = useState(null);

  const currentMonth = currentDate.getMonth() + 1;
  const currentYear = currentDate.getFullYear();
  const daysInMonth = getDaysInMonth(currentMonth, currentYear);
  const firstDay = getFirstDayOfMonth(currentMonth, currentYear);
  const startDayPadding = firstDay === 0 ? 6 : firstDay - 1;

  const monthNames = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];

  const nextMonth = () => setCurrentDate(new Date(currentYear, currentMonth, 1));
  const prevMonth = () => setCurrentDate(new Date(currentYear, currentMonth - 2, 1));

  const renderCalendarDays = () => {
    const days = [];
    for (let i = 0; i < startDayPadding; i++) {
      days.push(<div key={`empty-${i}`} className="p-2 border border-gray-200 bg-gray-50 min-h-[100px]"></div>);
    }
    for (let i = 1; i <= daysInMonth; i++) {
      const dateString = `${currentYear}-${String(currentMonth).padStart(2, '0')}-${String(i).padStart(2, '0')}`;
      const dayEvents = eventsData.filter(e => e.Date === dateString);

      days.push(
        <div key={i} className="p-2 border border-gray-200 hover:bg-orange-50 min-h-[100px] flex flex-col transition-colors">
          <span className="text-sm font-bold text-gray-700">{i}</span>
          <div className="flex flex-col gap-1 mt-1">
            {dayEvents.map((event, idx) => (
              <button
                key={idx}
                onClick={() => setSelectedEvent(event)}
                className="bg-orange-500 text-white text-[10px] p-1 rounded hover:bg-orange-600 truncate"
              >
                {event.Title}
              </button>
            ))}
          </div>
        </div>
      );
    }
    return days;
  };

  return (
    <div className="min-h-screen bg-gray-100 p-8 font-sans">
      <div className="max-w-6xl mx-auto">
        <header className="flex justify-between items-center mb-8 bg-white p-6 rounded-2xl shadow-sm">
          <h1 className="text-3xl font-extrabold text-gray-900">Groovy<span className="text-orange-500">Calendar</span></h1>
          <div className="flex items-center gap-4">
            <button onClick={prevMonth} className="px-4 py-2 bg-gray-200 rounded-lg hover:bg-gray-300">Prev</button>
            <h2 className="text-xl font-bold w-32 text-center">{monthNames[currentMonth - 1]} {currentYear}</h2>
            <button onClick={nextMonth} className="px-4 py-2 bg-gray-200 rounded-lg hover:bg-gray-300">Next</button>
          </div>
        </header>

        <div className="flex flex-col lg:flex-row gap-8">
          <div className="flex-1 bg-white rounded-2xl shadow-sm border border-gray-200 overflow-hidden">
            <div className="grid grid-cols-7 bg-gray-100 border-b">
              {['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map(day => (
                <div key={day} className="py-2 text-center text-xs font-bold text-gray-500 uppercase">{day}</div>
              ))}
            </div>
            <div className="grid grid-cols-7 bg-white">
              {renderCalendarDays()}
            </div>
          </div>

          {selectedEvent && (
            <div className="lg:w-80 bg-white p-6 rounded-2xl shadow-lg h-fit sticky top-8">
              <img src={selectedEvent.ImageUrl} alt={selectedEvent.Title} className="w-full h-40 object-cover rounded-xl mb-4" />
              <h3 className="font-bold text-lg mb-2">{selectedEvent.Title}</h3>
              <p className="text-sm text-gray-600 mb-2">{selectedEvent.Date} | {selectedEvent.Time}</p>
              <p className="text-sm text-orange-600 font-bold mb-4">{selectedEvent.Price}</p>
              <button onClick={() => setSelectedEvent(null)} className="w-full py-2 bg-gray-800 text-white rounded-lg">Close</button>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}