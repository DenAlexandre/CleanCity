import type { ReactNode } from 'react'
import { useAuth } from './AuthContext'
import type { UserPermissions } from './permissions'
import { PlaceholderPage } from '../pages/PlaceholderPage'

interface RequirePermissionProps {
  permission: keyof UserPermissions
  children: ReactNode
}

export function RequirePermission({ permission, children }: RequirePermissionProps) {
  const { permissions } = useAuth()

  if (!permissions[permission]) {
    return <PlaceholderPage title="Accès non autorisé" message="Vous n'avez pas les droits nécessaires pour accéder à cette section." />
  }

  return <>{children}</>
}
