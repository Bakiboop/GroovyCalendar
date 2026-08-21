import React, { useState, useEffect, useRef } from 'react';

const getDaysInMonth = (month, year) => new Date(year, month, 0).getDate();
const getFirstDayOfMonth = (month, year) => new Date(year, month - 1, 1).getDay();

export default function App() {
  const [eventsData, setEventsData] = useState([]);
  const [currentDate, setCurrentDate] = useState(new Date());
  const [selectedEvent, setSelectedEvent] = useState(null);

  // Reference για να κάνουμε scroll στην κάρτα του event
  const eventCardRef = useRef(null);

  // Αυτόματο Scroll όταν ανοίγει ένα event 
  useEffect(() => {
    if (selectedEvent && eventCardRef.current) {
      setTimeout(() => {
        eventCardRef.current.scrollIntoView({ behavior: 'smooth', block: 'start' });
      }, 100);
    }
  }, [selectedEvent]);

  // --- Η ΛΟΓΙΚΗ ΤΟΥ SWIPE ΓΙΑ ΤΟ ΗΜΕΡΟΛΟΓΙΟ (Αλλαγή Μήνα) ---
  const [touchStart, setTouchStart] = useState(null);
  const [touchEnd, setTouchEnd] = useState(null);
  const minSwipeDistance = 120;

  const onTouchStart = (e) => {
    setTouchEnd(null);
    setTouchStart(e.targetTouches[0].clientX);
  };
  const onTouchMove = (e) => setTouchEnd(e.targetTouches[0].clientX);
  const onTouchEndHandler = () => {
    if (!touchStart || !touchEnd) return;
    const distance = touchStart - touchEnd;
    if (distance > minSwipeDistance) nextMonth();
    if (distance < -minSwipeDistance) prevMonth();
  };
  // ---------------------------------------------------------

  // --- ΛΟΓΙΚΗ: SWIPE ΠΑΝΩ ΣΤΟ EVENT ΚΑΡΤΕΛΑΚΙ ---
  const [eventTouchStart, setEventTouchStart] = useState(null);
  const [eventTouchEnd, setEventTouchEnd] = useState(null);
  const eventMinSwipeDistance = 50;

  const onEventTouchStart = (e) => {
    setEventTouchEnd(null);
    setEventTouchStart(e.targetTouches[0].clientX);
  };
  const onEventTouchMove = (e) => setEventTouchEnd(e.targetTouches[0].clientX);
  const onEventTouchEndHandler = () => {
    if (!eventTouchStart || !eventTouchEnd || !selectedEvent) return;
    const distance = eventTouchStart - eventTouchEnd;
    const isLeftSwipe = distance > eventMinSwipeDistance;
    const isRightSwipe = distance < -eventMinSwipeDistance;

    if (isLeftSwipe || isRightSwipe) {
      const sortedEvents = [...eventsData].sort((a, b) => {
        if (a.Date !== b.Date) return a.Date.localeCompare(b.Date);
        return (a.Time || "").localeCompare(b.Time || "");
      });

      const currentIndex = sortedEvents.findIndex(e => e.Title === selectedEvent.Title && e.Date === selectedEvent.Date);

      if (isLeftSwipe && currentIndex >= 0 && currentIndex < sortedEvents.length - 1) {
        setSelectedEvent(sortedEvents[currentIndex + 1]);
      }
      if (isRightSwipe && currentIndex > 0) {
        setSelectedEvent(sortedEvents[currentIndex - 1]);
      }
    }
  };
  // ---------------------------------------------------------

  useEffect(() => {
    fetch('/events.json')
      .then((response) => response.json())
      .then((data) => {
        setEventsData(data);
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
      days.push(<div key={`empty-${i}`} className="p-2 border-r-2 border-b-2 border-[#362c28] bg-[#ebdcc5]/60 min-h-[100px]"></div>);
    }
    for (let i = 1; i <= daysInMonth; i++) {
      const dateString = `${currentYear}-${String(currentMonth).padStart(2, '0')}-${String(i).padStart(2, '0')}`;
      const dayEvents = eventsData.filter(e => e.Date === dateString);

      days.push(
        <div key={i} className="p-2 border-r-2 border-b-2 border-[#362c28] bg-[#fcf8f2] hover:bg-[#f7efe1] min-h-[100px] flex flex-col transition-colors">
          <span className="text-xl font-black text-[#362c28]">{i}</span>

          <div className="flex flex-row flex-wrap gap-2 mt-2">
            {dayEvents.map((event, idx) => (
              <div
                key={idx}
                onClick={() => setSelectedEvent(event)}
                title={`${event.Title} @ ${event.SchoolName}`}
                className="w-8 h-8 md:w-10 md:h-10 border-2 border-[#362c28] rounded-sm cursor-pointer hover:-translate-y-0.5 transition-all shadow-[2px_2px_0_0_#362c28] overflow-hidden flex-shrink-0 bg-[#ebdcc5]"
              >
                <img
                  src={event.ImageUrl}
                  alt={event.Title}
                  referrerPolicy="no-referrer"
                  onError={(e) => {
                    e.target.onerror = null;
                    e.target.src = '/default-image.jpg';
                  }}
                  className="w-full h-full block object-cover sepia-[0.2] contrast-105"
                />
              </div>
            ))}
          </div>
        </div>
      );
    }
    return days;
  };

  return (
    <div className="min-h-screen bg-[#ebdcc5] p-4 md:p-8 font-sans text-[#362c28] selection:bg-[#d4735e] selection:text-[#fcf8f2]">

      <style>{`
        @keyframes slideFadeIn {
          0% { opacity: 0; transform: translateY(15px); }
          100% { opacity: 1; transform: translateY(0); }
        }
        .animate-month-change {
          animation: slideFadeIn 0.3s ease-out forwards;
        }
      `}</style>

      <div className="max-w-6xl mx-auto">

        <header className="flex flex-col md:flex-row justify-between items-center gap-6 mb-8 bg-[#5e9596] p-6 rounded-xl border-4 border-[#362c28] shadow-[8px_8px_0_0_#362c28]">
          <div className="flex flex-col items-center md:items-start transform -rotate-1">
            <h1 className="text-4xl md:text-6xl font-black text-[#fcf8f2] tracking-tighter drop-shadow-[3px_3px_0_#362c28]">
              Groovy<span className="text-[#e8a56f]">Calendar</span>
            </h1>
            <span className="text-xs md:text-sm font-bold tracking-widest mt-2 text-[#362c28] bg-[#e8a56f] px-2 py-0.5 border-2 border-[#362c28]">
              Find Your Next Social
            </span>
          </div>

          <div className="flex items-center gap-4 bg-[#fcf8f2] p-2 rounded-lg border-4 border-[#362c28] shadow-[4px_4px_0_0_#362c28]">
            <button onClick={prevMonth} className="px-4 py-2 bg-[#e8a56f] text-[#362c28] border-2 border-[#362c28] font-black rounded hover:bg-[#d4735e] hover:text-[#fcf8f2] text-xs transition-colors shadow-[2px_2px_0_0_#362c28] active:translate-y-1 active:translate-x-1 active:shadow-none">
              Prev
            </button>
            <div className="flex flex-col items-center justify-center w-32">
              <div className="bg-[#362c28] text-[#e8a56f] font-black px-3 py-1 rounded-sm border-2 border-[#362c28] shadow-[2px_2px_0_0_#d4735e] uppercase tracking-widest mb-1">
                {monthNames[currentMonth - 1]}
              </div>
              <span className="text-sm font-bold text-[#d4735e]">{currentYear}</span>
            </div>
            <button onClick={nextMonth} className="px-4 py-2 bg-[#e8a56f] text-[#362c28] border-2 border-[#362c28] font-black rounded hover:bg-[#d4735e] hover:text-[#fcf8f2] text-xs transition-colors shadow-[2px_2px_0_0_#362c28] active:translate-y-1 active:translate-x-1 active:shadow-none">
              Next
            </button>
          </div>
        </header>

        {/* 🌟 ΕΔΩ ΑΦΑΙΡΕΘΗΚΕ ΤΟ REVERSE: flex-col αντί για flex-col-reverse */}
        <div className="flex flex-col lg:flex-row gap-10">

          <div
            key={currentMonth}
            className="flex-1 animate-month-change bg-[#fcf8f2] rounded-xl border-4 border-[#362c28] shadow-[8px_8px_0_0_#362c28] overflow-hidden"
            onTouchStart={onTouchStart}
            onTouchMove={onTouchMove}
            onTouchEnd={onTouchEndHandler}
          >
            <div className="w-full">
              <div className="grid grid-cols-7 bg-[#d4735e] border-b-4 border-[#362c28]">
                {['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map(day => (
                  <div key={day} className="py-3 text-center text-xs md:text-sm font-black text-[#fcf8f2] uppercase tracking-widest">{day}</div>
                ))}
              </div>
              <div className="grid grid-cols-7 bg-[#fcf8f2]">
                {renderCalendarDays()}
              </div>
            </div>
          </div>

          {selectedEvent && (
            <div
              ref={eventCardRef}
              onTouchStart={onEventTouchStart}
              onTouchMove={onEventTouchMove}
              onTouchEnd={onEventTouchEndHandler}
              className="lg:w-80 bg-[#fcf8f2] p-6 rounded-xl border-4 border-[#362c28] shadow-[8px_8px_0_0_#362c28] h-fit transform rotate-1 transition-transform"
            >
              <div className="flex items-center gap-3 mb-4 text-[#d4735e] border-b-4 border-[#362c28] pb-4">
                <span className="text-3xl filter drop-shadow-[2px_2px_0_#362c28]">🎸</span>
                <span className="font-black uppercase tracking-wider text-sm text-[#362c28]">
                  {selectedEvent.SchoolName}
                </span>
              </div>

              <div className="w-full mb-6 overflow-hidden border-4 border-[#2c1e16] shadow-[4px_4px_0_0_#2c1e16] flex bg-[#2c1e16]">
                <img
                  src={selectedEvent.ImageUrl}
                  alt={selectedEvent.Title}
                  referrerPolicy="no-referrer"
                  onError={(e) => {
                    e.target.onerror = null;
                    e.target.src = '/default-image.jpg';
                  }}
                  className="w-full h-auto block object-cover sepia-[0.2] contrast-105 pointer-events-none"
                />
              </div>

              <h3 className="font-black text-2xl mb-4 text-[#362c28] leading-tight">{selectedEvent.Title}</h3>

              <div className="flex flex-col gap-3 mt-4 font-bold text-sm bg-[#ebdcc5]/60 p-4 border-2 border-[#362c28] rounded-lg shadow-[inset_2px_2px_0_0_#362c28]">
                <div className="flex items-start gap-3 text-[#362c28]">
                  <span className="text-xl leading-none mt-0.5">⏰</span>
                  <div className="flex items-center gap-2">
                    <span className="bg-[#362c28] text-[#e8a56f] px-2 py-0.5 rounded-sm border-2 border-[#362c28] font-black uppercase text-xs shadow-[2px_2px_0_0_rgba(0,0,0,0.2)]">
                      {selectedEvent.Date}
                    </span>
                    <span className="font-bold text-sm">{selectedEvent.Time}</span>
                  </div>
                </div>

                <div className="flex items-start gap-3 text-[#362c28]">
                  <span className="text-xl leading-none mt-0.5">📍</span>
                  <span className="leading-tight">{selectedEvent.Location}</span>
                </div>

                <div className="flex items-start gap-3 text-[#d4735e] font-black text-xs md:text-sm pt-1">
                  <span className="text-xl leading-none mt-0.5">🎟️</span>
                  <span className="leading-tight mt-1.5">{selectedEvent.Price}</span>
                </div>
              </div>

              <div className="flex gap-4 mt-6">
                <a
                  href={selectedEvent.EventUrl}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="flex-1 text-center py-3 bg-[#d4735e] text-[#fcf8f2] border-2 border-[#362c28] hover:bg-[#bd634e] font-black transition-all text-sm tracking-widest shadow-[4px_4px_0_0_#362c28] rounded-lg active:translate-y-1 active:translate-x-1 active:shadow-none uppercase"
                >
                  Event Link
                </a>
                <button
                  onClick={() => setSelectedEvent(null)}
                  className="py-3 px-4 bg-[#fcf8f2] text-[#362c28] border-2 border-[#362c28] hover:bg-[#ebdcc5] font-black transition-all text-sm tracking-widest shadow-[4px_4px_0_0_#362c28] rounded-lg active:translate-y-1 active:translate-x-1 active:shadow-none uppercase"
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