import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'
import { NO_PERMISSIONS, type UserPermissions } from './permissions'
import type { AdminCredentials } from '../api/authApi'

const STORAGE_KEY = 'cortexia.auth'

interface StoredAuth {
  username: string
  password: string
  accessToken: string
  tokenType: string
  permissions: UserPermissions
}

interface AuthContextValue {
  username: string | null
  accessToken: string | null
  authorizationHeader: string | null
  permissions: UserPermissions
  /** Identifiants à renvoyer aux endpoints admin (X-Admin-*), disponibles seulement si l'utilisateur a le droit ManageAccounts. */
  adminCredentials: AdminCredentials | null
  /** Identifiants du compte connecté, pour les endpoints qui revérifient un droit autre que ManageAccounts (ex: Export). */
  siteCredentials: AdminCredentials | null
  login: (username: string, password: string, accessToken: string, tokenType: string, permissions: UserPermissions) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

function readStoredAuth(): StoredAuth | null {
  const raw = sessionStorage.getItem(STORAGE_KEY)
  if (!raw) return null
  try {
    return JSON.parse(raw) as StoredAuth
  } catch {
    return null
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [auth, setAuth] = useState<StoredAuth | null>(readStoredAuth)

  const value = useMemo<AuthContextValue>(
    () => ({
      username: auth?.username ?? null,
      accessToken: auth?.accessToken ?? null,
      authorizationHeader: auth ? `${auth.tokenType} ${auth.accessToken}` : null,
      permissions: auth?.permissions ?? NO_PERMISSIONS,
      adminCredentials:
        auth?.permissions.manageAccounts ? { adminUsername: auth.username, adminPassword: auth.password } : null,
      siteCredentials: auth ? { adminUsername: auth.username, adminPassword: auth.password } : null,
      login: (username, password, accessToken, tokenType, permissions) => {
        const next = { username, password, accessToken, tokenType, permissions }
        sessionStorage.setItem(STORAGE_KEY, JSON.stringify(next))
        setAuth(next)
      },
      logout: () => {
        sessionStorage.removeItem(STORAGE_KEY)
        setAuth(null)
      },
    }),
    [auth],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth doit être utilisé à l\'intérieur de <AuthProvider>.')
  }
  return context
}
