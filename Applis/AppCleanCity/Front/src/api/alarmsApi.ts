import { API_BASE_URL } from './config'
import type { AdminCredentials } from './authApi'

export class AlarmsError extends Error {}

export interface Alarm {
  id: number
  measuredAt: string
  street: string | null
  typeCode: number
  typeName: string
  count: number
  threshold: number
  emailSent: boolean
}

export interface PagedAlarms {
  total: number
  page: number
  pageSize: number
  items: Alarm[]
}

export async function fetchAlarms(page: number, pageSize: number, startDate?: string, endDate?: string): Promise<PagedAlarms> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (startDate) params.set('startDate', new Date(startDate).toISOString())
  if (endDate) params.set('endDate', new Date(endDate).toISOString())

  const response = await fetch(`${API_BASE_URL}/api/Alarms?${params}`)
  if (!response.ok) {
    throw new Error('Impossible de charger les alarmes.')
  }
  return response.json()
}

export interface ClearAlarmsResult {
  deletedCount: number
}

export async function clearAlarms(credentials: AdminCredentials): Promise<ClearAlarmsResult> {
  const response = await fetch(`${API_BASE_URL}/api/Alarms`, {
    method: 'DELETE',
    headers: {
      'X-Admin-Username': credentials.adminUsername,
      'X-Admin-Password': credentials.adminPassword,
    },
  })

  if (!response.ok) {
    const data = await response.json().catch(() => null)
    throw new AlarmsError(data?.error ?? 'Échec de la suppression des alarmes.')
  }

  return response.json()
}
