import { API_BASE_URL } from './config'
import { AdminActionError, type AdminCredentials } from './authApi'

export interface AlarmEmailRecipient {
  id: number
  email: string
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
    throw new AdminActionError(data?.error ?? 'Cette adresse e-mail est déjà destinataire.')
  }
  if (response.status === 404) {
    throw new AdminActionError('Destinataire introuvable.')
  }
  throw new AdminActionError(data?.error ?? 'Action impossible.')
}

export async function listAlarmEmailRecipients(admin: AdminCredentials): Promise<AlarmEmailRecipient[]> {
  const response = await fetch(`${API_BASE_URL}/api/AlarmEmailRecipients`, { headers: adminHeaders(admin) })
  if (!response.ok) return throwOnError(response)
  return response.json()
}

export async function createAlarmEmailRecipient(admin: AdminCredentials, email: string): Promise<AlarmEmailRecipient> {
  const response = await fetch(`${API_BASE_URL}/api/AlarmEmailRecipients`, {
    method: 'POST',
    headers: adminHeaders(admin),
    body: JSON.stringify({ email }),
  })
  if (!response.ok) return throwOnError(response)
  return response.json()
}

export async function deleteAlarmEmailRecipient(admin: AdminCredentials, id: number): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/AlarmEmailRecipients/${id}`, {
    method: 'DELETE',
    headers: adminHeaders(admin),
  })
  if (!response.ok) return throwOnError(response)
}
