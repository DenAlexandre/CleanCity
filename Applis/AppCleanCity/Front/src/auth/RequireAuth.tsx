import type { ReactNode } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from './AuthContext'

export function RequireAuth({ children }: { children: ReactNode }) {
  const { username } = useAuth()

  if (!username) {
    return <Navigate to="/login" replace />
  }

  return <>{children}</>
}
