import { useEffect, useState, type FormEvent } from 'react'
import { useAuth } from '../auth/AuthContext'
import { AdminActionError, type AdminCredentials } from '../api/authApi'
import {
  fetchDetectionDisplaySettings,
  fetchWeatherSettings,
  updateDetectionDisplaySettings,
  updateWeatherSettings,
  type DetectionDisplaySettings,
  type WeatherSettings,
} from '../api/settingsApi'
import { searchCities, type CitySearchResult } from '../api/weatherApi'
import {
  createAlarmThreshold,
  deleteAlarmThreshold,
  listAlarmThresholds,
  listDetectionTypes,
  updateAlarmThreshold,
  type AlarmThreshold,
  type DetectionType,
  type SaveAlarmThresholdInput,
} from '../api/alarmThresholdsApi'
import {
  createAlarmEmailRecipient,
  deleteAlarmEmailRecipient,
  listAlarmEmailRecipients,
  type AlarmEmailRecipient,
} from '../api/alarmEmailRecipientsApi'
import { PointsInteretPage } from './PointsInteretPage'
import '../pages/AdminPage.css'
import './ParametresPage.css'

const CITY_SEARCH_DEBOUNCE_MS = 400

type Tab = 'general' | 'cartographie' | 'alarms' | 'recipients' | 'poi'

export function ParametresPage() {
  const { adminCredentials } = useAuth()
  const [tab, setTab] = useState<Tab>('general')

  return (
    <div className="parametres-page">
      <div className="parametres-tabs">
        <button className={tab === 'general' ? 'active' : ''} onClick={() => setTab('general')}>
          Général
        </button>
        <button className={tab === 'cartographie' ? 'active' : ''} onClick={() => setTab('cartographie')}>
          Cartographie
        </button>
        <button className={tab === 'alarms' ? 'active' : ''} onClick={() => setTab('alarms')}>
          Seuils des alarmes
        </button>
        <button className={tab === 'recipients' ? 'active' : ''} onClick={() => setTab('recipients')}>
          Destinataires des alertes
        </button>
        <button className={tab === 'poi' ? 'active' : ''} onClick={() => setTab('poi')}>
          Points d'intérêt
        </button>
      </div>

      <div className="parametres-tab-content">
        {tab === 'general' && <WeatherCityCard adminCredentials={adminCredentials} />}
        {tab === 'cartographie' && <DetectionThresholdsCard adminCredentials={adminCredentials} />}
        {tab === 'alarms' && <AlarmThresholdsManager adminCredentials={adminCredentials} />}
        {tab === 'recipients' && <AlarmEmailRecipientsManager adminCredentials={adminCredentials} />}
        {tab === 'poi' && <PointsInteretPage adminCredentials={adminCredentials} />}
      </div>
    </div>
  )
}

