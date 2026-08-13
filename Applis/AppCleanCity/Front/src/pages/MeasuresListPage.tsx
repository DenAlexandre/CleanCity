import { useEffect, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  fetchMeasurements,
  fetchMeasurementStreets,
  fetchMeasurementTypeBreakdown,
  type Measurement,
  type MeasurementSortColumn,
  type MeasurementTypeBreakdown,
  type SortDirection,
} from '../api/measurementsApi'
import { usePeriod } from '../period/PeriodContext'
import { PieChart } from '../components/PieChart'
import { ItinerairesPage } from './ItinerairesPage'
import { PointsInteretMeasuresPage } from './PointsInteretMeasuresPage'
import './MeasuresListPage.css'

type Tab = 'objects' | 'itineraries' | 'poi'

const VALID_TABS: Tab[] = ['objects', 'itineraries', 'poi']

const PAGE_SIZE = 50

const COLUMNS: { key: MeasurementSortColumn; label: string }[] = [
  { key: 'type', label: "Type d'objet" },
  { key: 'quantity', label: 'Quantité' },
  { key: 'measuredAt', label: 'Date de détection' },
  { key: 'street', label: 'Rue' },
  { key: 'latitude', label: 'Latitude' },
  { key: 'longitude', label: 'Longitude' },
]

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('fr-FR', { dateStyle: 'medium', timeStyle: 'medium' })
}

