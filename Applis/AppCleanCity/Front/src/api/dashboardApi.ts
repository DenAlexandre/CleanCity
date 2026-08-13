import { API_BASE_URL } from './config'

export interface CleanlinessScore {
  currentAverage: number | null
  previousAverage: number | null
}

export interface CleanlinessHistoryPoint {
  weekStart: string
  averageCci: number
}

export interface DirtiestStreet {
  street: string
  averageCci: number
}

export interface PointOfInterestCategoryScore {
  category: string
  averageCci: number | null
  poiCount: number
}

export interface UrgentAlert {
  measuredAt: string
  street: string | null
  typeCode: number
  typeName: string
  count: number
  threshold: number
}

function periodParams(startDate: string, endDate: string): URLSearchParams {
  return new URLSearchParams({ startDate: new Date(startDate).toISOString(), endDate: new Date(endDate).toISOString() })
}

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`)
  if (!response.ok) {
    throw new Error(`Impossible de charger ${path}.`)
  }
  return response.json()
}

export function fetchCleanlinessScore(startDate: string, endDate: string): Promise<CleanlinessScore> {
  return getJson(`/api/Dashboard/cleanliness-score?${periodParams(startDate, endDate)}`)
}

export function fetchCleanlinessHistory(startDate: string, endDate: string): Promise<CleanlinessHistoryPoint[]> {
  return getJson(`/api/Dashboard/cleanliness-history?${periodParams(startDate, endDate)}`)
}

export function fetchDirtiestStreets(startDate: string, endDate: string, limit = 5): Promise<DirtiestStreet[]> {
  const params = periodParams(startDate, endDate)
  params.set('limit', String(limit))
  return getJson(`/api/Dashboard/dirtiest-streets?${params}`)
}

export function fetchPointOfInterestScores(startDate: string, endDate: string): Promise<PointOfInterestCategoryScore[]> {
  return getJson(`/api/Dashboard/points-of-interest-scores?${periodParams(startDate, endDate)}`)
}

export function fetchUrgentAlerts(limit = 5): Promise<UrgentAlert[]> {
  return getJson(`/api/Dashboard/urgent-alerts?limit=${limit}`)
}
