import { API_BASE_URL } from './config'
import type { AdminCredentials } from './authApi'

export class ExportError extends Error {}

export async function exportDatabase(credentials: AdminCredentials): Promise<{ blob: Blob; fileName: string }> {
  const response = await fetch(`${API_BASE_URL}/api/Export/database`, {
    headers: {
      'X-Admin-Username': credentials.adminUsername,
      'X-Admin-Password': credentials.adminPassword,
    },
  })

  if (!response.ok) {
    const data = await response.json().catch(() => null)
    throw new ExportError(data?.error ?? "Échec de l'export de la base de données.")
  }

  const disposition = response.headers.get('Content-Disposition') ?? ''
  const match = /filename="?([^";]+)"?/.exec(disposition)
  const fileName = match?.[1] ?? `cortexia_auth_${new Date().toISOString().slice(0, 10)}.sql`

  return { blob: await response.blob(), fileName }
}

/**
 * Restaure la base à partir d'un fichier .sql généré par exportDatabase : efface et recrée les objets
 * existants (le dump contient déjà des "DROP ... IF EXISTS") — destructeur et irréversible.
 */
export async function restoreDatabase(credentials: AdminCredentials, file: File): Promise<void> {
  const formData = new FormData()
  formData.append('file', file)

  const response = await fetch(`${API_BASE_URL}/api/Export/restore`, {
    method: 'POST',
    headers: {
      'X-Admin-Username': credentials.adminUsername,
      'X-Admin-Password': credentials.adminPassword,
    },
    body: formData,
  })

  if (!response.ok) {
    const data = await response.json().catch(() => null)
    throw new ExportError(data?.error ?? 'Échec de la restauration de la base de données.')
  }
}
