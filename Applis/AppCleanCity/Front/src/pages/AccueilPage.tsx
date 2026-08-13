import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import {
  fetchCleanlinessScore,
  fetchDirtiestStreets,
  fetchPointOfInterestScores,
  fetchUrgentAlerts,
  type CleanlinessScore,
  type DirtiestStreet,
  type PointOfInterestCategoryScore,
  type UrgentAlert,
} from '../api/dashboardApi'
import { fetchItineraries, type Itinerary } from '../api/itinerariesApi'
import { usePeriod } from '../period/PeriodContext'
import { WeatherWidget } from '../components/WeatherWidget'
import { HistoryBarChart, type HistoryBarPoint } from '../components/HistoryBarChart'
import './AccueilPage.css'

const NATIONAL_SCORE = 3.8
const MS_PER_DAY = 24 * 60 * 60 * 1000

function formatScore(value: number | null): string {
  return value !== null ? value.toFixed(2).replace('.', ',') : '—'
}

function formatAlertDate(iso: string): string {
  return new Date(iso).toLocaleString('fr-FR', { dateStyle: 'short', timeStyle: 'short' })
}

function TrendArrow({ direction }: { direction: 1 | -1 }) {
  return (
    <svg width="76" height="76" viewBox="0 0 24 24" fill="currentColor" stroke="none">
      {direction > 0 ? (
        <path d="M12 2 22 12 17 12 17 22 7 22 7 12 2 12Z" />
      ) : (
        <path d="M12 22 2 12 7 12 7 2 17 2 17 12 22 12Z" />
      )}
    </svg>
  )
}

function NationalBadgeIcon() {
  return (
    <svg width="28" height="28" viewBox="0 0 24 24" stroke="none">
      <path fill="currentColor" d="M3 21V9a1 1 0 0 1 1-1h6v13H3Zm9 0V5a1 1 0 0 1 1-1h7a1 1 0 0 1 1 1v16h-9Z" />
      <rect x="5" y="11" width="1.8" height="1.8" fill="var(--color-primary)" />
      <rect x="8" y="11" width="1.8" height="1.8" fill="var(--color-primary)" />
      <rect x="5" y="15" width="1.8" height="1.8" fill="var(--color-primary)" />
      <rect x="8" y="15" width="1.8" height="1.8" fill="var(--color-primary)" />
      <rect x="14.5" y="7" width="1.8" height="1.8" fill="var(--color-primary)" />
      <rect x="18" y="7" width="1.8" height="1.8" fill="var(--color-primary)" />
      <rect x="14.5" y="11" width="1.8" height="1.8" fill="var(--color-primary)" />
      <rect x="18" y="11" width="1.8" height="1.8" fill="var(--color-primary)" />
      <rect x="14.5" y="15" width="1.8" height="1.8" fill="var(--color-primary)" />
      <rect x="18" y="15" width="1.8" height="1.8" fill="var(--color-primary)" />
    </svg>
  )
}

// Icône par mot-clé de catégorie (les catégories sont saisies librement page Paramètres, voir
// SUGGESTED_CATEGORIES dans PointsInteretPage.tsx) ; repli sur une icône générique sinon.
function categoryIcon(category: string) {
  const key = category.toLowerCase()
  if (key.includes('gare')) {
    return <path d="M12 3c-4 0-7 1-7 5v7a3 3 0 0 0 3 3h8a3 3 0 0 0 3-3V8c0-4-3-5-7-5ZM5 12h14M8 20l-2 2m12-2 2 2M8.5 16h0M15.5 16h0" />
  }
  if (key.includes('école') || key.includes('ecole')) {
    return <path d="M12 3 2 8l10 5 10-5-10-5ZM6 10.5V16c0 1.5 3 3 6 3s6-1.5 6-3v-5.5" />
  }
  if (key.includes('parc') || key.includes('square')) {
    return <path d="M12 2 7 10h3l-4 6h4v6h4v-6h4l-4-6h3z" />
  }
  if (key.includes('mairie')) {
    return <path d="M4 21h16M6 21V9l6-5 6 5v12M10 21v-5h4v5" />
  }
  if (key.includes('commerce')) {
    return <path d="M4 8h16l-1.5 11a2 2 0 0 1-2 2h-9a2 2 0 0 1-2-2L4 8ZM8 8V6a4 4 0 1 1 8 0v2" />
  }
  return <path d="M12 21s7-6.5 7-11.5A7 7 0 0 0 5 9.5C5 14.5 12 21 12 21ZM12 12a2.5 2.5 0 1 0 0-5 2.5 2.5 0 0 0 0 5Z" />
}

