import React, { useState, useEffect } from 'react'; // Προσθέσαμε το useEffect

const getDaysInMonth = (month, year) => new Date(year, month, 0).getDate();
const getFirstDayOfMonth = (month, year) => new Date(year, month - 1, 1).getDay();

export default function App() {
  // 1. Νέο State για τα events (ξεκινάει άδειο)
  const [eventsData, setEventsData] = useState([]);

  const [currentDate, setCurrentDate] = useState(new Date(2026, 5, 1));
  const [selectedEvent, setSelectedEvent] = useState(null);

  // 2. Το useEffect τραβάει το JSON μόλις ανοίξει η σελίδα
  useEffect(() => {
    fetch('/events.json')
      .then((response) => response.json())
      .then((data) => {
        setEventsData(data);
        console.log("Events loaded successfully!", data);
      })
      .catch((error) => console.error("Error fetching events:", error));
  }, []);

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

      // Το eventsData τώρα είναι δυναμικό!
      const dayEvents = eventsData.filter(e => e.Date === dateString);

      days.push(
        <div key={i} className="p-2 border border-gray-200 hover:bg-orange-50 min-h-[100px] flex flex-col transition-colors">
          <span className="text-sm font-bold text-gray-700">{i}</span>
          <div className="flex flex-col gap-1 mt-1">
            {dayEvents.map((event, idx) => (
              <div
                key={idx}
                onClick={() => setSelectedEvent(event)}
                className="bg-orange-500 text-white p-1 md:p-1.5 rounded cursor-pointer hover:bg-orange-600 transition-colors shadow-sm mb-1"
              >
                <div className="text-[10px] md:text-xs font-bold truncate">{event.Title}</div>
                {/* Εδώ βάλαμε το όνομα της σχολής κάτω από τον τίτλο */}
                <div className="text-[9px] opacity-90 truncate">{event.SchoolName}</div>
              </div>
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

              {/* --- ΝΕΟ: Το Όνομα της Σχολής Πάνω Πάνω! --- */}
              <div className="flex items-center gap-2 mb-4 text-orange-600 border-b border-gray-100 pb-3">
                <span className="text-xl">🏫</span>
                <span className="font-extrabold uppercase tracking-wider text-sm">
                  {selectedEvent.SchoolName}
                </span>
              </div>
              {/* ------------------------------------------ */}

              {/* ------------------------------------------ */}

              {/* --- SMART THUMBNAIL LAYOUT --- */}
              <div className="w-full max-h-[320px] mb-4 rounded-xl overflow-hidden border border-gray-100 shadow-sm bg-gray-50 flex items-center justify-center p-1">
                <img
                  src={selectedEvent.ImageUrl}
                  alt={selectedEvent.Title}
                  className="max-w-full max-h-[310px] object-contain rounded-lg"
                />
              </div>

              <h3 className="font-bold text-lg mb-2 text-gray-900 leading-tight">{selectedEvent.Title}</h3>
              <div className="flex flex-col gap-1.5 mt-4">
                <p className="text-sm text-gray-600 flex items-center gap-2">
                  <span>📅</span> {selectedEvent.Date} | {selectedEvent.Time}
                </p>
                <p className="text-sm text-gray-600 flex items-center gap-2">
                  <span>📍</span> {selectedEvent.Location}
                </p>
                <p className="text-sm text-orange-600 font-bold flex items-center gap-2 mt-1">
                  <span>🎟️</span> {selectedEvent.Price}
                </p>
              </div>

              {/* Τα δύο κουμπιά */}
              <div className="flex gap-2 mt-6">
                <a
                  href={selectedEvent.EventUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex-1 text-center py-2.5 bg-gradient-to-r from-purple-500 to-pink-500 text-white rounded-lg font-semibold hover:opacity-90 transition-opacity text-sm shadow-sm"
                >
                  Event Link
                </a>
                <button
                  onClick={() => setSelectedEvent(null)}
                  className="flex-1 py-2.5 bg-gray-100 text-gray-700 font-semibold rounded-lg hover:bg-gray-200 transition-colors text-sm"
                >
                  Close
                </button>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}