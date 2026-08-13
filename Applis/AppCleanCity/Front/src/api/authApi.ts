import { API_BASE_URL } from './config'
import type { UserPermissions } from '../auth/permissions'

export interface CortexiaToken {
  accessToken: string
  tokenType: string
  permissions: UserPermissions
}

export class LoginError extends Error {}
export class AdminActionError extends Error {}

export async function login(username: string, password: string): Promise<CortexiaToken> {
  const response = await fetch(`${API_BASE_URL}/api/Auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password }),
  })

  if (!response.ok) {
    if (response.status === 401 || response.status === 403) {
      throw new LoginError("Nom d'utilisateur ou mot de passe incorrect.")
    }
    throw new LoginError('Connexion impossible. Merci de réessayer.')
  }

  const data = await response.json()
  return { accessToken: data.accessToken, tokenType: data.tokenType, permissions: data.permissions }
}

export interface AdminCredentials {
  adminUsername: string
  adminPassword: string
}

export interface AccountSummary {
  username: string
  email: string
  cortexiaUsername: string
  roleId: number
  roleName: string
  permissions: UserPermissions
  createdAtUtc: string
}

export interface CreateAccountInput {
  username: string
  email: string
  password: string
  cortexiaUsername: string
  cortexiaPassword: string
  roleId: number
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
    throw new AdminActionError('Cet identifiant existe déjà.')
  }
  if (response.status === 404) {
    throw new AdminActionError('Compte introuvable.')
  }
  throw new AdminActionError(data?.error ?? 'Action impossible.')
}

export async function listAccounts(admin: AdminCredentials): Promise<AccountSummary[]> {
  const response = await fetch(`${API_BASE_URL}/api/Auth/users`, { headers: adminHeaders(admin) })
  if (!response.ok) return throwOnError(response)
  return response.json()
}

export async function createAccount(admin: AdminCredentials, input: CreateAccountInput): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/Auth/users`, {
    method: 'POST',
    headers: adminHeaders(admin),
    body: JSON.stringify(input),
  })
  if (!response.ok) return throwOnError(response)
}

export interface UpdateAccountInput {
  username: string
  email: string
  cortexiaUsername: string
  /** Laisser vide pour conserver le mot de passe Cortexia existant. */
  cortexiaPassword?: string
  roleId: number
}

export async function updateAccount(admin: AdminCredentials, currentUsername: string, input: UpdateAccountInput): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/Auth/users/${encodeURIComponent(currentUsername)}`, {
    method: 'PUT',
    headers: adminHeaders(admin),
    body: JSON.stringify(input),
  })
  if (!response.ok) return throwOnError(response)
}

export async function resetPassword(admin: AdminCredentials, username: string, newPassword: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/Auth/users/${encodeURIComponent(username)}/reset-password`, {
    method: 'POST',
    headers: adminHeaders(admin),
    body: JSON.stringify({ newPassword }),
  })
  if (!response.ok) return throwOnError(response)
}

export async function deleteAccount(admin: AdminCredentials, username: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/Auth/users/${encodeURIComponent(username)}`, {
    method: 'DELETE',
    headers: adminHeaders(admin),
  })
  if (!response.ok) return throwOnError(response)
}
