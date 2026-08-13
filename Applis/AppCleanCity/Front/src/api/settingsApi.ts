import { API_BASE_URL } from './config'
import { AdminActionError, type AdminCredentials } from './authApi'

export interface DetectionDisplaySettings {
  positiveMin: number
  positiveMax: number
  positiveColor: string
  averageMin: number
  averageMax: number
  averageColor: string
  hideObjectsWithoutStreet: boolean
}

function adminHeaders(admin: AdminCredentials): HeadersInit {
  return {
    'Content-Type': 'application/json',
    'X-Admin-Username': admin.adminUsername,
    'X-Admin-Password': admin.adminPassword,
  }
}

export async function fetchDetectionDisplaySettings(): Promise<DetectionDisplaySettings> {
  const response = await fetch(`${API_BASE_URL}/api/Settings/detection-display`)
  if (!response.ok) {
    throw new Error('Impossible de charger les paramètres de détection.')
  }
  return response.json()
}

export async function updateDetectionDisplaySettings(
  admin: AdminCredentials,
  settings: DetectionDisplaySettings,
): Promise<DetectionDisplaySettings> {
  const response = await fetch(`${API_BASE_URL}/api/Settings/detection-display`, {
    method: 'PUT',
    headers: adminHeaders(admin),
    body: JSON.stringify(settings),
  })
  if (!response.ok) {
    const data = await response.json().catch(() => null)
    throw new AdminActionError(data?.error ?? 'Impossible d\'enregistrer les paramètres.')
  }
  return response.json()
}

export interface WeatherSettings {
  city: string
  latitude: number
  longitude: number
}

export async function fetchWeatherSettings(): Promise<WeatherSettings> {
  const response = await fetch(`${API_BASE_URL}/api/Settings/weather`)
  if (!response.ok) {
    throw new Error('Impossible de charger la ville météo.')
  }
  return response.json()
}

export async function updateWeatherSettings(admin: AdminCredentials, settings: WeatherSettings): Promise<WeatherSettings> {
  const response = await fetch(`${API_BASE_URL}/api/Settings/weather`, {
    method: 'PUT',
    headers: adminHeaders(admin),
    body: JSON.stringify(settings),
  })
  if (!response.ok) {
    const data = await response.json().catch(() => null)
    throw new AdminActionError(data?.error ?? 'Impossible d\'enregistrer la ville météo.')
  }
  return response.json()
}

export interface PointOfInterestSettings {
  radiusMeters: number
}

export async function fetchPointOfInterestSettings(): Promise<PointOfInterestSettings> {
  const response = await fetch(`${API_BASE_URL}/api/Settings/point-of-interest`)
  if (!response.ok) {
    throw new Error('Impossible de charger le rayon des points d\'intérêt.')
  }
  return response.json()
}

export async function updatePointOfInterestSettings(
  admin: AdminCredentials,
  settings: PointOfInterestSettings,
): Promise<PointOfInterestSettings> {
  const response = await fetch(`${API_BASE_URL}/api/Settings/point-of-interest`, {
    method: 'PUT',
    headers: adminHeaders(admin),
    body: JSON.stringify(settings),
  })
  if (!response.ok) {
    const data = await response.json().catch(() => null)
    throw new AdminActionError(data?.error ?? 'Impossible d\'enregistrer le rayon.')
  }
  return response.json()
}
