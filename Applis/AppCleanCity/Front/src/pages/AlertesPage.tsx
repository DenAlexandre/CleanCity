import { useEffect, useState } from 'react'
import { fetchAlarms, type Alarm } from '../api/alarmsApi'
import { usePeriod } from '../period/PeriodContext'
import './AlertesPage.css'

const PAGE_SIZE = 50

function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('fr-FR', { dateStyle: 'medium', timeStyle: 'medium' })
}

export function AlertesPage() {
  const { period } = usePeriod()
  const [page, setPage] = useState(1)
  const [items, setItems] = useState<Alarm[]>([])
  const [total, setTotal] = useState(0)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    setPage(1)
  }, [period])

  useEffect(() => {
    let cancelled = false
    setIsLoading(true)
    setError(null)

    fetchAlarms(page, PAGE_SIZE, period.start, period.end)
      .then((data) => {
        if (cancelled) return
        setItems(data.items)
        setTotal(data.total)
      })
      .catch(() => {
        if (!cancelled) setError('Impossible de charger les alarmes.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [page, period])

  const pageCount = Math.max(1, Math.ceil(total / PAGE_SIZE))

  return (
    <div className="alertes-page">
      <div className="alertes-card">
        <h3>Alarmes</h3>
        <p>{total.toLocaleString('fr-FR')} alarme(s) sur la période sélectionnée.</p>

        {error && <p className="alertes-error">{error}</p>}

        <div className="alertes-table-wrapper">
          <table className="alertes-table">
            <thead>
              <tr>
                <th>Date de détection</th>
                <th>Rue</th>
                <th>Type d'objet</th>
                <th>Quantité</th>
                <th>Seuil</th>
                <th>E-mail envoyé</th>
              </tr>
            </thead>
            <tbody>
              {!isLoading && items.length === 0 && (
                <tr>
                  <td colSpan={6} className="alertes-empty">
                    Aucune alarme sur la période.
                  </td>
                </tr>
              )}
              {items.map((alarm) => (
                <tr key={alarm.id}>
                  <td>{formatDate(alarm.measuredAt)}</td>
                  <td>{alarm.street ?? '—'}</td>
                  <td>{alarm.typeName}</td>
                  <td>{alarm.count}</td>
                  <td>{alarm.threshold}</td>
                  <td>{alarm.emailSent ? 'Oui' : 'Non'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <div className="alertes-pagination">
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
    </div>
  )
}
