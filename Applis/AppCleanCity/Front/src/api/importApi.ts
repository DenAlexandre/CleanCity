import { API_BASE_URL } from './config'
import type { AdminCredentials } from './authApi'

export class ImportError extends Error {}

export interface ImportSnapshotsResult {
  rowCount: number
  alarmsCreated: number
}

/** Charge un fichier de relevés (aggregated_snapshots) et déclenche la détection d'alarmes. */
export async function importSnapshots(credentials: AdminCredentials, file: File): Promise<ImportSnapshotsResult> {
  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(`${API_BASE_URL}/api/import/snapshots`, {
    method: 'POST',
    headers: {
      'X-Admin-Username': credentials.adminUsername,
      'X-Admin-Password': credentials.adminPassword,
    },
    body: formData,
  })

  if (!response.ok) {
    const data = await response.json().catch(() => null)
    throw new ImportError(data?.error ?? "Échec de l'import du fichier.")
  }

  return response.json()
}

export interface ClearImportDataResult {
  alarmsDeleted: number
  snapshotsDeleted: number
  cciMeasurementsDeleted: number
}

/** Supprime relevés, mesures Cci et alarmes à partir d'une date/heure donnée. */
export async function clearImportData(credentials: AdminCredentials, fromDate: string): Promise<ClearImportDataResult> {
  const params = new URLSearchParams({ fromDate: new Date(fromDate).toISOString() })

  const response = await fetch(`${API_BASE_URL}/api/import/data?${params}`, {
    method: 'DELETE',
    headers: {
      'X-Admin-Username': credentials.adminUsername,
      'X-Admin-Password': credentials.adminPassword,
    },
  })

  if (!response.ok) {
    const data = await response.json().catch(() => null)
    throw new ImportError(data?.error ?? 'Échec de la suppression des données.')
  }

  return response.json()
}