// Clic sur l'icône = raccourci vers le détail des objets détectés pour cette catégorie, sans
// avoir à repasser par le filtre manuellement dans Liste des mesures.
function CategoryIcon({ category }: { category: string }) {
  const navigate = useNavigate()

  function handleClick() {
    navigate(`/liste-mesures?tab=poi&category=${encodeURIComponent(category)}`)
  }

  return (
    <button type="button" className="accueil-category-icon" onClick={handleClick} title={`Voir les objets détectés — ${category}`}>
      <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth={1.8} strokeLinecap="round" strokeLinejoin="round">
        {categoryIcon(category)}
      </svg>
    </button>
  )
}

// Un slot par jour calendaire (pas par itinéraire) : les jours sans itinéraire restent visibles
// comme un trou dans le graphique au lieu de rapprocher artificiellement les barres voisines.
function buildDailyHistorySlots(itineraries: (Itinerary & { averageCci: number })[]): HistoryBarPoint[] {
  if (itineraries.length === 0) return []

  const averageByDay = new Map<string, number[]>()
  for (const itinerary of itineraries) {
    const values = averageByDay.get(itinerary.day) ?? []
    values.push(itinerary.averageCci)
    averageByDay.set(itinerary.day, values)
  }

  const sortedDays = [...averageByDay.keys()].sort()
  const firstDay = new Date(sortedDays[0])
  const lastDay = new Date(sortedDays[sortedDays.length - 1])
  const dayCount = Math.round((lastDay.getTime() - firstDay.getTime()) / MS_PER_DAY) + 1

  return Array.from({ length: dayCount }, (_, index) => {
    const date = new Date(firstDay.getTime() + index * MS_PER_DAY)
    const dayKey = date.toISOString().slice(0, 10)
    const values = averageByDay.get(dayKey)
    const value = values ? values.reduce((sum, v) => sum + v, 0) / values.length : null

    return {
      key: dayKey,
      value,
      valueLabel: formatScore(value),
      dateLabel: date.toLocaleDateString('fr-FR', { day: 'numeric', month: 'short' }),
      tooltip: `${date.toLocaleDateString('fr-FR')} — Note : ${formatScore(value)} /5`,
      showDateLabel: index === 0 || index === dayCount - 1 || index % 7 === 0,
    }
  })
}

