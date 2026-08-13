import { useEffect, useMemo, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import type { Measurement, MeasurementSortColumn, MeasurementTypeBreakdown, SortDirection } from '../api/measurementsApi'
import { fetchMeasurementTypeBreakdown } from '../api/measurementsApi'
import {
  fetchPointOfInterestMeasurements,
  fetchPointOfInterestMeasurementTypeBreakdown,
  listPointsOfInterest,
  type PointOfInterest,
} from '../api/pointsOfInterestApi'
import { usePeriod, type Period } from '../period/PeriodContext'
import { PieChart } from '../components/PieChart'
import './PointsInteretMeasuresPage.css'

const PAGE_SIZE = 50

const COLUMNS: { key: MeasurementSortColumn; label: string }[] = [
  { key: 'type', label: "Type d'objet" },
  { key: 'quantity', label: 'Quantité' },
  { key: 'measuredAt', label: 'Date de détection' },
  { key: 'street', label: 'Rue' },
]

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('fr-FR', { dateStyle: 'medium', timeStyle: 'medium' })
}

export function PointsInteretMeasuresPage() {
  const { period } = usePeriod()
  const [searchParams] = useSearchParams()
  const [page, setPage] = useState(1)
  const [sortBy, setSortBy] = useState<MeasurementSortColumn>('measuredAt')
  const [sortDir, setSortDir] = useState<SortDirection>('desc')
  const [items, setItems] = useState<Measurement[]>([])
  const [total, setTotal] = useState(0)
  const [totalObjects, setTotalObjects] = useState(0)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [breakdown, setBreakdown] = useState<MeasurementTypeBreakdown[]>([])
  const [points, setPoints] = useState<PointOfInterest[]>([])

  const [typeFilter, setTypeFilter] = useState<number | ''>('')
  // Pré-rempli si on arrive depuis le clic sur une catégorie de la page Accueil (?category=...) ;
  // ignoré après le premier rendu si l'utilisateur change le filtre lui-même.
  const [categoryFilter, setCategoryFilter] = useState(searchParams.get('category') ?? '')
  const [poiFilter, setPoiFilter] = useState('')

  useEffect(() => {
    listPointsOfInterest()
      .then(setPoints)
      .catch(() => setPoints([]))
  }, [])

  useEffect(() => {
    let cancelled = false
    fetchMeasurementTypeBreakdown(period.start, period.end)
      .then((data) => {
        if (!cancelled) setBreakdown(data)
      })
      .catch(() => {
        if (!cancelled) setBreakdown([])
      })

    return () => {
      cancelled = true
    }
  }, [period])

  useEffect(() => {
    setPage(1)
  }, [period, typeFilter, categoryFilter, poiFilter])

  useEffect(() => {
    let cancelled = false
    setIsLoading(true)
    setError(null)

    fetchPointOfInterestMeasurements(page, PAGE_SIZE, sortBy, sortDir, {
      startDate: period.start,
      endDate: period.end,
      typeCode: typeFilter === '' ? undefined : typeFilter,
      category: categoryFilter || undefined,
      poiId: poiFilter || undefined,
    })
      .then((data) => {
        if (cancelled) return
        setItems(data.items)
        setTotal(data.total)
        setTotalObjects(data.totalObjects)
      })
      .catch(() => {
        if (!cancelled) setError("Impossible de charger les objets détectés à proximité des points d'intérêt.")
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [page, sortBy, sortDir, period, typeFilter, categoryFilter, poiFilter])

  function handleSort(column: MeasurementSortColumn) {
    if (column === sortBy) {
      setSortDir(sortDir === 'asc' ? 'desc' : 'asc')
    } else {
      setSortBy(column)
      setSortDir('desc')
    }
    setPage(1)
  }

  const categories = useMemo(
    () => [...new Set(points.map((p) => p.category))].sort((a, b) => a.localeCompare(b)),
    [points],
  )

  // La liste des points d'intérêt proposée se limite à la catégorie choisie (sinon tous).
  const poisForCategory = useMemo(
    () => points.filter((p) => !categoryFilter || p.category === categoryFilter).sort((a, b) => a.name.localeCompare(b.name)),
    [points, categoryFilter],
  )

  function handleCategoryChange(category: string) {
    setCategoryFilter(category)
    setPoiFilter('')
  }

  const pageCount = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <div className="poi-measures-page">
      <div className="poi-measures-row">
        <div className="poi-measures-card poi-measures-table-card">
          <h3>Objets détectés à proximité des points d'intérêt</h3>
          <p>{totalObjects.toLocaleString('fr-FR')} objet(s) détecté(s) ({total.toLocaleString('fr-FR')} ligne(s)).</p>

          <div className="poi-measures-filters">
            <label>
              <span>Catégorie</span>
              <select value={categoryFilter} onChange={(e) => handleCategoryChange(e.target.value)}>
                <option value="">Toutes les catégories</option>
                {categories.map((category) => (
                  <option key={category} value={category}>
                    {category}
                  </option>
                ))}
              </select>
            </label>
            <label>
              <span>Point d'intérêt</span>
              <select value={poiFilter} onChange={(e) => setPoiFilter(e.target.value)}>
                <option value="">Tous les points d'intérêt</option>
                {poisForCategory.map((point) => (
                  <option key={point.id} value={point.id}>
                    {point.name}
                  </option>
                ))}
              </select>
            </label>
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
          </div>

          {error && <p className="poi-measures-error">{error}</p>}

          <div className="poi-measures-table-wrapper">
            <table className="poi-measures-table">
              <thead>
                <tr>
                  {COLUMNS.map((column) => (
                    <th key={column.key}>
                      <button type="button" className="poi-measures-sort-button" onClick={() => handleSort(column.key)}>
                        {column.label}
                        {sortBy === column.key && (
                          <span className="poi-measures-sort-arrow">{sortDir === 'asc' ? ' ▲' : ' ▼'}</span>
                        )}
                      </button>
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {!isLoading && items.length === 0 && (
                  <tr>
                    <td colSpan={COLUMNS.length} className="poi-measures-empty">
                      Aucun objet détecté à proximité sur la période.
                    </td>
                  </tr>
                )}
                {items.map((item, index) => (
                  <tr key={`${item.snapshotId}-${item.typeCode}-${index}`}>
                    <td>{item.typeName}</td>
                    <td>{item.quantity}</td>
                    <td>{formatDate(item.measuredAt)}</td>
                    <td>{item.street ?? '—'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="poi-measures-pagination">
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

        <PoiMeasuresBreakdownChart period={period} category={categoryFilter} poiId={poiFilter} />
      </div>
    </div>
  )
}

function PoiMeasuresBreakdownChart({ period, category, poiId }: { period: Period; category: string; poiId: string }) {
  const [breakdown, setBreakdown] = useState<MeasurementTypeBreakdown[]>([])

  useEffect(() => {
    let cancelled = false
    fetchPointOfInterestMeasurementTypeBreakdown({
      startDate: period.start,
      endDate: period.end,
      category: category || undefined,
      poiId: poiId || undefined,
    })
      .then((data) => {
        if (!cancelled) setBreakdown(data)
      })
      .catch(() => {
        if (!cancelled) setBreakdown([])
      })

    return () => {
      cancelled = true
    }
  }, [period, category, poiId])

  return (
    <div className="poi-measures-card poi-measures-chart-card">
      <h3>Répartition par type</h3>
      <PieChart slices={breakdown.map((b) => ({ label: b.typeName, value: b.count }))} />
    </div>
  )
}
