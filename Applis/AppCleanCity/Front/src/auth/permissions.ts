export interface UserPermissions {
  manageAccounts: boolean
  viewMesures: boolean
  viewListeMesures: boolean
  viewItineraires: boolean
  viewPointsInteret: boolean
  viewAlertes: boolean
  viewParametres: boolean
  viewSysteme: boolean
  manageCortexia: boolean
}

export const NO_PERMISSIONS: UserPermissions = {
  manageAccounts: false,
  viewMesures: false,
  viewListeMesures: false,
  viewItineraires: false,
  viewPointsInteret: false,
  viewAlertes: false,
  viewParametres: false,
  viewSysteme: false,
  manageCortexia: false,
}

export const PERMISSION_LABELS: Record<keyof UserPermissions, string> = {
  manageAccounts: 'Gestion des comptes',
  viewMesures: 'Mesures',
  viewListeMesures: 'Liste des mesures',
  viewItineraires: 'Itinéraires',
  viewPointsInteret: "Points d'intérêt",
  viewAlertes: 'Alertes',
  viewParametres: 'Paramètres',
  viewSysteme: 'Système',
  manageCortexia: 'Gestion Cortexia',
}