export function AccueilPage() {
  const navigate = useNavigate()
  const { period } = usePeriod()
  const [score, setScore] = useState<CleanlinessScore | null>(null)
  const [itineraryHistory, setItineraryHistory] = useState<Itinerary[]>([])
  const [dirtiestStreets, setDirtiestStreets] = useState<DirtiestStreet[]>([])
  const [poiScores, setPoiScores] = useState<PointOfInterestCategoryScore[]>([])
  const [alerts, setAlerts] = useState<UrgentAlert[]>([])

  useEffect(() => {
    // Garde anti-course : une requête sur une large période (ex. "Tous les itinéraires") peut
    // répondre après une requête plus étroite lancée ensuite, et écraserait sinon les données
    // fraîches avec un résultat obsolète.
    let cancelled = false

    fetchCleanlinessScore(period.start, period.end)
      .then((data) => { if (!cancelled) setScore(data) })
      .catch(() => { if (!cancelled) setScore(null) })
    // Historique = un itinéraire par barre (dans l'ordre chronologique), avec sa note en info-bulle.
    fetchItineraries(period.start, period.end)
      .then((data) => { if (!cancelled) setItineraryHistory([...data].reverse()) })
      .catch(() => { if (!cancelled) setItineraryHistory([]) })
    fetchDirtiestStreets(period.start, period.end)
      .then((data) => { if (!cancelled) setDirtiestStreets(data) })
      .catch(() => { if (!cancelled) setDirtiestStreets([]) })
    fetchPointOfInterestScores(period.start, period.end)
      .then((data) => { if (!cancelled) setPoiScores(data) })
      .catch(() => { if (!cancelled) setPoiScores([]) })

    return () => {
      cancelled = true
    }
  }, [period])

  useEffect(() => {
    fetchUrgentAlerts().then(setAlerts).catch(() => setAlerts([]))
  }, [])

  const trend =
    score?.currentAverage != null && score.previousAverage != null
      ? Math.sign(score.currentAverage - score.previousAverage)
      : 0
  const historyPoints = itineraryHistory.filter((i): i is Itinerary & { averageCci: number } => i.averageCci !== null)

  return (
    <div className="accueil-page">
      <div className="accueil-topbar">
        <WeatherWidget />
      </div>

      <div className="accueil-grid">
        <div className="accueil-column">
          <div className="accueil-card accueil-score-card">
            <span className="accueil-card-label">Note de propreté</span>
            <div className="accueil-score-value">
              {trend !== 0 && (
                <span className={trend > 0 ? 'accueil-trend-up' : 'accueil-trend-down'}>
                  <TrendArrow direction={trend > 0 ? 1 : -1} />
                </span>
              )}
              <span>{formatScore(score?.currentAverage ?? null)} /5</span>
            </div>
          </div>

          <div className="accueil-card accueil-national-card">
            <span className="accueil-card-label">Note nationale</span>
            <div className="accueil-score-value accueil-score-value-small">
              <span>{formatScore(NATIONAL_SCORE)} /5</span>
              <span className="accueil-national-badge">
                <NationalBadgeIcon />
              </span>
            </div>
          </div>

          <div className="accueil-card">
            <h3>Historique des notes de propreté</h3>
            {historyPoints.length === 0 ? (
              <p className="accueil-empty">Aucune donnée sur la période.</p>
            ) : (
              <HistoryBarChart points={buildDailyHistorySlots(historyPoints)} />
            )}
          </div>
        </div>

        <div className="accueil-column">
          <div className="accueil-card">
            <h3>Centres d'intérêt</h3>
            {poiScores.length === 0 && <p className="accueil-empty">Aucun point d'intérêt pour le moment.</p>}
            <ul className="accueil-list accueil-poi-list">
              {poiScores.map((category) => (
                <li key={category.category}>
                  <span>{category.category}</span>
                  <CategoryIcon category={category.category} />
                  <span className="accueil-list-value">{formatScore(category.averageCci)} /5</span>
                </li>
              ))}
            </ul>
          </div>

          <div className="accueil-card">
            <h3>Top 5 des rues les plus sales</h3>
            {dirtiestStreets.length === 0 && <p className="accueil-empty">Aucune donnée sur la période.</p>}
            <ul className="accueil-list accueil-rues-list">
              {dirtiestStreets.map((street, index) => (
                <li key={street.street}>
                  <span className="accueil-list-street">{street.street}</span>
                  <button
                    type="button"
                    className="accueil-rank"
                    onClick={() => navigate(`/mesures?tab=map&street=${encodeURIComponent(street.street)}`)}
                    title={`Voir sur la carte — ${street.street}`}
                  >
                    {index + 1}
                  </button>
                  <span className="accueil-list-value">{formatScore(street.averageCci)} /5</span>
                </li>
              ))}
            </ul>
          </div>
        </div>

        <div className="accueil-column">
          <div className="accueil-card accueil-alerts-card">
            <h3>Top 5 des alarmes de la dernière tournée</h3>
            {alerts.length === 0 && <p className="accueil-empty">Aucune alarme récente.</p>}
            <ul className="accueil-alerts-list">
              {alerts.map((alert, index) => (
                <li key={index}>
                  <div className="accueil-alert-header">
                    <span className="accueil-alert-badge">Urgente</span>
                    <span className="accueil-alert-date">{formatAlertDate(alert.measuredAt)}</span>
                  </div>
                  <p>
                    Détection de {alert.count} {alert.typeName}
                    {alert.street ? ` sur ${alert.street}` : ''} (seuil : {alert.threshold}).
                  </p>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </div>
  )
}
