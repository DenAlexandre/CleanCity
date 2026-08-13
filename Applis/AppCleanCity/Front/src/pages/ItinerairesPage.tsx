import { useEffect, useMemo, useState } from 'react'
import {
  fetchItineraries,
  fetchItineraryStreets,
  itineraryKey,
  type Itinerary,
  type ItineraryStreetDetail,
} from '../api/itinerariesApi'
import { usePeriod } from '../period/PeriodContext'
import './ItinerairesPage.css'

type StreetSortColumn = 'street' | 'averageCci'
type SortDirection = 'asc' | 'desc'

function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('fr-FR', { dateStyle: 'medium' })
}

// itinerary.itineraryIndex redémarre à 1 chaque jour (numérotation par jour/suitcase) : la
// plupart des jours n'ayant qu'un seul itinéraire, il faudrait sinon afficher "Itinéraire1" pour
// presque toutes les lignes. On utilise donc un identifiant global, unique sur toute la liste.
function itineraryLabel(globalId: number): string {
  return `Itinéraire${globalId}`
}

const OBJECTS_PREVIEW_COUNT = 4

function formatObjects(objects: ItineraryStreetDetail['objects']): string {
  const parts = objects.map((o) => `${o.typeName} (${o.count})`)
  if (parts.length <= OBJECTS_PREVIEW_COUNT) return parts.join(', ')
  const remaining = parts.length - OBJECTS_PREVIEW_COUNT
  return `${parts.slice(0, OBJECTS_PREVIEW_COUNT).join(', ')} et ${remaining} autre(s)`
}

export function ItinerairesPage() {
  const { period } = usePeriod()
  const [itineraries, setItineraries] = useState<Itinerary[]>([])
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [selected, setSelected] = useState<Itinerary | null>(null)

  const [streets, setStreets] = useState<ItineraryStreetDetail[]>([])
  const [streetsError, setStreetsError] = useState<string | null>(null)
  const [streetSortBy, setStreetSortBy] = useState<StreetSortColumn | null>(null)
  const [streetSortDir, setStreetSortDir] = useState<SortDirection>('asc')

  useEffect(() => {
    let cancelled = false
    setIsLoading(true)
    setError(null)

    fetchItineraries(period.start, period.end)
      .then((data) => {
        if (cancelled) return
        setItineraries(data)
        setSelected(data[0] ?? null)
      })
      .catch(() => {
        if (!cancelled) setError('Impossible de charger les itinéraires.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [period])

  useEffect(() => {
    if (!selected) {
      setStreets([])
      return
    }

    let cancelled = false
    setStreetsError(null)
    fetchItineraryStreets(selected.suitcaseId, selected.day, selected.itineraryIndex)
      .then((data) => {
        if (!cancelled) setStreets(data)
      })
      .catch(() => {
        if (!cancelled) setStreetsError('Impossible de charger le détail par rue.')
      })

    return () => {
      cancelled = true
    }
  }, [selected])

  const selectedLabel = selected
    ? itineraryLabel(itineraries.findIndex((i) => itineraryKey(i) === itineraryKey(selected)) + 1)
    : null

  const sortedStreets = useMemo(() => {
    if (!streetSortBy) return streets
    const sign = streetSortDir === 'asc' ? 1 : -1
    return [...streets].sort((a, b) => {
      if (streetSortBy === 'street') return a.street.localeCompare(b.street) * sign
      const aCci = a.averageCci ?? -Infinity
      const bCci = b.averageCci ?? -Infinity
      return (aCci - bCci) * sign
    })
  }, [streets, streetSortBy, streetSortDir])

  function handleStreetSort(column: StreetSortColumn) {
    if (column === streetSortBy) {
      setStreetSortDir((dir) => (dir === 'asc' ? 'desc' : 'asc'))
    } else {
      setStreetSortBy(column)
      setStreetSortDir('asc')
    }
  }

  return (
    <div className="itineraires-page">
      <div className="itineraires-row">
        <div className="itineraires-card itineraires-list-card">
          <h3>Itinéraires</h3>
          <p>{itineraries.length.toLocaleString('fr-FR')} itinéraire(s) sur la période sélectionnée.</p>

          {error && <p className="itineraires-error">{error}</p>}

          <div className="itineraires-table-wrapper">
            <table className="itineraires-table">
              <thead>
                <tr>
                  <th>Itinéraire</th>
                  <th>Date</th>
                  <th>Note (Cci)</th>
                </tr>
              </thead>
              <tbody>
                {!isLoading && itineraries.length === 0 && (
                  <tr>
                    <td colSpan={3} className="itineraires-empty">
                      Aucun itinéraire trouvé.
                    </td>
                  </tr>
                )}
                {itineraries.map((itinerary, index) => (
                  <tr
                    key={itineraryKey(itinerary)}
                    className={selected && itineraryKey(selected) === itineraryKey(itinerary) ? 'itineraires-row-selected' : ''}
                    onClick={() => setSelected(itinerary)}
                  >
                    <td>{itineraryLabel(index + 1)}</td>
                    <td>{formatDate(itinerary.day)}</td>
                    <td>{itinerary.averageCci !== null ? itinerary.averageCci.toFixed(2) : '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>

        <div className="itineraires-card itineraires-detail-card">
          <h3>Détail par rue</h3>
          {selected ? (
            <p>
              {selectedLabel} du {formatDate(selected.day)} — {streets.length.toLocaleString('fr-FR')} tronçon(s) parcouru(s).
            </p>
          ) : (
            <p>Sélectionnez un itinéraire pour voir le détail par rue.</p>
          )}

          {streetsError && <p className="itineraires-error">{streetsError}</p>}

          {selected && (
            <div className="itineraires-table-wrapper itineraires-detail-scroll">
              <table className="itineraires-table">
                <thead>
                  <tr>
                    <th>
                      <button type="button" className="itineraires-sort-button" onClick={() => handleStreetSort('street')}>
                        Rue
                        {streetSortBy === 'street' && (
                          <span className="itineraires-sort-arrow">{streetSortDir === 'asc' ? ' ▲' : ' ▼'}</span>
                        )}
                      </button>
                    </th>
                    <th>
                      <button type="button" className="itineraires-sort-button" onClick={() => handleStreetSort('averageCci')}>
                        Note (Cci)
                        {streetSortBy === 'averageCci' && (
                          <span className="itineraires-sort-arrow">{streetSortDir === 'asc' ? ' ▲' : ' ▼'}</span>
                        )}
                      </button>
                    </th>
                    <th>Objets détectés</th>
                  </tr>
                </thead>
                <tbody>
                  {sortedStreets.length === 0 && (
                    <tr>
                      <td colSpan={3} className="itineraires-empty">
                        Aucun objet détecté.
                      </td>
                    </tr>
                  )}
                  {sortedStreets.map((street, index) => (
                    <tr key={`${street.street}-${index}`}>
                      <td>{street.street}</td>
                      <td>{street.averageCci !== null ? street.averageCci.toFixed(2) : '—'}</td>
                      <td className="itineraires-streets" title={street.objects.map((o) => `${o.typeName} (${o.count})`).join(', ')}>
                        {formatObjects(street.objects)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
