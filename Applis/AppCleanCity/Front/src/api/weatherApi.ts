// Open-Meteo : API météo gratuite, sans clé, utilisable directement depuis le navigateur.

export interface DailyForecast {
  date: string
  weatherCode: number
  temperatureMax: number
  temperatureMin: number
}

export interface WeatherData {
  currentTemperature: number
  currentWeatherCode: number
  daily: DailyForecast[]
}

interface OpenMeteoResponse {
  current: { temperature_2m: number; weather_code: number }
  daily: { time: string[]; weather_code: number[]; temperature_2m_max: number[]; temperature_2m_min: number[] }
}

export async function fetchWeather(latitude: number, longitude: number): Promise<WeatherData> {
  const params = new URLSearchParams({
    latitude: String(latitude),
    longitude: String(longitude),
    current: 'temperature_2m,weather_code',
    daily: 'weather_code,temperature_2m_max,temperature_2m_min',
    timezone: 'Europe/Paris',
    forecast_days: '7',
  })

  const response = await fetch(`https://api.open-meteo.com/v1/forecast?${params}`)
  if (!response.ok) {
    throw new Error('Impossible de charger la météo.')
  }
  const data: OpenMeteoResponse = await response.json()

  return {
    currentTemperature: data.current.temperature_2m,
    currentWeatherCode: data.current.weather_code,
    daily: data.daily.time.map((date, index) => ({
      date,
      weatherCode: data.daily.weather_code[index],
      temperatureMax: data.daily.temperature_2m_max[index],
      temperatureMin: data.daily.temperature_2m_min[index],
    })),
  }
}

export interface CitySearchResult {
  name: string
  country: string | null
  admin1: string | null
  latitude: number
  longitude: number
}

interface OpenMeteoGeocodingResponse {
  results?: { name: string; country?: string; admin1?: string; latitude: number; longitude: number }[]
}

/** Recherche de ville via l'API de géocodage Open-Meteo (gratuite, sans clé). */
export async function searchCities(query: string): Promise<CitySearchResult[]> {
  if (query.trim().length < 2) return []

  const params = new URLSearchParams({ name: query, count: '5', language: 'fr', format: 'json' })
  const response = await fetch(`https://geocoding-api.open-meteo.com/v1/search?${params}`)
  if (!response.ok) {
    throw new Error('Impossible de rechercher cette ville.')
  }
  const data: OpenMeteoGeocodingResponse = await response.json()

  return (data.results ?? []).map((r) => ({
    name: r.name,
    country: r.country ?? null,
    admin1: r.admin1 ?? null,
    latitude: r.latitude,
    longitude: r.longitude,
  }))
}

// Codes WMO (https://open-meteo.com/en/docs) regroupés en pictogrammes simples.
export function weatherIcon(code: number): string {
  if (code === 0) return '☀️'
  if (code <= 2) return '🌤️'
  if (code === 3) return '☁️'
  if (code <= 48) return '🌫️'
  if (code <= 57) return '🌦️'
  if (code <= 67) return '🌧️'
  if (code <= 77) return '🌨️'
  if (code <= 82) return '🌧️'
  if (code <= 86) return '🌨️'
  return '⛈️'
}
