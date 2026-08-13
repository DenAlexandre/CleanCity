import type { ReactNode } from 'react'
import type { UserPermissions } from '../auth/permissions'

// Libellés et pages provisoires : le contenu détaillé de chaque section sera précisé plus tard.
// La structure (icônes, routes, repli de la barre, permissions) est en place et prête à être complétée.
export interface NavItem {
  key: string
  label: string
  path: string
  icon: ReactNode
  permission: keyof UserPermissions
}

const iconProps = {
  width: 20,
  height: 20,
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 1.8,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
}

export const navItems: NavItem[] = [
  {
    key: 'home',
    label: 'Accueil',
    path: '/',
    permission: 'viewMesures',
    icon: (
      <svg {...iconProps}>
        <path d="M3 11.5 12 4l9 7.5" />
        <path d="M5.5 10v9.5a1 1 0 0 0 1 1H9v-6h6v6h2.5a1 1 0 0 0 1-1V10" />
      </svg>
    ),
  },
  {
    key: 'map',
    label: 'Cartographie',
    path: '/mesures',
    permission: 'viewMesures',
    icon: (
      <svg {...iconProps}>
        <path d="M9 20 3 18V5l6 2 6-2 6 2v13l-6-2-6 2Z" />
        <path d="M9 7v13M15 5v13" />
      </svg>
    ),
  },
  {
    key: 'measures-list',
    label: 'Liste des mesures',
    path: '/liste-mesures',
    permission: 'viewListeMesures',
    icon: (
      <svg {...iconProps}>
        <path d="M4 6h5M4 12h5M4 18h5" />
        <path d="M12 6h8M12 12h8M12 18h8" strokeWidth={1.2} opacity={0.5} />
      </svg>
    ),
  },
  {
    key: 'alerts',
    label: 'Alertes',
    path: '/alertes',
    permission: 'viewAlertes',
    icon: (
      <svg {...iconProps}>
        <path d="M12 4 3 20h18L12 4Z" />
        <path d="M12 10v4M12 17h.01" />
      </svg>
    ),
  },
  {
    key: 'settings',
    label: 'Paramètres',
    path: '/parametres',
    permission: 'viewParametres',
    icon: (
      <svg {...iconProps}>
        <path d="M4 6h6M14 6h6" />
        <circle cx="10" cy="6" r="2" />
        <path d="M4 12h10M18 12h2" />
        <circle cx="16" cy="12" r="2" />
        <path d="M4 18h2M10 18h10" />
        <circle cx="7" cy="18" r="2" />
      </svg>
    ),
  },
  {
    key: 'systeme',
    label: 'Système',
    path: '/systeme',
    permission: 'viewSysteme',
    icon: (
      <svg {...iconProps}>
        <circle cx="12" cy="12" r="3" />
        <path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 1 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 1 1-4 0v-.09a1.65 1.65 0 0 0-1-1.51 1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 1 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 1 1 0-4h.09a1.65 1.65 0 0 0 1.51-1 1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 1 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33h0A1.65 1.65 0 0 0 10 3.09V3a2 2 0 1 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 1 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82v0c.26.604.85 1 1.51 1H21a2 2 0 1 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1Z" />
      </svg>
    ),
  },
  {
    key: 'admin',
    label: 'Administration',
    path: '/administration',
    permission: 'manageAccounts',
    icon: (
      <svg {...iconProps}>
        <path d="M12 3 4 6v6c0 5 3.5 7.5 8 9 4.5-1.5 8-4 8-9V6l-8-3Z" />
        <path d="m9 12 2 2 4-4" />
      </svg>
    ),
  },
]
