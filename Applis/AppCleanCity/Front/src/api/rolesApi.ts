import { API_BASE_URL } from './config'
import type { UserPermissions } from '../auth/permissions'
import { AdminActionError, type AdminCredentials } from './authApi'

export interface Role {
  id: number
  name: string
  permissions: UserPermissions
}

export interface SaveRoleInput {
  name: string
  permissions: UserPermissions
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
    throw new AdminActionError(data?.error ?? 'Ce rôle existe déjà ou est utilisé.')
  }
  if (response.status === 404) {
    throw new AdminActionError('Rôle introuvable.')
  }
  throw new AdminActionError(data?.error ?? 'Action impossible.')
}

export async function listRoles(admin: AdminCredentials): Promise<Role[]> {
  const response = await fetch(`${API_BASE_URL}/api/Roles`, { headers: adminHeaders(admin) })
  if (!response.ok) return throwOnError(response)
  return response.json()
}

export async function createRole(admin: AdminCredentials, input: SaveRoleInput): Promise<Role> {
  const response = await fetch(`${API_BASE_URL}/api/Roles`, {
    method: 'POST',
    headers: adminHeaders(admin),
    body: JSON.stringify(input),
  })
  if (!response.ok) return throwOnError(response)
  return response.json()
}

export async function updateRole(admin: AdminCredentials, id: number, input: SaveRoleInput): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/Roles/${id}`, {
    method: 'PUT',
    headers: adminHeaders(admin),
    body: JSON.stringify(input),
  })
  if (!response.ok) return throwOnError(response)
}

export async function deleteRole(admin: AdminCredentials, id: number): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/Roles/${id}`, {
    method: 'DELETE',
    headers: adminHeaders(admin),
  })
  if (!response.ok) return throwOnError(response)
}
