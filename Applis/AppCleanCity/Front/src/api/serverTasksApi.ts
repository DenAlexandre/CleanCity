import { API_BASE_URL } from './config'
import type { AdminCredentials } from './authApi'

export class ServerTaskError extends Error {}

export interface ServerTaskResult {
  message: string
}

async function runTask(route: string, credentials: AdminCredentials): Promise<ServerTaskResult> {
  const response = await fetch(`${API_BASE_URL}/api/tasks/${route}`, {
    method: 'POST',
    headers: {
      'X-Admin-Username': credentials.adminUsername,
      'X-Admin-Password': credentials.adminPassword,
    },
  })

  if (!response.ok) {
    const data = await response.json().catch(() => null)
    throw new ServerTaskError(data?.error ?? "Échec de l'exécution de la tâche.")
  }

  return response.json()
}

/** Synchronise le réseau routier et les lieux (RoadEdges/Places) depuis Cortexia. */
export const importEdgesAndPlaces = (credentials: AdminCredentials) => runTask('edges-and-places', credentials)

/** Importe les relevés et notes Cci depuis Cortexia (depuis le dernier point de reprise). */
export const importMeasurements = (credentials: AdminCredentials) => runTask('measurements', credentials)

/** Supprime les relevés/mesures Cci en double (ré-import après un échec partiel, par exemple). */
export const cleanupDuplicateMeasurements = (credentials: AdminCredentials) => runTask('cleanup-duplicates', credentials)

/** Recalcule les numéros d'itinéraire (utile après un import tardif de données plus anciennes). */
export const assignItineraryNumbers = (credentials: AdminCredentials) => runTask('assign-itinerary-numbers', credentials)

/** Détecte les nouveaux dépassements de seuil et envoie les e-mails d'alarme correspondants. */
export const detectAlarms = (credentials: AdminCredentials) => runTask('detect-alarms', credentials)
