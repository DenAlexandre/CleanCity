import { useRef, useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import { exportDatabase, ExportError } from '../api/exportApi'
import { importSnapshots, importCciMeasurements, clearImportData, ImportError } from '../api/importApi'
import { clearAlarms, AlarmsError } from '../api/alarmsApi'
import {
  assignItineraryNumbers,
  cleanupDuplicateMeasurements,
  detectAlarms,
  downloadCortexiaData,
  importEdgesAndPlaces,
  importMeasurements,
  ServerTaskError,
  type ServerTaskResult,
} from '../api/serverTasksApi'
import type { AdminCredentials } from '../api/authApi'
import './SystemePage.css'

declare global {
  interface Window {
    showSaveFilePicker?: (options?: {
      suggestedName?: string
      types?: { description: string; accept: Record<string, string[]> }[]
    }) => Promise<FileSystemFileHandle>
  }
}

function downloadBlob(blob: Blob, fileName: string): void {
  // Repli pour les navigateurs sans File System Access API (Firefox, Safari) :
  // téléchargement classique, l'emplacement dépend des réglages du navigateur.
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  link.click()
  URL.revokeObjectURL(url)
}

export function SystemePage() {
  const { siteCredentials } = useAuth()
  const [isExporting, setIsExporting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  async function handleExport() {
    if (!siteCredentials) return
    setError(null)
    setSuccess(null)

    // Ouvre la boîte "Enregistrer sous" tout de suite, dans la foulée du clic (activation
    // utilisateur), avant l'appel réseau qui peut prendre plusieurs secondes.
    let handle: FileSystemFileHandle | null = null
    if (window.showSaveFilePicker) {
      try {
        handle = await window.showSaveFilePicker({
          suggestedName: `cortexia_auth_${new Date().toISOString().slice(0, 10)}.sql`,
          types: [{ description: 'Fichier SQL', accept: { 'application/sql': ['.sql'] } }],
        })
      } catch (err) {
        if (err instanceof DOMException && err.name === 'AbortError') return
        throw err
      }
    }

    setIsExporting(true)
    try {
      const { blob, fileName } = await exportDatabase(siteCredentials)
      if (handle) {
        const writable = await handle.createWritable()
        await writable.write(blob)
        await writable.close()
      } else {
        downloadBlob(blob, fileName)
      }
      setSuccess(`Export "${fileName}" terminé.`)
    } catch (err) {
      setError(err instanceof ExportError ? err.message : 'Erreur inattendue lors de l\'export.')
    } finally {
      setIsExporting(false)
    }
  }

  return (
    <div className="systeme-page">
      <div className="systeme-card">
        <h3>Export de la base de données</h3>
        <p>Génère une sauvegarde complète de la base au format .sql (PostgreSQL / pg_dump).</p>

        {error && <p className="systeme-error">{error}</p>}
        {success && <p className="systeme-success">{success}</p>}

        <button type="button" onClick={handleExport} disabled={isExporting}>
          {isExporting ? 'Export en cours…' : 'Exporter la base de données'}
        </button>
      </div>

      <ClearImportDataCard />
      <TasksCard />
      <CortexiaFilesCard />
      <AlarmesManagementCard />
    </div>
  )
}

interface Task {
  key: string
  label: string
  description: string
  run: (credentials: AdminCredentials) => Promise<ServerTaskResult>
}

const TASKS: Task[] = [
  {
    key: 'edges-and-places',
    label: 'Synchroniser le réseau routier',
    description: 'Récupère les rues et lieux (RoadEdges/Places) depuis Cortexia.',
    run: importEdgesAndPlaces,
  },
  {
    key: 'measurements',
    label: 'Importer les relevés Cortexia',
    description: 'Récupère les nouveaux relevés et notes Cci depuis Cortexia, depuis le dernier point de reprise.',
    run: importMeasurements,
  },
  {
    key: 'cleanup-duplicates',
    label: 'Dédoublonner les mesures',
    description: 'Supprime les relevés et mesures Cci en double (ex: après un import rejoué).',
    run: cleanupDuplicateMeasurements,
  },
  {
    key: 'assign-itinerary-numbers',
    label: 'Recalculer les itinéraires',
    description: "Réattribue les numéros d'itinéraire (ex: après un import tardif de données plus anciennes).",
    run: assignItineraryNumbers,
  },
  {
    key: 'detect-alarms',
    label: 'Détecter les alarmes',
    description: 'Recherche les nouveaux dépassements de seuil et envoie les e-mails correspondants.',
    run: detectAlarms,
  },
]

// Déclenchement manuel des tâches normalement exécutées automatiquement en arrière-plan
// (CortexiaImportBackgroundService) : utile pour forcer un rafraîchissement immédiat sans attendre
// le prochain cycle périodique (jusqu'à 24h pour le réseau routier, 1h pour les relevés).
function TasksCard() {
  const { siteCredentials } = useAuth()
  const [runningKey, setRunningKey] = useState<string | null>(null)
  const [results, setResults] = useState<Record<string, string>>({})
  const [errors, setErrors] = useState<Record<string, string>>({})

  async function handleRun(task: Task) {
    if (!siteCredentials) return
    setErrors((previous) => ({ ...previous, [task.key]: '' }))
    setResults((previous) => ({ ...previous, [task.key]: '' }))
    setRunningKey(task.key)
    try {
      const result = await task.run(siteCredentials)
      setResults((previous) => ({ ...previous, [task.key]: result.message }))
    } catch (err) {
      setErrors((previous) => ({
        ...previous,
        [task.key]: err instanceof ServerTaskError ? err.message : "Erreur inattendue lors de l'exécution.",
      }))
    } finally {
      setRunningKey(null)
    }
  }

  const readOnly = !siteCredentials

  return (
    <div className="systeme-card systeme-card-wide">
      <h3>Tâches</h3>
      <p>
        Exécute immédiatement une tâche normalement lancée automatiquement en arrière-plan (lecture Cortexia,
        imports, calculs), sans attendre son prochain cycle périodique.
      </p>

      {readOnly && <p className="systeme-error">Réservé aux comptes disposant du droit "Système".</p>}

      <ul className="systeme-tasks-list">
        {TASKS.map((task) => (
          <li key={task.key} className="systeme-task">
            <div className="systeme-task-info">
              <span className="systeme-task-label">{task.label}</span>
              <span className="systeme-task-description">{task.description}</span>
              {errors[task.key] && <p className="systeme-error">{errors[task.key]}</p>}
              {results[task.key] && <p className="systeme-success">{results[task.key]}</p>}
            </div>
            <button type="button" onClick={() => handleRun(task)} disabled={readOnly || runningKey !== null}>
              {runningKey === task.key ? 'Exécution…' : 'Exécuter'}
            </button>
          </li>
        ))}
      </ul>
    </div>
  )
}

// Complète la tâche "Importer les relevés Cortexia" (qui repart toujours du dernier point de reprise) :
// permet de récupérer/rejouer manuellement les fichiers JSON bruts d'une date précise.
function CortexiaFilesCard() {
  const { siteCredentials } = useAuth()
  const [date, setDate] = useState('')
  const [isDownloading, setIsDownloading] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)

  const snapshotsInputRef = useRef<HTMLInputElement>(null)
  const cciInputRef = useRef<HTMLInputElement>(null)
  const [snapshotsFile, setSnapshotsFile] = useState<File | null>(null)
  const [cciFile, setCciFile] = useState<File | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [uploadSuccess, setUploadSuccess] = useState<string | null>(null)

  async function handleDownload() {
    if (!siteCredentials || !date) return
    setDownloadError(null)
    setIsDownloading(true)
    try {
      const { blob, fileName } = await downloadCortexiaData(siteCredentials, date)
      downloadBlob(blob, fileName)
    } catch (err) {
      setDownloadError(err instanceof ServerTaskError ? err.message : 'Erreur inattendue lors du téléchargement.')
    } finally {
      setIsDownloading(false)
    }
  }

  async function handleUpload() {
    if (!siteCredentials || (!snapshotsFile && !cciFile)) return
    setUploadError(null)
    setUploadSuccess(null)
    setIsUploading(true)
    try {
      const messages: string[] = []
      if (snapshotsFile) {
        const result = await importSnapshots(siteCredentials, snapshotsFile)
        messages.push(`${result.rowCount} relevé(s) importé(s), ${result.alarmsCreated} alarme(s) créée(s)`)
      }
      if (cciFile) {
        const result = await importCciMeasurements(siteCredentials, cciFile)
        messages.push(`${result.rowCount} mesure(s) Cci importée(s)`)
      }
      setUploadSuccess(`${messages.join(', ')}.`)
      setSnapshotsFile(null)
      setCciFile(null)
      if (snapshotsInputRef.current) snapshotsInputRef.current.value = ''
      if (cciInputRef.current) cciInputRef.current.value = ''
    } catch (err) {
      setUploadError(err instanceof ImportError ? err.message : "Erreur inattendue lors de l'import.")
    } finally {
      setIsUploading(false)
    }
  }

  const readOnly = !siteCredentials

  return (
    <div className="systeme-card systeme-card-wide">
      <h3>Fichiers Cortexia (JSON)</h3>
      <p>
        Télécharge les relevés et notes Cci bruts reçus de Cortexia pour une date donnée, ou recharge de tels
        fichiers en base — utile pour rattraper une date précise sans dépendre du point de reprise automatique.
      </p>

      {readOnly && <p className="systeme-error">Réservé aux comptes disposant du droit "Système".</p>}

      <div className="systeme-subsection-row">
        <div className="systeme-subsection">
          <h4>Télécharger</h4>
          <label className="systeme-field">
            <span>Date</span>
            <input type="date" value={date} disabled={readOnly} onChange={(e) => setDate(e.target.value)} />
          </label>

          {downloadError && <p className="systeme-error">{downloadError}</p>}

          <button type="button" onClick={handleDownload} disabled={readOnly || !date || isDownloading}>
            {isDownloading ? 'Téléchargement…' : 'Télécharger les fichiers JSON'}
          </button>
        </div>

        <div className="systeme-subsection">
          <h4>Charger en base</h4>
          <label className="systeme-field">
            <span>Relevés (aggregated_snapshots.json)</span>
            <input
              ref={snapshotsInputRef}
              type="file"
              accept=".json"
              disabled={readOnly}
              onChange={(e) => setSnapshotsFile(e.target.files?.[0] ?? null)}
            />
          </label>
          <label className="systeme-field">
            <span>Notes Cci (edges_and_places_cci.json)</span>
            <input
              ref={cciInputRef}
              type="file"
              accept=".json"
              disabled={readOnly}
              onChange={(e) => setCciFile(e.target.files?.[0] ?? null)}
            />
          </label>

          {uploadError && <p className="systeme-error">{uploadError}</p>}
          {uploadSuccess && <p className="systeme-success">{uploadSuccess}</p>}

          <button
            type="button"
            onClick={handleUpload}
            disabled={readOnly || (!snapshotsFile && !cciFile) || isUploading}
          >
            {isUploading ? 'Chargement…' : 'Charger les fichiers'}
          </button>
        </div>
      </div>
    </div>
  )
}

function ClearImportDataCard() {
  const { siteCredentials } = useAuth()
  const [fromDate, setFromDate] = useState('')
  const [isClearing, setIsClearing] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  async function handleClear() {
    if (!siteCredentials || !fromDate) return
    if (
      !window.confirm(
        `Supprimer définitivement tous les relevés, mesures Cci et alarmes à partir du ${new Date(fromDate).toLocaleString('fr-FR')} ?`,
      )
    ) {
      return
    }

    setError(null)
    setSuccess(null)
    setIsClearing(true)
    try {
      const result = await clearImportData(siteCredentials, fromDate)
      setSuccess(
        `${result.snapshotsDeleted} relevé(s), ${result.cciMeasurementsDeleted} mesure(s) Cci et ${result.alarmsDeleted} alarme(s) supprimé(s).`,
      )
    } catch (err) {
      setError(err instanceof ImportError ? err.message : 'Erreur inattendue lors de la suppression.')
    } finally {
      setIsClearing(false)
    }
  }

  const readOnly = !siteCredentials

  return (
    <div className="systeme-card">
      <h3>Données importées</h3>
      <p>
        Supprime les relevés, mesures Cci et alarmes à partir d'une date/heure donnée (incluse). Le réseau routier et
        les lieux ne sont pas concernés. Ces données ne seront pas récupérées automatiquement au prochain import
        Cortexia.
      </p>

      {readOnly && <p className="systeme-error">Réservé aux comptes disposant du droit "Système".</p>}

      <label className="systeme-field">
        <span>À partir de</span>
        <input
          type="datetime-local"
          value={fromDate}
          disabled={readOnly}
          onChange={(e) => setFromDate(e.target.value)}
        />
      </label>

      {error && <p className="systeme-error">{error}</p>}
      {success && <p className="systeme-success">{success}</p>}

      <button type="button" className="systeme-danger" onClick={handleClear} disabled={readOnly || !fromDate || isClearing}>
        {isClearing ? 'Suppression…' : 'Supprimer'}
      </button>
    </div>
  )
}

function AlarmesManagementCard() {
  const { siteCredentials } = useAuth()
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [selectedFile, setSelectedFile] = useState<File | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)
  const [uploadSuccess, setUploadSuccess] = useState<string | null>(null)

  const [isClearing, setIsClearing] = useState(false)
  const [clearError, setClearError] = useState<string | null>(null)
  const [clearSuccess, setClearSuccess] = useState<string | null>(null)

  async function handleUpload() {
    if (!siteCredentials || !selectedFile) return
    setUploadError(null)
    setUploadSuccess(null)
    setIsUploading(true)
    try {
      const result = await importSnapshots(siteCredentials, selectedFile)
      setUploadSuccess(
        `${result.rowCount} relevé(s) importé(s), ${result.alarmsCreated} alarme(s) créée(s).`,
      )
      setSelectedFile(null)
      if (fileInputRef.current) fileInputRef.current.value = ''
    } catch (err) {
      setUploadError(err instanceof ImportError ? err.message : "Erreur inattendue lors de l'import.")
    } finally {
      setIsUploading(false)
    }
  }

  async function handleClear() {
    if (!siteCredentials) return
    if (!window.confirm('Supprimer définitivement toutes les alarmes enregistrées ?')) return

    setClearError(null)
    setClearSuccess(null)
    setIsClearing(true)
    try {
      const result = await clearAlarms(siteCredentials)
      setClearSuccess(`${result.deletedCount} alarme(s) supprimée(s).`)
    } catch (err) {
      setClearError(err instanceof AlarmsError ? err.message : 'Erreur inattendue lors de la suppression.')
    } finally {
      setIsClearing(false)
    }
  }

  const readOnly = !siteCredentials

  return (
    <div className="systeme-card systeme-card-wide">
      <h3>Alarmes</h3>

      {readOnly && <p className="systeme-error">Réservé aux comptes disposant du droit "Système".</p>}

      <div className="systeme-subsection-row">
        <div className="systeme-subsection">
          <h4>Charger des relevés de test</h4>
          <p>
            Importe un fichier JSON de relevés (format "aggregated_snapshots" de Cortexia) pour simuler des
            détections et déclencher de fausses alarmes. Voir un{' '}
            <a href="/samples/fake-alarm-seringues.json" target="_blank" rel="noreferrer">
              exemple de fichier (seringues)
            </a>
            .
          </p>

          <input
            ref={fileInputRef}
            type="file"
            accept=".json"
            disabled={readOnly}
            onChange={(e) => setSelectedFile(e.target.files?.[0] ?? null)}
          />

          {uploadError && <p className="systeme-error">{uploadError}</p>}
          {uploadSuccess && <p className="systeme-success">{uploadSuccess}</p>}

          <button type="button" onClick={handleUpload} disabled={readOnly || !selectedFile || isUploading}>
            {isUploading ? 'Import en cours…' : 'Charger le fichier'}
          </button>
        </div>

        <div className="systeme-subsection">
          <h4>Vider l'historique</h4>
          <p>Supprime tout l'historique des alarmes enregistrées (utile pour repartir de zéro après des tests).</p>

          {clearError && <p className="systeme-error">{clearError}</p>}
          {clearSuccess && <p className="systeme-success">{clearSuccess}</p>}

          <button type="button" className="systeme-danger" onClick={handleClear} disabled={readOnly || isClearing}>
            {isClearing ? 'Suppression…' : 'Vider les alarmes'}
          </button>
        </div>
      </div>
    </div>
  )
}