function DetectionThresholdsCard({ adminCredentials }: { adminCredentials: AdminCredentials | null }) {
  const [settings, setSettings] = useState<DetectionDisplaySettings | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  useEffect(() => {
    fetchDetectionDisplaySettings()
      .then(setSettings)
      .catch(() => setLoadError('Impossible de charger les paramètres.'))
  }, [])

  async function handleSave() {
    if (!adminCredentials || !settings) return
    setSaveError(null)
    setSuccess(null)
    setIsSaving(true)
    try {
      const saved = await updateDetectionDisplaySettings(adminCredentials, settings)
      setSettings(saved)
      setSuccess('Paramètres enregistrés.')
    } catch (err) {
      setSaveError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    } finally {
      setIsSaving(false)
    }
  }

  if (loadError) return <p className="parametres-error">{loadError}</p>
  if (!settings) return null

  const readOnly = !adminCredentials

  return (
    <div className="parametres-card">
      <h3>Seuils de détection sur la carte</h3>
      <p>
        Définit les seuils de note (Cci) et les couleurs utilisés par les cases "Détection positive" et "Détection
        moyenne" de la page Mesures.
      </p>

      <div className="parametres-threshold">
        <h4>Détection positive</h4>
        <div className="parametres-fields">
          <label>
            <span>Note minimum</span>
            <input
              type="number"
              step="0.1"
              value={settings.positiveMin}
              disabled={readOnly}
              onChange={(e) => setSettings({ ...settings, positiveMin: Number(e.target.value) })}
            />
          </label>
          <label>
            <span>Note maximum</span>
            <input
              type="number"
              step="0.1"
              value={settings.positiveMax}
              disabled={readOnly}
              onChange={(e) => setSettings({ ...settings, positiveMax: Number(e.target.value) })}
            />
          </label>
          <label>
            <span>Couleur</span>
            <input
              type="color"
              value={settings.positiveColor}
              disabled={readOnly}
              onChange={(e) => setSettings({ ...settings, positiveColor: e.target.value })}
            />
          </label>
        </div>
      </div>

      <div className="parametres-threshold">
        <h4>Détection moyenne</h4>
        <div className="parametres-fields">
          <label>
            <span>Note minimum</span>
            <input
              type="number"
              step="0.1"
              value={settings.averageMin}
              disabled={readOnly}
              onChange={(e) => setSettings({ ...settings, averageMin: Number(e.target.value) })}
            />
          </label>
          <label>
            <span>Note maximum</span>
            <input
              type="number"
              step="0.1"
              value={settings.averageMax}
              disabled={readOnly}
              onChange={(e) => setSettings({ ...settings, averageMax: Number(e.target.value) })}
            />
          </label>
          <label>
            <span>Couleur</span>
            <input
              type="color"
              value={settings.averageColor}
              disabled={readOnly}
              onChange={(e) => setSettings({ ...settings, averageColor: e.target.value })}
            />
          </label>
        </div>
      </div>

      <div className="parametres-threshold">
        <h4>Onglet Détails de la carte</h4>
        <label className="parametres-checkbox">
          <input
            type="checkbox"
            checked={settings.hideObjectsWithoutStreet}
            disabled={readOnly}
            onChange={(e) => setSettings({ ...settings, hideObjectsWithoutStreet: e.target.checked })}
          />
          Cacher les objets détectés sans rue associée
        </label>
      </div>

      {saveError && <p className="parametres-error">{saveError}</p>}
      {success && <p className="parametres-success">{success}</p>}

      {!readOnly && (
        <button type="button" onClick={handleSave} disabled={isSaving}>
          {isSaving ? 'Enregistrement…' : 'Enregistrer'}
        </button>
      )}
    </div>
  )
}
function WeatherCityCard({ adminCredentials }: { adminCredentials: AdminCredentials | null }) {
  const [city, setCity] = useState<WeatherSettings | null>(null)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)

  const [query, setQuery] = useState('')
  const [results, setResults] = useState<CitySearchResult[]>([])
  const [searchError, setSearchError] = useState<string | null>(null)

  useEffect(() => {
    fetchWeatherSettings()
      .then(setCity)
      .catch(() => setLoadError('Impossible de charger la ville météo.'))
  }, [])

  useEffect(() => {
    if (query.trim().length < 2) {
      setResults([])
      return
    }

    const timeout = setTimeout(() => {
      searchCities(query)
        .then(setResults)
        .catch(() => setSearchError('Recherche de ville impossible.'))
    }, CITY_SEARCH_DEBOUNCE_MS)

    return () => clearTimeout(timeout)
  }, [query])

  function selectCity(result: CitySearchResult) {
    setCity({ city: result.name, latitude: result.latitude, longitude: result.longitude })
    setQuery('')
    setResults([])
    setSuccess(null)
  }

  async function handleSave() {
    if (!adminCredentials || !city) return
    setSaveError(null)
    setSuccess(null)
    setIsSaving(true)
    try {
      const saved = await updateWeatherSettings(adminCredentials, city)
      setCity(saved)
      setSuccess('Ville météo enregistrée.')
    } catch (err) {
      setSaveError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    } finally {
      setIsSaving(false)
    }
  }

  if (loadError) return <p className="parametres-error">{loadError}</p>
  if (!city) return null

  const readOnly = !adminCredentials

  return (
    <div className="parametres-card">
      <h3>Ville pour la météo</h3>
      <p>Ville affichée dans le bandeau météo (aujourd'hui + prévisions 6 jours).</p>

      <p className="parametres-current-city">
        Ville actuelle : <strong>{city.city}</strong>
      </p>

      {!readOnly && (
        <div className="parametres-city-search">
          <label>
            <span>Rechercher une nouvelle ville</span>
            <input
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Ex : Palaiseau"
            />
          </label>

          {searchError && <p className="parametres-error">{searchError}</p>}

          {results.length > 0 && (
            <ul className="parametres-city-results">
              {results.map((result) => (
                <li key={`${result.name}-${result.latitude}-${result.longitude}`}>
                  <button type="button" onClick={() => selectCity(result)}>
                    {result.name}
                    {result.admin1 ? `, ${result.admin1}` : ''}
                    {result.country ? ` (${result.country})` : ''}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}

      {saveError && <p className="parametres-error">{saveError}</p>}
      {success && <p className="parametres-success">{success}</p>}

      {!readOnly && (
        <button type="button" onClick={handleSave} disabled={isSaving}>
          {isSaving ? 'Enregistrement…' : 'Enregistrer'}
        </button>
      )}
    </div>
  )
}

const EMPTY_THRESHOLD_FORM: SaveAlarmThresholdInput = { typeCode: 0, quantity: 1, sendEmail: false }

function AlarmThresholdsManager({ adminCredentials }: { adminCredentials: AdminCredentials | null }) {
  const [thresholds, setThresholds] = useState<AlarmThreshold[]>([])
  const [types, setTypes] = useState<DetectionType[]>([])
  const [form, setForm] = useState<SaveAlarmThresholdInput>({ ...EMPTY_THRESHOLD_FORM })
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editQuantity, setEditQuantity] = useState(1)
  const [editSendEmail, setEditSendEmail] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function refresh(admin: AdminCredentials) {
    setThresholds(await listAlarmThresholds(admin))
  }

  useEffect(() => {
    if (!adminCredentials) return
    listDetectionTypes().then(setTypes).catch(() => setTypes([]))
    refresh(adminCredentials).catch(() => setError('Impossible de charger les seuils.'))
  }, [adminCredentials])

  // Le droit "Gestion des comptes" est requis côté serveur pour consulter et modifier les seuils
  // (mêmes règles que le reste de l'administration) : sans lui, cette carte reste en lecture seule.
  if (!adminCredentials) {
    return (
      <div className="admin-card">
        <h3>Seuils des alarmes</h3>
        <p className="admin-hint">Réservé aux comptes disposant du droit "Gestion des comptes".</p>
      </div>
    )
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault()
    if (!adminCredentials || !form.typeCode) return
    setError(null)
    setSuccess(null)
    setIsSubmitting(true)
    try {
      const created = await createAlarmThreshold(adminCredentials, form)
      setSuccess(`Seuil pour "${created.typeName}" créé.`)
      setForm({ ...EMPTY_THRESHOLD_FORM })
      await refresh(adminCredentials)
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    } finally {
      setIsSubmitting(false)
    }
  }

  function startEdit(threshold: AlarmThreshold) {
    setEditingId(threshold.id)
    setEditQuantity(threshold.quantity)
    setEditSendEmail(threshold.sendEmail)
  }

  async function handleSaveEdit(threshold: AlarmThreshold) {
    if (!adminCredentials) return
    setError(null)
    try {
      await updateAlarmThreshold(adminCredentials, threshold.id, {
        typeCode: threshold.typeCode,
        quantity: editQuantity,
        sendEmail: editSendEmail,
      })
      setEditingId(null)
      await refresh(adminCredentials)
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    }
  }

  async function handleDelete(threshold: AlarmThreshold) {
    if (!adminCredentials) return
    if (!window.confirm(`Supprimer le seuil sur "${threshold.typeName}" ?`)) return
    setError(null)
    try {
      await deleteAlarmThreshold(adminCredentials, threshold.id)
      await refresh(adminCredentials)
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    }
  }

  const availableTypes = types.filter((t) => !thresholds.some((th) => th.typeCode === t.typeCode))

  return (
    <div className="admin-card">
      <h3>Seuils des alarmes</h3>
      <p className="admin-hint">
        Déclenche une alarme lorsque le nombre d'objets détectés d'un type atteint ou dépasse le seuil configuré, sur un même
        passage.
      </p>
      {error && <p className="admin-error">{error}</p>}

      <div className="admin-table-wrapper">
        <table className="admin-table">
          <thead>
            <tr>
              <th>Type de déchet</th>
              <th>Seuil (déclenche si &ge;)</th>
              <th>Envoyer un mail</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {thresholds.map((threshold) =>
              editingId === threshold.id ? (
                <tr key={threshold.id}>
                  <td>{threshold.typeName}</td>
                  <td>
                    <input
                      type="number"
                      min={1}
                      value={editQuantity}
                      onChange={(e) => setEditQuantity(Number(e.target.value))}
                    />
                  </td>
                  <td className="admin-table-checkbox">
                    <input type="checkbox" checked={editSendEmail} onChange={(e) => setEditSendEmail(e.target.checked)} />
                  </td>
                  <td className="admin-table-actions">
                    <button type="button" onClick={() => handleSaveEdit(threshold)}>
                      Enregistrer
                    </button>
                    <button type="button" onClick={() => setEditingId(null)}>
                      Annuler
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={threshold.id}>
                  <td>{threshold.typeName}</td>
                  <td>{threshold.quantity}</td>
                  <td className="admin-table-checkbox">
                    <input type="checkbox" checked={threshold.sendEmail} disabled />
                  </td>
                  <td className="admin-table-actions">
                    <button type="button" onClick={() => startEdit(threshold)}>
                      Modifier
                    </button>
                    <button type="button" className="admin-danger" onClick={() => handleDelete(threshold)}>
                      Supprimer
                    </button>
                  </td>
                </tr>
              ),
            )}
          </tbody>
        </table>
      </div>

      <form className="admin-role-create" onSubmit={handleCreate}>
        <label>
          <span>Type de déchet</span>
          <select
            value={form.typeCode || ''}
            onChange={(e) => setForm({ ...form, typeCode: Number(e.target.value) })}
            required
          >
            <option value="" disabled>
              Choisir un type
            </option>
            {availableTypes.map((type) => (
              <option key={type.typeCode} value={type.typeCode}>
                {type.typeName}
              </option>
            ))}
          </select>
        </label>
        <label>
          <span>Seuil</span>
          <input
            type="number"
            min={1}
            value={form.quantity}
            onChange={(e) => setForm({ ...form, quantity: Number(e.target.value) })}
            required
          />
        </label>
        <label className="parametres-checkbox">
          <input
            type="checkbox"
            checked={form.sendEmail}
            onChange={(e) => setForm({ ...form, sendEmail: e.target.checked })}
          />
          Envoyer un mail
        </label>

        {success && <p className="admin-success">{success}</p>}

        <button type="submit" disabled={isSubmitting || availableTypes.length === 0}>
          {isSubmitting ? 'Création…' : 'Ajouter le seuil'}
        </button>
      </form>
    </div>
  )
}

function AlarmEmailRecipientsManager({ adminCredentials }: { adminCredentials: AdminCredentials | null }) {
  const [recipients, setRecipients] = useState<AlarmEmailRecipient[]>([])
  const [email, setEmail] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function refresh(admin: AdminCredentials) {
    setRecipients(await listAlarmEmailRecipients(admin))
  }

  useEffect(() => {
    if (!adminCredentials) return
    refresh(adminCredentials).catch(() => setError('Impossible de charger les destinataires.'))
  }, [adminCredentials])

  if (!adminCredentials) {
    return (
      <div className="admin-card">
        <h3>Destinataires des alertes</h3>
        <p className="admin-hint">Réservé aux comptes disposant du droit "Gestion des comptes".</p>
      </div>
    )
  }

  async function handleCreate(event: FormEvent) {
    event.preventDefault()
    if (!adminCredentials) return
    setError(null)
    setSuccess(null)
    setIsSubmitting(true)
    try {
      const created = await createAlarmEmailRecipient(adminCredentials, email)
      setSuccess(`"${created.email}" ajouté aux destinataires.`)
      setEmail('')
      await refresh(adminCredentials)
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleDelete(recipient: AlarmEmailRecipient) {
    if (!adminCredentials) return
    if (!window.confirm(`Supprimer "${recipient.email}" des destinataires ?`)) return
    setError(null)
    try {
      await deleteAlarmEmailRecipient(adminCredentials, recipient.id)
      await refresh(adminCredentials)
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    }
  }

  return (
    <div className="admin-card">
      <h3>Destinataires des alertes</h3>
      <p className="admin-hint">
        Adresses e-mail notifiées lorsqu'un seuil d'alarme configuré avec "Envoyer un mail" est dépassé.
      </p>
      {error && <p className="admin-error">{error}</p>}

      <div className="admin-table-wrapper">
        <table className="admin-table">
          <thead>
            <tr>
              <th>Adresse e-mail</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {recipients.length === 0 && (
              <tr>
                <td colSpan={2} className="admin-hint">
                  Aucun destinataire configuré.
                </td>
              </tr>
            )}
            {recipients.map((recipient) => (
              <tr key={recipient.id}>
                <td>{recipient.email}</td>
                <td className="admin-table-actions">
                  <button type="button" className="admin-danger" onClick={() => handleDelete(recipient)}>
                    Supprimer
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <form className="admin-role-create" onSubmit={handleCreate}>
        <label>
          <span>Adresse e-mail</span>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            autoComplete="off"
            required
          />
        </label>

        {success && <p className="admin-success">{success}</p>}

        <button type="submit" disabled={isSubmitting}>
          {isSubmitting ? 'Ajout…' : 'Ajouter le destinataire'}
        </button>
      </form>
    </div>
  )
}
