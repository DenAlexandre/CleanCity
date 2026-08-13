import { useEffect, useState } from 'react'
import { fetchWeather, weatherIcon, type WeatherData } from '../api/weatherApi'
import { fetchWeatherSettings } from '../api/settingsApi'
import './WeatherWidget.css'

export function WeatherWidget() {
  const [city, setCity] = useState<string | null>(null)
  const [weather, setWeather] = useState<WeatherData | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    fetchWeatherSettings()
      .then((settings) => {
        if (cancelled) return
        setCity(settings.city)
        return fetchWeather(settings.latitude, settings.longitude)
      })
      .then((data) => {
        if (!cancelled && data) setWeather(data)
      })
      .catch(() => {
        if (!cancelled) setError('Météo indisponible.')
      })

    return () => {
      cancelled = true
    }
  }, [])

  if (error) return <p className="weather-error">{error}</p>
  if (!weather) return null

  const [, ...nextDays] = weather.daily

  return (
    <div className="weather-widget">
      {city && <span className="weather-city">{city}</span>}
      <div className="weather-today">
        <span className="weather-icon">{weatherIcon(weather.currentWeatherCode)}</span>
        <span className="weather-temp">{Math.round(weather.currentTemperature)}°C</span>
      </div>
      {nextDays.map((day) => (
        <div key={day.date} className="weather-day">
          <span className="weather-day-label">{new Date(day.date).toLocaleDateString('fr-FR', { weekday: 'short' })}</span>
          <span className="weather-icon">{weatherIcon(day.weatherCode)}</span>
          <span className="weather-day-temps">
            {Math.round(day.temperatureMax)}° <span className="weather-day-temp-min">{Math.round(day.temperatureMin)}°</span>
          </span>
        </div>
      ))}
    </div>
  )
}
