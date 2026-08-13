import { API_BASE_URL } from './config'

export async function fetchEdgesAndPlacesGeoJson(authorizationHeader: string): Promise<GeoJSON.FeatureCollection> {
  const response = await fetch(`${API_BASE_URL}/api/geo/edges-and-places`, {
    headers: { Authorization: authorizationHeader },
  })

  if (!response.ok) {
    throw new Error(`Impossible de charger la cartographie Cortexia (HTTP ${response.status}).`)
  }

  return response.json()
}

/** Note (Cci moyen) par tronçon sur la période donnée, pour la coloration des détections. */
export async function fetchEdgeScoresGeoJson(startDate: string, endDate: string): Promise<GeoJSON.FeatureCollection> {
  const params = new URLSearchParams({
    startDate: new Date(startDate).toISOString(),
    endDate: new Date(endDate).toISOString(),
  })
  const response = await fetch(`${API_BASE_URL}/api/geo/local/edge-scores?${params}`)

  if (!response.ok) {
    throw new Error(`Impossible de charger les notes par tronçon (HTTP ${response.status}).`)
  }

  return response.json()
}

/** Tronçons effectivement parcourus par un itinéraire sur la période donnée. */
export async function fetchItineraryEdgesGeoJson(startDate: string, endDate: string): Promise<GeoJSON.FeatureCollection> {
  const params = new URLSearchParams({
    startDate: new Date(startDate).toISOString(),
    endDate: new Date(endDate).toISOString(),
  })
  const response = await fetch(`${API_BASE_URL}/api/geo/local/itinerary-edges?${params}`)

  if (!response.ok) {
    throw new Error(`Impossible de charger les tronçons parcourus (HTTP ${response.status}).`)
  }

  return response.json()
}

/** Noms de toutes les rues/lieux du réseau routier local, pour la recherche sur la carte. */
export async function fetchLocalStreetNames(): Promise<string[]> {
  const response = await fetch(`${API_BASE_URL}/api/geo/local/streets`)
  if (!response.ok) {
    throw new Error(`Impossible de charger la liste des rues (HTTP ${response.status}).`)
  }
  return response.json()
}

/** Géométrie de la rue/lieu portant ce nom, pour zoomer la carte dessus. */
export async function fetchStreetGeoJson(name: string): Promise<GeoJSON.FeatureCollection> {
  const params = new URLSearchParams({ name })
  const response = await fetch(`${API_BASE_URL}/api/geo/local/street?${params}`)
  if (!response.ok) {
    throw new Error(`Impossible de localiser cette rue (HTTP ${response.status}).`)
  }
  return response.json()
}
