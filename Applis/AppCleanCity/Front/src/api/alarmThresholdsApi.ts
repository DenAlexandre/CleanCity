import { API_BASE_URL } from './config'
import { AdminActionError, type AdminCredentials } from './authApi'

export interface AlarmThreshold {
  id: number
  typeCode: number
  typeName: string
  quantity: number
  sendEmail: boolean
}

export interface DetectionType {
  typeCode: number
  typeName: string
}

export interface SaveAlarmThresholdInput {
  typeCode: number
  quantity: number
  sendEmail: boolean
}

function adminHeaders(admin: AdminCredentials): HeadersInit {
  return {
    'Content-Type': 'application/json',
    'X-Admin-Username': admin.adminUsername,
    'X-Admin-Password': admin.adminPassword,
  }
}

async function throwOnError(response: Response): Promise<never> {
  const data = await response.json().catch(() => null)
  if (response.status === 401) {
    throw new AdminActionError('Identifiants administrateur invalides.')
  }
  if (response.status === 409) {
    throw new AdminActionError(data?.error ?? 'Un seuil existe déjà pour ce type.')
  }
  if (response.status === 404) {
    throw new AdminActionError('Seuil introuvable.')
  }
  throw new AdminActionError(data?.error ?? 'Action impossible.')
}

export async function listAlarmThresholds(admin: AdminCredentials): Promise<AlarmThreshold[]> {
  const response = await fetch(`${API_BASE_URL}/api/AlarmThresholds`, { headers: adminHeaders(admin) })
  if (!response.ok) return throwOnError(response)
  return response.json()
}

export async function listDetectionTypes(): Promise<DetectionType[]> {
  const response = await fetch(`${API_BASE_URL}/api/AlarmThresholds/types`)
  if (!response.ok) throw new Error('Impossible de charger le catalogue des types.')
  return response.json()
}

export async function createAlarmThreshold(admin: AdminCredentials, input: SaveAlarmThresholdInput): Promise<AlarmThreshold> {
  const response = await fetch(`${API_BASE_URL}/api/AlarmThresholds`, {
    method: 'POST',
    headers: adminHeaders(admin),
    body: JSON.stringify(input),
  })
  if (!response.ok) return throwOnError(response)
  return response.json()
}

export async function updateAlarmThreshold(admin: AdminCredentials, id: number, input: SaveAlarmThresholdInput): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/AlarmThresholds/${id}`, {
    method: 'PUT',
    headers: adminHeaders(admin),
    body: JSON.stringify(input),
  })
  if (!response.ok) return throwOnError(response)
}

export async function deleteAlarmThreshold(admin: AdminCredentials, id: number): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/AlarmThresholds/${id}`, {
    method: 'DELETE',
    headers: adminHeaders(admin),
  })
  if (!response.ok) return throwOnError(response)
}
