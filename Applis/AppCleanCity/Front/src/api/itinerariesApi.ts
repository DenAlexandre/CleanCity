import { API_BASE_URL } from './config'

export interface Itinerary {
  suitcaseId: string
  day: string
  itineraryIndex: number
  startTime: string
  endTime: string
  objectCount: number
  streets: string[]
  averageCci: number | null
}

// startDate/endDate optionnels : un appel sans dates renvoie tous les itinéraires (toutes périodes
// confondues), utilisé par le sélecteur d'itinéraire de la Période, indépendant des dates choisies.
export async function fetchItineraries(startDate?: string, endDate?: string): Promise<Itinerary[]> {
  const params = new URLSearchParams()
  if (startDate) params.set('startDate', new Date(startDate).toISOString())
  if (endDate) params.set('endDate', new Date(endDate).toISOString())
  const query = params.toString()

  const response = await fetch(`${API_BASE_URL}/api/Itineraries${query ? `?${query}` : ''}`)
  if (!response.ok) {
    throw new Error('Impossible de charger les itinéraires.')
  }
  return response.json()
}

export function itineraryKey(itinerary: Pick<Itinerary, 'suitcaseId' | 'day' | 'itineraryIndex'>): string {
  return `${itinerary.suitcaseId}|${itinerary.day}|${itinerary.itineraryIndex}`
}

export interface ItineraryObjectBreakdown {
  typeCode: number
  typeName: string
  count: number
}

export interface ItineraryStreetDetail {
  street: string
  totalObjects: number
  averageCci: number | null
  objects: ItineraryObjectBreakdown[]
}

export async function fetchItineraryStreets(suitcaseId: string, day: string, itineraryIndex: number): Promise<ItineraryStreetDetail[]> {
  const params = new URLSearchParams({ suitcaseId, day, itineraryIndex: String(itineraryIndex) })
  const response = await fetch(`${API_BASE_URL}/api/Itineraries/streets?${params}`)
  if (!response.ok) {
    throw new Error("Impossible de charger le détail par rue de l'itinéraire.")
  }
  return response.json()
}
