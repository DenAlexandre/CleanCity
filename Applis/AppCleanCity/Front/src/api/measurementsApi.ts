import { API_BASE_URL } from './config'

export interface Measurement {
  snapshotId: number
  typeCode: number
  typeName: string
  quantity: number
  measuredAt: string
  street: string | null
  latitude: number
  longitude: number
}

export interface PagedMeasurements {
  total: number
  totalObjects: number
  page: number
  pageSize: number
  items: Measurement[]
}

export type MeasurementSortColumn = 'measuredAt' | 'type' | 'street' | 'latitude' | 'longitude' | 'quantity'
export type SortDirection = 'asc' | 'desc'

export interface MeasurementFilters {
  startDate: string
  endDate: string
  typeCode?: number
  street?: string
}

export async function fetchMeasurements(
  page: number,
  pageSize: number,
  sortBy: MeasurementSortColumn,
  sortDir: SortDirection,
  filters: MeasurementFilters,
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
  if (filters.street) params.set('street', filters.street)

  const response = await fetch(`${API_BASE_URL}/api/Measurements?${params}`)
  if (!response.ok) {
    throw new Error('Impossible de charger la liste des mesures.')
  }
  return response.json()
}

export interface MeasurementTypeBreakdown {
  typeCode: number
  typeName: string
  count: number
}

export async function fetchMeasurementTypeBreakdown(startDate: string, endDate: string, street?: string): Promise<MeasurementTypeBreakdown[]> {
  const params = new URLSearchParams({
    startDate: new Date(startDate).toISOString(),
    endDate: new Date(endDate).toISOString(),
  })
  if (street) params.set('street', street)

  const response = await fetch(`${API_BASE_URL}/api/Measurements/type-breakdown?${params}`)
  if (!response.ok) {
    throw new Error('Impossible de charger la répartition par type.')
  }
  return response.json()
}

export interface MeasurementPoint {
  latitude: number
  longitude: number
  typeCode: number
  typeName: string
  quantity: number
  street: string | null
}

export async function fetchMeasurementPoints(startDate: string, endDate: string): Promise<MeasurementPoint[]> {
  const params = new URLSearchParams({
    startDate: new Date(startDate).toISOString(),
    endDate: new Date(endDate).toISOString(),
  })

  const response = await fetch(`${API_BASE_URL}/api/Measurements/points?${params}`)
  if (!response.ok) {
    throw new Error('Impossible de charger les points de détection.')
  }
  return response.json()
}

export async function fetchMeasurementStreets(startDate: string, endDate: string, typeCode?: number): Promise<string[]> {
  const params = new URLSearchParams({
    startDate: new Date(startDate).toISOString(),
    endDate: new Date(endDate).toISOString(),
  })
  if (typeCode !== undefined) params.set('typeCode', String(typeCode))

  const response = await fetch(`${API_BASE_URL}/api/Measurements/streets?${params}`)
  if (!response.ok) {
    throw new Error('Impossible de charger la liste des rues.')
  }
  return response.json()
}
