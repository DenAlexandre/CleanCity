import { useEffect, useMemo, useState, type FormEvent } from 'react'
import type { AdminCredentials } from '../api/authApi'
import { AdminActionError } from '../api/authApi'
import {
  createPointOfInterest,
  deletePointOfInterest,
  fetchPointOfInterestObjects,
  fetchPointOfInterestScoresList,
  listPointsOfInterest,
  updatePointOfInterest,
  type PointOfInterest,
  type PointOfInterestObjectBreakdown,
  type PointOfInterestScore,
  type SavePointOfInterestInput,
} from '../api/pointsOfInterestApi'
import { fetchPointOfInterestSettings, updatePointOfInterestSettings } from '../api/settingsApi'
import { usePeriod } from '../period/PeriodContext'
import { Modal } from '../components/Modal'
import './PointsInteretPage.css'

const EMPTY_FORM: SavePointOfInterestInput = { name: '', description: '', category: '', latitude: 0, longitude: 0 }

const SUGGESTED_CATEGORIES = ['Gares', 'Écoles', 'Parcs et squares', 'Mairies', 'Commerces']

function formatScore(value: number | null | undefined): string {
  return value !== null && value !== undefined ? value.toFixed(2).replace('.', ',') : '—'
}

export function PointsInteretPage({ adminCredentials }: { adminCredentials: AdminCredentials | null }) {
  const { period } = usePeriod()
  const [points, setPoints] = useState<PointOfInterest[]>([])
  const [scores, setScores] = useState<PointOfInterestScore[]>([])
  const [loadError, setLoadError] = useState<string | null>(null)
  const [selectedId, setSelectedId] = useState<string | null>(null)
  const [objects, setObjects] = useState<PointOfInterestObjectBreakdown[]>([])
  const [detailError, setDetailError] = useState<string | null>(null)
  const [showAddModal, setShowAddModal] = useState(false)

  async function refresh() {
    try {
      setPoints(await listPointsOfInterest())
      setLoadError(null)
    } catch {
      setLoadError("Impossible de charger les points d'intérêt.")
    }
  }

  useEffect(() => {
    refresh()
  }, [])

  useEffect(() => {
    let cancelled = false
    fetchPointOfInterestScoresList(period.start, period.end)
      .then((data) => {
        if (!cancelled) setScores(data)
      })
      .catch(() => {
        if (!cancelled) setScores([])
      })

    return () => {
      cancelled = true
    }
  }, [period])

  useEffect(() => {
    if (!selectedId) {
      setObjects([])
      return
    }

    let cancelled = false
    setDetailError(null)
    fetchPointOfInterestObjects(selectedId, period.start, period.end)
      .then((data) => {
        if (!cancelled) setObjects(data)
      })
      .catch(() => {
        if (!cancelled) {
          setObjects([])
          setDetailError('Impossible de charger le détail des objets détectés.')
        }
      })

    return () => {
      cancelled = true
    }
  }, [selectedId, period])

  const scoreById = useMemo(() => new Map(scores.map((s) => [s.id, s.averageCci])), [scores])

  // Une catégorie regroupe plusieurs points d'intérêt : sa note est la moyenne des notes
  // individuelles de ses points (ceux ayant une note, les autres étant ignorés du calcul).
  const categories = useMemo(() => {
    const byCategory = new Map<string, { total: number; count: number; poiCount: number }>()
    for (const score of scores) {
      const entry = byCategory.get(score.category) ?? { total: 0, count: 0, poiCount: 0 }
      entry.poiCount += 1
      if (score.averageCci !== null) {
        entry.total += score.averageCci
        entry.count += 1
      }
      byCategory.set(score.category, entry)
    }

    return [...byCategory.entries()]
      .map(([category, { total, count, poiCount }]) => ({
        category,
        averageCci: count > 0 ? total / count : null,
        poiCount,
      }))
      .sort((a, b) => a.category.localeCompare(b.category))
  }, [scores])

  const selectedPoint = points.find((p) => p.id === selectedId) ?? null

  return (
    <>
      <PointOfInterestRadiusCard adminCredentials={adminCredentials} />

      <div className="poi-card">
        <h3>Catégories</h3>
        {categories.length === 0 ? (
          <p className="poi-empty">Aucune catégorie pour le moment.</p>
        ) : (
          <ul className="poi-category-list">
            {categories.map((c) => (
              <li key={c.category}>
                <span>
                  {c.category} <span className="poi-category-count">({c.poiCount})</span>
                </span>
                <span className="poi-category-score">{formatScore(c.averageCci)} /5</span>
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="poi-row">
        <div className="poi-card poi-list-card">
          <div className="poi-list-header">
            <h3>Points d'intérêt</h3>
            {adminCredentials && (
              <button type="button" onClick={() => setShowAddModal(true)}>
                Ajouter
              </button>
            )}
          </div>
          {loadError && <p className="poi-error">{loadError}</p>}
          <PointsTable
            points={points}
            scoreById={scoreById}
            admin={adminCredentials}
            onChanged={refresh}
            selectedId={selectedId}
            onSelect={setSelectedId}
          />
        </div>

        <div className="poi-card poi-detail-card">
          <h3>Détail</h3>
          {!selectedPoint ? (
            <p className="poi-empty">Sélectionnez un point d'intérêt pour voir le détail.</p>
          ) : (
            <>
              <p className="poi-detail-summary">
                <strong>{selectedPoint.name}</strong> — {selectedPoint.category} — Note :{' '}
                {formatScore(scoreById.get(selectedPoint.id))} /5
              </p>
              {detailError && <p className="poi-error">{detailError}</p>}
              <div className="poi-table-wrapper">
                <table className="poi-table">
                  <thead>
                    <tr>
                      <th>Type d'objet</th>
                      <th>Quantité</th>
                    </tr>
                  </thead>
                  <tbody>
                    {objects.length === 0 && (
                      <tr>
                        <td colSpan={2} className="poi-empty">
                          Aucun objet détecté à proximité sur la période.
                        </td>
                      </tr>
                    )}
                    {objects.map((o) => (
                      <tr key={o.typeCode}>
                        <td>{o.typeName}</td>
                        <td>{o.count}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </div>
      </div>

      {showAddModal && adminCredentials && (
        <Modal title="Ajouter un point d'intérêt" onClose={() => setShowAddModal(false)}>
          <PointForm
            admin={adminCredentials}
            onSaved={() => {
              refresh()
              setShowAddModal(false)
            }}
          />
        </Modal>
      )}
    </>
  )
}

function PointOfInterestRadiusCard({ adminCredentials }: { adminCredentials: AdminCredentials | null }) {
  const [radiusMeters, setRadiusMeters] = useState<number | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    fetchPointOfInterestSettings()
      .then((settings) => setRadiusMeters(settings.radiusMeters))
      .catch(() => setLoadError('Impossible de charger le rayon de calcul.'))
  }, [])

  async function handleSave() {
    if (!adminCredentials || radiusMeters === null) return
    setSaveError(null)
    setSuccess(null)
    setIsSaving(true)
    try {
      const saved = await updatePointOfInterestSettings(adminCredentials, { radiusMeters })
      setRadiusMeters(saved.radiusMeters)
      setSuccess('Rayon enregistré.')
    } catch (err) {
      setSaveError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    } finally {
      setIsSaving(false)
    }
  }

  if (loadError) return <p className="poi-error">{loadError}</p>
  if (radiusMeters === null) return null

  const readOnly = !adminCredentials

  return (
    <div className="poi-card">
      <h3>Rayon de calcul</h3>
      <p>
        Distance (en mètres) autour d'un point d'intérêt prise en compte pour calculer sa note et le détail des
        objets détectés à proximité.
      </p>
      <label className="poi-radius-field">
        <span>Rayon (mètres)</span>
        <input
          type="number"
          min={1}
          value={radiusMeters}
          disabled={readOnly}
          onChange={(e) => setRadiusMeters(Number(e.target.value))}
        />
      </label>

      {saveError && <p className="poi-error">{saveError}</p>}
      {success && <p className="poi-success">{success}</p>}

      {!readOnly && (
        <button type="button" onClick={handleSave} disabled={isSaving}>
          {isSaving ? 'Enregistrement…' : 'Enregistrer'}
        </button>
      )}
    </div>
  )
}

function PointForm({ admin, onSaved }: { admin: AdminCredentials; onSaved: () => void }) {
  const [form, setForm] = useState<SavePointOfInterestInput>({ ...EMPTY_FORM })
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setSuccess(null)
    setIsSubmitting(true)

    try {
      await createPointOfInterest(admin, form)
      setSuccess(`Point d'intérêt "${form.name}" créé.`)
      setForm({ ...EMPTY_FORM })
      onSaved()
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form className="poi-card" onSubmit={handleSubmit}>
      <div className="poi-form-grid">
        <label>
          <span>Nom</span>
          <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
        </label>
        <label className="poi-form-description">
          <span>Description</span>
          <input value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} />
        </label>
        <label>
          <span>Catégorie</span>
          <input
            list="poi-category-suggestions"
            value={form.category}
            onChange={(e) => setForm({ ...form, category: e.target.value })}
            required
          />
        </label>
        <label>
          <span>Latitude</span>
          <input
            type="number"
            step="any"
            value={form.latitude}
            onChange={(e) => setForm({ ...form, latitude: Number(e.target.value) })}
            required
          />
        </label>
        <label>
          <span>Longitude</span>
          <input
            type="number"
            step="any"
            value={form.longitude}
            onChange={(e) => setForm({ ...form, longitude: Number(e.target.value) })}
            required
          />
        </label>
      </div>

      <datalist id="poi-category-suggestions">
        {SUGGESTED_CATEGORIES.map((category) => (
          <option key={category} value={category} />
        ))}
      </datalist>

      {error && <p className="poi-error">{error}</p>}
      {success && <p className="poi-success">{success}</p>}

      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Création…' : 'Ajouter'}
      </button>
    </form>
  )
}

function PointsTable({
  points,
  scoreById,
  admin,
  onChanged,
  selectedId,
  onSelect,
}: {
  points: PointOfInterest[]
  scoreById: Map<string, number | null>
  admin: AdminCredentials | null
  onChanged: () => void
  selectedId: string | null
  onSelect: (id: string) => void
}) {
  const [editingId, setEditingId] = useState<string | null>(null)
  const [editForm, setEditForm] = useState<SavePointOfInterestInput>({ ...EMPTY_FORM })
  const [error, setError] = useState<string | null>(null)

  function startEdit(point: PointOfInterest) {
    setEditingId(point.id)
    setEditForm({
      name: point.name,
      description: point.description ?? '',
      category: point.category,
      latitude: point.latitude,
      longitude: point.longitude,
    })
  }

  async function handleSaveEdit(id: string) {
    if (!admin) return
    setError(null)
    try {
      await updatePointOfInterest(admin, id, editForm)
      setEditingId(null)
      onChanged()
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    }
  }

  async function handleDelete(point: PointOfInterest) {
    if (!admin) return
    if (!window.confirm(`Supprimer le point d'intérêt "${point.name}" ?`)) return
    setError(null)
    try {
      await deletePointOfInterest(admin, point.id)
      onChanged()
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    }
  }

  return (
    <>
      {error && <p className="poi-error">{error}</p>}
      <div className="poi-table-wrapper">
        <table className="poi-table">
          <thead>
            <tr>
              <th>Nom</th>
              <th>Description</th>
              <th>Catégorie</th>
              <th>Note</th>
              <th>Latitude</th>
              <th>Longitude</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {points.length === 0 && (
              <tr>
                <td colSpan={7} className="poi-empty">
                  Aucun point d'intérêt pour le moment.
                </td>
              </tr>
            )}
            {points.map((point) =>
              editingId === point.id ? (
                <tr key={point.id}>
                  <td>
                    <input value={editForm.name} onChange={(e) => setEditForm({ ...editForm, name: e.target.value })} />
                  </td>
                  <td>
                    <input
                      value={editForm.description}
                      onChange={(e) => setEditForm({ ...editForm, description: e.target.value })}
                    />
                  </td>
                  <td>
                    <input
                      list="poi-category-suggestions"
                      value={editForm.category}
                      onChange={(e) => setEditForm({ ...editForm, category: e.target.value })}
                    />
                  </td>
                  <td>{formatScore(scoreById.get(point.id))} /5</td>
                  <td>
                    <input
                      type="number"
                      step="any"
                      value={editForm.latitude}
                      onChange={(e) => setEditForm({ ...editForm, latitude: Number(e.target.value) })}
                    />
                  </td>
                  <td>
                    <input
                      type="number"
                      step="any"
                      value={editForm.longitude}
                      onChange={(e) => setEditForm({ ...editForm, longitude: Number(e.target.value) })}
                    />
                  </td>
                  <td className="poi-table-actions">
                    <button type="button" onClick={() => handleSaveEdit(point.id)}>
                      Enregistrer
                    </button>
                    <button type="button" onClick={() => setEditingId(null)}>
                      Annuler
                    </button>
                  </td>
                </tr>
              ) : (
                <tr
                  key={point.id}
                  className={`poi-row-clickable ${selectedId === point.id ? 'poi-row-selected' : ''}`}
                  onClick={() => onSelect(point.id)}
                >
                  <td>{point.name}</td>
                  <td>{point.description}</td>
                  <td>{point.category}</td>
                  <td>{formatScore(scoreById.get(point.id))} /5</td>
                  <td>{point.latitude}</td>
                  <td>{point.longitude}</td>
                  <td className="poi-table-actions" onClick={(e) => e.stopPropagation()}>
                    {admin && (
                      <>
                        <button type="button" onClick={() => startEdit(point)}>
                          Modifier
                        </button>
                        <button type="button" className="poi-danger" onClick={() => handleDelete(point)}>
                          Supprimer
                        </button>
                      </>
                    )}
                  </td>
                </tr>
              ),
            )}
          </tbody>
        </table>
      </div>
    </>
  )
}
