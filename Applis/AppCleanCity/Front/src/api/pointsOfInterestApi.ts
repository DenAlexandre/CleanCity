import { API_BASE_URL } from './config'
import type { AdminCredentials } from './authApi'
import { AdminActionError } from './authApi'
import type { MeasurementSortColumn, MeasurementTypeBreakdown, PagedMeasurements, SortDirection } from './measurementsApi'

export interface PointOfInterest {
  id: string
  name: string
  description: string | null
  category: string
  latitude: number
  longitude: number
  createdAtUtc: string
}

export interface SavePointOfInterestInput {
  name: string
  description: string
  category: string
  latitude: number
  longitude: number
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
  if (response.status === 404) {
    throw new AdminActionError('Point d\'intérêt introuvable.')
  }
  throw new AdminActionError(data?.error ?? 'Action impossible.')
}

export async function listPointsOfInterest(): Promise<PointOfInterest[]> {
  const response = await fetch(`${API_BASE_URL}/api/PointsOfInterest`)
  if (!response.ok) return throwOnError(response)
  return response.json()
}

export async function createPointOfInterest(admin: AdminCredentials, input: SavePointOfInterestInput): Promise<PointOfInterest> {
  const response = await fetch(`${API_BASE_URL}/api/PointsOfInterest`, {
    method: 'POST',
    headers: adminHeaders(admin),
    body: JSON.stringify(input),
  })
  if (!response.ok) return throwOnError(response)
  return response.json()
}

export async function updatePointOfInterest(admin: AdminCredentials, id: string, input: SavePointOfInterestInput): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/PointsOfInterest/${encodeURIComponent(id)}`, {
    method: 'PUT',
    headers: adminHeaders(admin),
    body: JSON.stringify(input),
  })
  if (!response.ok) return throwOnError(response)
}

export async function deletePointOfInterest(admin: AdminCredentials, id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}/api/PointsOfInterest/${encodeURIComponent(id)}`, {
    method: 'DELETE',
    headers: adminHeaders(admin),
  })
  if (!response.ok) return throwOnError(response)
}

export interface PointOfInterestScore {
  id: string
  name: string
  description: string | null
  category: string
  averageCci: number | null
}

export async function fetchPointOfInterestScoresList(startDate: string, endDate: string): Promise<PointOfInterestScore[]> {
  const params = new URLSearchParams({
    startDate: new Date(startDate).toISOString(),
    endDate: new Date(endDate).toISOString(),
  })
  const response = await fetch(`${API_BASE_URL}/api/PointsOfInterest/scores?${params}`)
  if (!response.ok) {
    throw new Error('Impossible de charger les notes des points d\'intérêt.')
  }
  return response.json()
}

export interface PointOfInterestObjectBreakdown {
  typeCode: number
  typeName: string
  count: number
}

export async function fetchPointOfInterestObjects(
  id: string,
  startDate: string,
  endDate: string,
): Promise<PointOfInterestObjectBreakdown[]> {
  const params = new URLSearchParams({
    startDate: new Date(startDate).toISOString(),
    endDate: new Date(endDate).toISOString(),
  })
  const response = await fetch(`${API_BASE_URL}/api/PointsOfInterest/${encodeURIComponent(id)}/objects?${params}`)
  if (!response.ok) {
    throw new Error('Impossible de charger le détail des objets détectés.')
  }
  return response.json()
}

export interface PointOfInterestMeasurementFilters {
  startDate: string
  endDate: string
  typeCode?: number
  category?: string
  poiId?: string
}

/** Objets détectés dans le rayon (configuré page Paramètres) d'au moins un point d'intérêt. */
export async function fetchPointOfInterestMeasurements(
  page: number,
  pageSize: number,
  sortBy: MeasurementSortColumn,
  sortDir: SortDirection,
  filters: PointOfInterestMeasurementFilters,
): Promise<PagedMeasurements> {
  const params = new URLSearchParams({
    page: String(page),
    pageSize: String(pageSize),
    sortBy,
    sortDir,
    startDate: new Date(filters.startDate).toISOString(),
    endDate: new Date(filters.endDate).toISOString(),
  })
  if (filters.typeCode !== undefined) params.set('typeCode', String(filters.typeCode))
  if (filters.category) params.set('category', filters.category)
  if (filters.poiId) params.set('poiId', filters.poiId)

  const response = await fetch(`${API_BASE_URL}/api/PointsOfInterest/measurements?${params}`)
  if (!response.ok) {
    throw new Error("Impossible de charger les objets détectés à proximité des points d'intérêt.")
  }
  return response.json()
}

export interface PointOfInterestMeasurementBreakdownFilters {
  startDate: string
  endDate: string
  category?: string
  poiId?: string
}

/** Répartition par type des objets détectés à proximité d'un point d'intérêt, pour le camembert. */
export async function fetchPointOfInterestMeasurementTypeBreakdown(
  filters: PointOfInterestMeasurementBreakdownFilters,
): Promise<MeasurementTypeBreakdown[]> {
  const params = new URLSearchParams({
    startDate: new Date(filters.startDate).toISOString(),
    endDate: new Date(filters.endDate).toISOString(),
  })
  if (filters.category) params.set('category', filters.category)
  if (filters.poiId) params.set('poiId', filters.poiId)

  const response = await fetch(`${API_BASE_URL}/api/PointsOfInterest/measurements/type-breakdown?${params}`)
  if (!response.ok) {
    throw new Error('Impossible de charger la répartition par type.')
  }
  return response.json()
}