export function MeasuresListPage() {
  const { period } = usePeriod()
  const [searchParams] = useSearchParams()
  // Permet à d'autres pages (ex: clic sur une catégorie de la page Accueil) de lier directement vers
  // un onglet précis via ?tab=... ; ignoré après le premier rendu si l'utilisateur change d'onglet.
  const initialTab = searchParams.get('tab')
  const [tab, setTab] = useState<Tab>(
    VALID_TABS.includes(initialTab as Tab) ? (initialTab as Tab) : 'objects',
  )
  const [page, setPage] = useState(1)
  const [sortBy, setSortBy] = useState<MeasurementSortColumn>('measuredAt')
  const [sortDir, setSortDir] = useState<SortDirection>('desc')
  const [items, setItems] = useState<Measurement[]>([])
  const [total, setTotal] = useState(0)
  const [totalObjects, setTotalObjects] = useState(0)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [breakdown, setBreakdown] = useState<MeasurementTypeBreakdown[]>([])
  const [streets, setStreets] = useState<string[]>([])

  const [typeFilter, setTypeFilter] = useState<number | ''>('')
  const [streetFilter, setStreetFilter] = useState('')

  useEffect(() => {
    let cancelled = false
    fetchMeasurementStreets(period.start, period.end, typeFilter === '' ? undefined : typeFilter)
      .then((data) => {
        if (!cancelled) setStreets(data)
      })
      .catch(() => {
        if (!cancelled) setStreets([])
      })

    return () => {
      cancelled = true
    }
  }, [period, typeFilter])

  useEffect(() => {
    setPage(1)
  }, [period, typeFilter, streetFilter])

  useEffect(() => {
    let cancelled = false
    setIsLoading(true)
    setError(null)

    fetchMeasurements(page, PAGE_SIZE, sortBy, sortDir, {
      startDate: period.start,
      endDate: period.end,
      typeCode: typeFilter === '' ? undefined : typeFilter,
      street: streetFilter || undefined,
    })
      .then((data) => {
        if (cancelled) return
        setItems(data.items)
        setTotal(data.total)
        setTotalObjects(data.totalObjects)
      })
      .catch(() => {
        if (!cancelled) setError('Impossible de charger la liste des mesures.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [page, sortBy, sortDir, period, typeFilter, streetFilter])

  useEffect(() => {
    let cancelled = false
    fetchMeasurementTypeBreakdown(period.start, period.end, streetFilter || undefined)
      .then((data) => {
        if (!cancelled) setBreakdown(data)
      })
      .catch(() => {
        if (!cancelled) setBreakdown([])
      })

    return () => {
      cancelled = true
    }
  }, [period, streetFilter])

  function handleSort(column: MeasurementSortColumn) {
    if (column === sortBy) {
      setSortDir(sortDir === 'asc' ? 'desc' : 'asc')
    } else {
      setSortBy(column)
      setSortDir('desc')
    }
    setPage(1)
  }

  const pageCount = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <div className="measures-list-page">
      <div className="measures-list-tabs">
        <button className={tab === 'objects' ? 'active' : ''} onClick={() => setTab('objects')}>
          Liste des objets
        </button>
        <button className={tab === 'itineraries' ? 'active' : ''} onClick={() => setTab('itineraries')}>
          Itinéraires
        </button>
        <button className={tab === 'poi' ? 'active' : ''} onClick={() => setTab('poi')}>
          Points d'intérêts
        </button>
      </div>

      <div className="measures-list-tab-content">
      {tab === 'itineraries' && <ItinerairesPage />}
      {tab === 'poi' && <PointsInteretMeasuresPage />}

      {tab === 'objects' && (
      <div className="measures-list-row">
        <div className="measures-list-card measures-list-table-card">
          <h3>Liste des mesures</h3>
          <p>{totalObjects.toLocaleString('fr-FR')} objet(s) détecté(s) ({total.toLocaleString('fr-FR')} ligne(s)).</p>

          <div className="measures-list-filters">
            <label>
              <span>Type d'objet</span>
              <select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value === '' ? '' : Number(e.target.value))}>
                <option value="">Tous les types</option>
                {breakdown.map((b) => (
                  <option key={b.typeCode} value={b.typeCode}>
                    {b.typeName}
                  </option>
                ))}
              </select>
            </label>
            <label>
              <span>Rue</span>
              <select value={streetFilter} onChange={(e) => setStreetFilter(e.target.value)}>
                <option value="">Toutes les rues</option>
                {streets.map((street) => (
                  <option key={street} value={street}>
                    {street}
                  </option>
                ))}
              </select>
            </label>
          </div>

          {error && <p className="measures-list-error">{error}</p>}

          <div className="measures-list-table-wrapper">
            <table className="measures-list-table">
              <thead>
                <tr>
                  {COLUMNS.map((column) => (
                    <th key={column.key}>
                      <button type="button" className="measures-list-sort-button" onClick={() => handleSort(column.key)}>
                        {column.label}
                        {sortBy === column.key && (
                          <span className="measures-list-sort-arrow">{sortDir === 'asc' ? ' ▲' : ' ▼'}</span>
                        )}
                      </button>
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {!isLoading && items.length === 0 && (
                  <tr>
                    <td colSpan={COLUMNS.length} className="measures-list-empty">
                      Aucune mesure trouvée.
                    </td>
                  </tr>
                )}
                {items.map((item, index) => (
                  <tr key={`${item.snapshotId}-${item.typeCode}-${index}`}>
                    <td>{item.typeName}</td>
                    <td>{item.quantity}</td>
                    <td>{formatDate(item.measuredAt)}</td>
                    <td>{item.street ?? '—'}</td>
                    <td>{item.latitude.toFixed(6)}</td>
                    <td>{item.longitude.toFixed(6)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="measures-list-pagination">
            <button type="button" onClick={() => setPage((p) => Math.max(1, p - 1))} disabled={page <= 1 || isLoading}>
              Précédent
            </button>
            <span>
              Page {page} / {pageCount}
            </span>
            <button
              type="button"
              onClick={() => setPage((p) => Math.min(pageCount, p + 1))}
              disabled={page >= pageCount || isLoading}
            >
              Suivant
            </button>
          </div>
        </div>

        <div className="measures-list-card measures-list-chart-card">
          <h3>Répartition par type</h3>
          <PieChart slices={breakdown.map((b) => ({ label: b.typeName, value: b.count }))} />
        </div>
      </div>
      )}
      </div>
    </div>
  )
}
