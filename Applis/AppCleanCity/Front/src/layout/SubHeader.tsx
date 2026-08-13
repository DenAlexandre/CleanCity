import { useEffect, useState } from 'react'
import { usePeriod } from '../period/PeriodContext'
import { fetchItineraries, itineraryKey, type Itinerary } from '../api/itinerariesApi'
import './SubHeader.css'

interface SubHeaderProps {
  title: string
}

const ALL_ITINERARIES_VALUE = '__all__'

// Numérotation propre à ce sélecteur (toutes périodes confondues, du plus récent au plus ancien,
// ordre déjà renvoyé par l'API) : indépendante de la numérotation de la page Itinéraires, qui elle
// ne porte que sur les itinéraires de la période sélectionnée.
function itineraryOptionLabel(itinerary: Itinerary, index: number): string {
  const date = new Date(itinerary.day).toLocaleDateString('fr-FR', { dateStyle: 'medium' })
  return `Itinéraire${index + 1} — ${date}`
}

// La plage de dates éditée ici reste locale tant que l'utilisateur n'a pas cliqué sur
// "Actualiser" : c'est à ce moment qu'elle est propagée au contexte partagé (PeriodContext),
// consommé par les pages qui filtrent leurs données par date.
export function SubHeader({ title }: SubHeaderProps) {
  const { period, setPeriod } = usePeriod()
  const [start, setStart] = useState(period.start)
  const [end, setEnd] = useState(period.end)
  const [itineraries, setItineraries] = useState<Itinerary[]>([])
  const [selectedItinerary, setSelectedItinerary] = useState(ALL_ITINERARIES_VALUE)

  useEffect(() => {
    fetchItineraries()
      .then(setItineraries)
      .catch(() => setItineraries([]))
  }, [])

  // Choisir un itinéraire est indépendant des dates saisies : ça affiche directement la journée
  // entière de cet itinéraire, sans attendre un clic sur "Actualiser".
  function handleSelectItinerary(key: string) {
    setSelectedItinerary(key)

    if (key === ALL_ITINERARIES_VALUE) {
      if (itineraries.length === 0) return
      const days = itineraries.map((i) => i.day).sort()
      const rangeStart = `${days[0]}T00:00`
      const rangeEnd = `${days[days.length - 1]}T23:59`
      setStart(rangeStart)
      setEnd(rangeEnd)
      setPeriod({ start: rangeStart, end: rangeEnd })
      return
    }

    const itinerary = itineraries.find((i) => itineraryKey(i) === key)
    if (!itinerary) return

    const dayStart = `${itinerary.day}T00:00`
    const dayEnd = `${itinerary.day}T23:59`
    setStart(dayStart)
    setEnd(dayEnd)
    setPeriod({ start: dayStart, end: dayEnd })
  }

  return (
    <div className="sub-header">
      <div className="sub-header-breadcrumb">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
          <path d="M3 11.5 12 4l9 7.5" />
          <path d="M5.5 10v9.5a1 1 0 0 0 1 1H9v-6h6v6h2.5a1 1 0 0 0 1-1V10" />
        </svg>
        <span>{title}</span>
      </div>

      <div className="sub-header-period">
        <span className="sub-header-period-label">Période</span>
        <input
          type="datetime-local"
          value={start}
          onChange={(e) => {
            setStart(e.target.value)
            setSelectedItinerary(ALL_ITINERARIES_VALUE)
          }}
        />
        <span>—</span>
        <input
          type="datetime-local"
          value={end}
          onChange={(e) => {
            setEnd(e.target.value)
            setSelectedItinerary(ALL_ITINERARIES_VALUE)
          }}
        />
        <select
          className="sub-header-itinerary-select"
          value={selectedItinerary}
          onChange={(e) => handleSelectItinerary(e.target.value)}
        >
          <option value={ALL_ITINERARIES_VALUE}>Tous les itinéraires</option>
          {itineraries.map((itinerary, index) => (
            <option key={itineraryKey(itinerary)} value={itineraryKey(itinerary)}>
              {itineraryOptionLabel(itinerary, index)}
            </option>
          ))}
        </select>
        <button
          type="button"
          className="sub-header-refresh"
          title="Actualiser"
          onClick={() => setPeriod({ start, end })}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
            <path d="M3 12a9 9 0 1 0 2.6-6.36M3 4v5h5" />
          </svg>
        </button>
      </div>
    </div>
  )
}
