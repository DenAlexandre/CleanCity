import { useEffect, useMemo, useRef, useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { DomEvent, geoJSON as leafletGeoJson } from 'leaflet'
import { MapContainer, TileLayer, GeoJSON, CircleMarker, Circle, Tooltip, Pane, useMap, useMapEvents } from 'react-leaflet'
import { fetchEdgeScoresGeoJson, fetchItineraryEdgesGeoJson, fetchLocalStreetNames, fetchStreetGeoJson } from '../api/geoApi'
import { listPointsOfInterest, type PointOfInterest } from '../api/pointsOfInterestApi'
import { fetchDetectionDisplaySettings, fetchPointOfInterestSettings, type DetectionDisplaySettings } from '../api/settingsApi'
import { fetchMeasurementPoints, type MeasurementPoint } from '../api/measurementsApi'
import { TILE_SERVER_URL } from '../api/config'
import { usePeriod } from '../period/PeriodContext'
import './MapPage.css'

function filterByScore(collection: GeoJSON.FeatureCollection | null, min: number, max: number): GeoJSON.FeatureCollection | null {
  if (!collection) return null
  return {
    type: 'FeatureCollection',
    features: collection.features.filter((feature) => {
      const cci = feature.properties?.cci
      return typeof cci === 'number' && cci >= min && cci <= max
    }),
  }
}

const PALAISEAU_CENTER: [number, number] = [48.7159, 2.2465]

// Valeur de repli tant que le réglage (page Paramètres) n'est pas encore chargé.
const DEFAULT_POINT_OF_INTEREST_RADIUS_METERS = 500

type Tab = 'map' | 'details'

interface DetectionCluster {
  key: string
  latitude: number
  longitude: number
  totalQuantity: number
  dominantTypeCode: number
  dominantTypeName: string
  dominantStreet: string | null
  breakdown: { typeCode: number; typeName: string; quantity: number }[]
}

// Couleur stable par type de déchet, indépendante des types présents dans le cluster courant
// (contrairement à un dégradé calculé sur l'index d'une liste, qui changerait selon le filtrage).
function colorForType(typeCode: number): string {
  const hue = (typeCode * 137.508) % 360
  return `hsl(${hue}, 65%, 48%)`
}

function radiusForQuantity(totalQuantity: number): number {
  return Math.min(24, Math.max(6, 4 + 3 * Math.log2(totalQuantity + 1)))
}

// Regroupe les points sur une grille en coordonnées pixels (à la projection Leaflet du zoom courant) :
// la taille des cases est donc fixe en pixels, ce qui fait grossir/rétrécir les clusters au zoom,
// exactement comme un cluster de marqueurs classique, sans dépendance supplémentaire.
function clusterPoints(points: MeasurementPoint[], project: (lat: number, lng: number) => { x: number; y: number }, gridSizePx: number): DetectionCluster[] {
  const buckets = new Map<
    string,
    {
      sumLat: number
      sumLng: number
      count: number
      byType: Map<number, { typeName: string; quantity: number }>
      byStreet: Map<string, number>
    }
  >()

  for (const point of points) {
    const px = project(point.latitude, point.longitude)
    const key = `${Math.round(px.x / gridSizePx)}_${Math.round(px.y / gridSizePx)}`
    let bucket = buckets.get(key)
    if (!bucket) {
      bucket = { sumLat: 0, sumLng: 0, count: 0, byType: new Map(), byStreet: new Map() }
      buckets.set(key, bucket)
    }
    bucket.sumLat += point.latitude
    bucket.sumLng += point.longitude
    bucket.count += 1
    const existing = bucket.byType.get(point.typeCode)
    if (existing) {
      existing.quantity += point.quantity
    } else {
      bucket.byType.set(point.typeCode, { typeName: point.typeName, quantity: point.quantity })
    }
    if (point.street) {
      bucket.byStreet.set(point.street, (bucket.byStreet.get(point.street) ?? 0) + point.quantity)
    }
  }

  return [...buckets.entries()].map(([key, bucket]) => {
    const breakdown = [...bucket.byType.entries()]
      .map(([typeCode, value]) => ({ typeCode, typeName: value.typeName, quantity: value.quantity }))
      .sort((a, b) => b.quantity - a.quantity)
    const dominant = breakdown[0]
    const dominantStreet = [...bucket.byStreet.entries()].sort((a, b) => b[1] - a[1])[0]?.[0] ?? null
    return {
      key,
      latitude: bucket.sumLat / bucket.count,
      longitude: bucket.sumLng / bucket.count,
      totalQuantity: breakdown.reduce((sum, item) => sum + item.quantity, 0),
      dominantTypeCode: dominant.typeCode,
      dominantTypeName: dominant.typeName,
      dominantStreet,
      breakdown,
    }
  })
}

const DETAILS_MAP_MAX_ZOOM = 21

// Taille de la grille (en pixels projetés) qui décroît avec le zoom : les bulles se scindent
// progressivement à mesure qu'on zoome.
function gridSizeForZoom(zoom: number): number {
  return Math.max(3, 60 - (zoom - 14) * 8)
}

// Un point = un objet détecté (un type sur un snapshot) : au zoom maximum, chaque objet est
// affiché individuellement, sans le moindre regroupement.
function pointsToSingletonClusters(points: MeasurementPoint[]): DetectionCluster[] {
  return points.map((point, index) => ({
    key: `pt-${index}`,
    latitude: point.latitude,
    longitude: point.longitude,
    totalQuantity: point.quantity,
    dominantTypeCode: point.typeCode,
    dominantTypeName: point.typeName,
    dominantStreet: point.street,
    breakdown: [{ typeCode: point.typeCode, typeName: point.typeName, quantity: point.quantity }],
  }))
}

function DetectionClusterLayer({ points }: { points: MeasurementPoint[] }) {
  const map = useMap()
  const [zoom, setZoom] = useState(map.getZoom())

  useMapEvents({
    zoomend: (e) => setZoom(e.target.getZoom()),
  })

  const clusters = useMemo(() => {
    if (zoom >= DETAILS_MAP_MAX_ZOOM) {
      return pointsToSingletonClusters(points)
    }
    const project = (lat: number, lng: number) => map.project([lat, lng], zoom)
    return clusterPoints(points, project, gridSizeForZoom(zoom))
  }, [points, map, zoom])

  return (
    <>
      {clusters.map((cluster) => (
        <CircleMarker
          key={cluster.key}
          center={[cluster.latitude, cluster.longitude]}
          radius={radiusForQuantity(cluster.totalQuantity)}
          color="#ffffff"
          weight={1.5}
          fillColor={colorForType(cluster.dominantTypeCode)}
          fillOpacity={0.8}
        >
          <Tooltip direction="top" offset={[0, -6]}>
            <strong>{cluster.dominantStreet ?? cluster.dominantTypeName}</strong>
            {cluster.breakdown.length > 1 ? (
              <>
                <div>Total : {cluster.totalQuantity}</div>
                <ul className="detection-cluster-breakdown">
                  {cluster.breakdown.slice(0, 6).map((item) => (
                    <li key={item.typeCode}>
                      {item.typeName} : {item.quantity}
                    </li>
                  ))}
                </ul>
              </>
            ) : (
              <div>
                {cluster.dominantTypeName} : {cluster.totalQuantity}
              </div>
            )}
          </Tooltip>
        </CircleMarker>
      ))}
    </>
  )
}

// Les deux onglets restent montés une fois visités (voir `visitedTabs`) pour ne jamais recharger
// les tuiles déjà en cache ; Leaflet ne détecte pas seul qu'un conteneur display:none reprend une
// taille valide, donc on force un recalcul à chaque fois que l'onglet redevient actif.
function InvalidateSizeOnShow({ active }: { active: boolean }) {
  const map = useMap()

  useEffect(() => {
    if (active) map.invalidateSize()
  }, [active, map])

  return null
}

// Recadre la vue sur les nouvelles données à chaque changement de période : sans ça, la carte
// garde le même centre/zoom fixe et un rafraîchissement peut sembler ne rien faire si les
// nouvelles données tombent hors du cadre actuel.
function FitBoundsToData({ collection, suppressFirst }: { collection: GeoJSON.FeatureCollection | null; suppressFirst?: boolean }) {
  const map = useMap()
  const firstFitDoneRef = useRef(false)

  useEffect(() => {
    if (!collection || collection.features.length === 0) return

    // Arrivée depuis un lien avec une rue déjà ciblée (ex: Top 5 rues de l'Accueil) : le premier
    // chargement des itinéraires ne doit pas écraser ce zoom avec un recadrage sur toutes les
    // routes. On laisse en revanche les recadrages suivants (ex: changement de période) normaux.
    if (suppressFirst && !firstFitDoneRef.current) {
      firstFitDoneRef.current = true
      return
    }
    firstFitDoneRef.current = true

    const bounds = leafletGeoJson(collection).getBounds()
    if (bounds.isValid()) {
      map.flyToBounds(bounds, { padding: [32, 32], maxZoom: 17 })
    }
  }, [collection, map, suppressFirst])

  return null
}

// Recherche d'une rue par son nom (autocomplétion) : la sélection recadre la carte sur sa
// géométrie complète. Rendu à l'intérieur du MapContainer pour accéder à useMap(), donc on
// désactive la propagation des clics/molette vers la carte pour pouvoir cliquer/taper dans le champ.
function StreetSearchControl({ inputId, initialStreet }: { inputId: string; initialStreet?: string }) {
  const map = useMap()
  const containerRef = useRef<HTMLDivElement>(null)
  const [streetNames, setStreetNames] = useState<string[]>([])
  const [query, setQuery] = useState(initialStreet ?? '')
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const container = containerRef.current
    if (!container) return
    DomEvent.disableClickPropagation(container)
    DomEvent.disableScrollPropagation(container)
  }, [])

  useEffect(() => {
    fetchLocalStreetNames()
      .then(setStreetNames)
      .catch(() => setStreetNames([]))
  }, [])

  async function zoomToStreet(name: string) {
    try {
      const collection = await fetchStreetGeoJson(name)
      const bounds = leafletGeoJson(collection).getBounds()
      if (bounds.isValid()) {
        map.flyToBounds(bounds, { padding: [48, 48], maxZoom: 18 })
      }
    } catch {
      setError('Impossible de localiser cette rue.')
    }
  }

  // Arrivée via un lien externe (ex: Top 5 des rues de l'Accueil) : zoome directement dessus, sans
  // attendre que l'utilisateur retape la rue dans le champ.
  useEffect(() => {
    if (initialStreet) zoomToStreet(initialStreet)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  function handleChange(value: string) {
    setQuery(value)
    setError(null)
    // La sélection d'une suggestion depuis la liste native déclenche un évènement "input" dans la
    // plupart des navigateurs (donc onChange se déclenche déjà ici), mais ce comportement n'est pas
    // garanti partout : onKeyDown (Entrée) et onBlur ci-dessous servent de filet de sécurité.
    if (streetNames.includes(value)) zoomToStreet(value)
  }

  function handleClear() {
    setQuery('')
    setError(null)
  }

  return (
    <div ref={containerRef} className="map-street-search">
      <div className="map-street-search-input-wrapper">
        <input
          list={inputId}
          value={query}
          onChange={(e) => handleChange(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === 'Enter' && streetNames.includes(query)) zoomToStreet(query)
          }}
          onBlur={(e) => {
            if (streetNames.includes(e.target.value)) zoomToStreet(e.target.value)
          }}
          placeholder="Rechercher une rue…"
        />
        {query && (
          <button type="button" className="map-street-search-clear" onClick={handleClear} aria-label="Effacer">
            ✕
          </button>
        )}
      </div>
      <datalist id={inputId}>
        {streetNames.map((name) => (
          <option key={name} value={name} />
        ))}
      </datalist>
      {error && <span className="map-street-search-error">{error}</span>}
    </div>
  )
}

interface MapViewState {
  center: [number, number]
  zoom: number
}

// Les deux onglets utilisent deux instances Leaflet distinctes (pour ne jamais recharger les
// tuiles déjà en cache lors d'un changement d'onglet) ; ce composant synchronise leur centre/zoom
// via un état partagé, afin que les deux cartes montrent toujours la même zone.
function SyncMapView({ viewState, onViewChange }: { viewState: MapViewState; onViewChange: (next: MapViewState) => void }) {
  const map = useMap()

  useEffect(() => {
    const current = map.getCenter()
    if (current.lat !== viewState.center[0] || current.lng !== viewState.center[1] || map.getZoom() !== viewState.zoom) {
      map.setView(viewState.center, viewState.zoom, { animate: false })
    }
  }, [viewState, map])

  useMapEvents({
    moveend: () => {
      const center = map.getCenter()
      const zoom = map.getZoom()
      if (center.lat === viewState.center[0] && center.lng === viewState.center[1] && zoom === viewState.zoom) return
      onViewChange({ center: [center.lat, center.lng], zoom })
    },
  })

  return null
}

export function MapPage() {
  const { period } = usePeriod()
  const [searchParams] = useSearchParams()
  // Permet à d'autres pages (ex: Top 5 des rues sales de l'Accueil) de lier directement vers un
  // onglet précis avec une rue déjà sélectionnée via ?tab=...&street=...
  const initialTab: Tab = searchParams.get('tab') === 'details' ? 'details' : 'map'
  const initialStreet = searchParams.get('street') ?? undefined
  const [tab, setTab] = useState<Tab>(initialTab)
  // Un onglet visité reste monté (juste masqué en CSS) pour ne jamais redemander à Leaflet de
  // recharger les tuiles déjà récupérées ; celui jamais ouvert n'est en revanche pas créé pour ne
  // pas déclencher de chargement de tuiles inutile au premier affichage de la page.
  const [visitedTabs, setVisitedTabs] = useState<Set<Tab>>(() => new Set<Tab>([initialTab]))

  function selectTab(next: Tab) {
    setTab(next)
    setVisitedTabs((previous) => (previous.has(next) ? previous : new Set(previous).add(next)))
  }

  // Partagé entre les deux instances Leaflet (une par onglet) via SyncMapView, pour garder la même
  // zone/zoom quel que soit l'onglet actif.
  const [mapView, setMapView] = useState<MapViewState>({ center: PALAISEAU_CENTER, zoom: 14 })

  const [routesCollection, setRoutesCollection] = useState<GeoJSON.FeatureCollection | null>(null)
  const [edgeScores, setEdgeScores] = useState<GeoJSON.FeatureCollection | null>(null)
  const [displaySettings, setDisplaySettings] = useState<DetectionDisplaySettings | null>(null)
  const [pointsOfInterest, setPointsOfInterest] = useState<PointOfInterest[]>([])
  const [poiRadiusMeters, setPoiRadiusMeters] = useState(DEFAULT_POINT_OF_INTEREST_RADIUS_METERS)
  const [measurementPoints, setMeasurementPoints] = useState<MeasurementPoint[]>([])
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const [showRoutes, setShowRoutes] = useState(true)
  const [showPointsOfInterest, setShowPointsOfInterest] = useState(true)
  const [showAverageDetection, setShowAverageDetection] = useState(false)
  const [showPositiveDetection, setShowPositiveDetection] = useState(false)
  const [hiddenTypeCodes, setHiddenTypeCodes] = useState<Set<number>>(new Set())

  function toggleTypeVisibility(typeCode: number) {
    setHiddenTypeCodes((previous) => {
      const next = new Set(previous)
      if (next.has(typeCode)) {
        next.delete(typeCode)
      } else {
        next.add(typeCode)
      }
      return next
    })
  }

  useEffect(() => {
    fetchDetectionDisplaySettings()
      .then(setDisplaySettings)
      .catch(() => setDisplaySettings(null))
  }, [])

  useEffect(() => {
    fetchPointOfInterestSettings()
      .then((settings) => setPoiRadiusMeters(settings.radiusMeters))
      .catch(() => setPoiRadiusMeters(DEFAULT_POINT_OF_INTEREST_RADIUS_METERS))
  }, [])

  const hasVisitedMap = visitedTabs.has('map')
  const hasVisitedDetails = visitedTabs.has('details')

  useEffect(() => {
    // Dépend de `hasVisitedMap` (stable dès la première visite) plutôt que de `tab`, pour ne pas
    // refaire cette requête (et donc le recadrage de FitBoundsToData, qui écraserait le zoom
    // synchronisé entre les deux onglets) à chaque simple retour sur cet onglet.
    if (!hasVisitedMap) return

    let cancelled = false
    setIsLoading(true)
    setError(null)
    // Vide la couche avant de recharger : sur un gros volume (ex. "Tous les itinéraires"), la mise
    // à jour directe des anciens tracés en nouveaux laissait parfois le rendu Leaflet en retard
    // d'un cran (le bleu affiché correspondait à la période précédente). Passer par un état vide
    // force un vrai retrait de la couche avant que la nouvelle ne soit dessinée.
    setRoutesCollection(null)
    setEdgeScores(null)

    // "Itinéraires" = tronçons réellement parcourus sur la période sélectionnée, pas tout le réseau routier.
    fetchItineraryEdgesGeoJson(period.start, period.end)
      .then((geojson) => {
        if (!cancelled) setRoutesCollection(geojson)
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Erreur de chargement de la carte.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    // Note (Cci) par tronçon sur la période, pour les cases Détection positive/moyenne.
    fetchEdgeScoresGeoJson(period.start, period.end)
      .then((geojson) => {
        if (!cancelled) setEdgeScores(geojson)
      })
      .catch(() => {
        if (!cancelled) setEdgeScores(null)
      })

    listPointsOfInterest()
      .then((points) => {
        if (!cancelled) setPointsOfInterest(points)
      })
      .catch(() => {
        if (!cancelled) setPointsOfInterest([])
      })

    return () => {
      cancelled = true
    }
  }, [hasVisitedMap, period])

  useEffect(() => {
    if (!hasVisitedDetails) return

    let cancelled = false
    setIsLoading(true)
    setError(null)

    fetchMeasurementPoints(period.start, period.end)
      .then((points) => {
        if (!cancelled) setMeasurementPoints(points)
      })
      .catch((err) => {
        if (!cancelled) setError(err instanceof Error ? err.message : 'Erreur de chargement de la carte.')
      })
      .finally(() => {
        if (!cancelled) setIsLoading(false)
      })

    return () => {
      cancelled = true
    }
  }, [hasVisitedDetails, period])

  const detectionTypeLegend = useMemo(() => {
    const totals = new Map<number, { typeName: string; quantity: number }>()
    for (const point of measurementPoints) {
      const existing = totals.get(point.typeCode)
      if (existing) {
        existing.quantity += point.quantity
      } else {
        totals.set(point.typeCode, { typeName: point.typeName, quantity: point.quantity })
      }
    }
    return [...totals.entries()]
      .map(([typeCode, value]) => ({ typeCode, ...value }))
      .sort((a, b) => b.quantity - a.quantity)
  }, [measurementPoints])

  // Le filtre "sans rue" est déjà appliqué côté serveur (réglage Paramètres) ; seul le filtre par
  // type (cases à cocher) reste géré ici.
  const visibleMeasurementPoints = useMemo(
    () => measurementPoints.filter((point) => !hiddenTypeCodes.has(point.typeCode)),
    [measurementPoints, hiddenTypeCodes],
  )

  const positiveCollection = useMemo(
    () => (displaySettings ? filterByScore(edgeScores, displaySettings.positiveMin, displaySettings.positiveMax) : null),
    [edgeScores, displaySettings],
  )
  const averageCollection = useMemo(
    () => (displaySettings ? filterByScore(edgeScores, displaySettings.averageMin, displaySettings.averageMax) : null),
    [edgeScores, displaySettings],
  )

  return (
    <div className="map-page">
      <div className="map-page-tabs">
        <button className={tab === 'map' ? 'active' : ''} onClick={() => selectTab('map')}>
          Cartographie
        </button>
        <button className={tab === 'details' ? 'active' : ''} onClick={() => selectTab('details')}>
          Détails
        </button>
      </div>

      <div className="map-page-body">
        {visitedTabs.has('map') && (
          <div className="map-page-map-wrapper" style={{ display: tab === 'map' ? undefined : 'none' }}>
            <MapContainer key="map-tab" center={mapView.center} zoom={mapView.zoom} maxZoom={DETAILS_MAP_MAX_ZOOM} className="map-page-map">
              <TileLayer
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                url={TILE_SERVER_URL}
                maxZoom={DETAILS_MAP_MAX_ZOOM}
                maxNativeZoom={19}
                updateWhenZooming={false}
                updateWhenIdle
              />
              <InvalidateSizeOnShow active={tab === 'map'} />
              <SyncMapView viewState={mapView} onViewChange={setMapView} />
              <StreetSearchControl inputId="map-street-suggestions-map" initialStreet={initialStreet} />
              {showRoutes && routesCollection && (
                <GeoJSON key={`routes-${period.start}-${period.end}`} data={routesCollection} style={{ color: '#0055a2', weight: 2 }} />
              )}
              <FitBoundsToData collection={routesCollection} suppressFirst={Boolean(initialStreet)} />
              {/* Pane dédié avec un zIndex fixe : les détections restent au-dessus des itinéraires
                  quelle que soit l'ordre dans lequel les cases sont décochées/recochées. */}
              <Pane name="detectionPane" style={{ zIndex: 410 }}>
                {showPositiveDetection && positiveCollection && positiveCollection.features.length > 0 && displaySettings && (
                  <GeoJSON
                    key={`positive-${period.start}-${period.end}-${displaySettings.positiveColor}`}
                    data={positiveCollection}
                    style={{ color: displaySettings.positiveColor, weight: 4 }}
                  />
                )}
                {showAverageDetection && averageCollection && averageCollection.features.length > 0 && displaySettings && (
                  <GeoJSON
                    key={`average-${period.start}-${period.end}-${displaySettings.averageColor}`}
                    data={averageCollection}
                    style={{ color: displaySettings.averageColor, weight: 4 }}
                  />
                )}
              </Pane>
              {showPointsOfInterest &&
                pointsOfInterest.map((poi) => (
                  <Circle
                    key={`${poi.id}-radius`}
                    center={[poi.latitude, poi.longitude]}
                    radius={poiRadiusMeters}
                    color="#1e8f5f"
                    weight={1}
                    fillColor="#1e8f5f"
                    fillOpacity={0.08}
                    interactive={false}
                  />
                ))}
              {showPointsOfInterest &&
                pointsOfInterest.map((poi) => (
                  <CircleMarker
                    key={poi.id}
                    center={[poi.latitude, poi.longitude]}
                    radius={6}
                    color="#1e8f5f"
                    fillColor="#1e8f5f"
                    fillOpacity={0.85}
                  >
                    <Tooltip>
                      <strong>{poi.name}</strong>
                      <br />
                      {poi.category}
                      {poi.description && (
                        <>
                          <br />
                          {poi.description}
                        </>
                      )}
                    </Tooltip>
                  </CircleMarker>
                ))}
            </MapContainer>

            <div className="map-legend">
              <label>
                <input type="checkbox" checked={showAverageDetection} onChange={(e) => setShowAverageDetection(e.target.checked)} />
                Détection moyenne
              </label>
              <label>
                <input type="checkbox" checked={showPositiveDetection} onChange={(e) => setShowPositiveDetection(e.target.checked)} />
                Détection positive
              </label>
              <label>
                <input
                  type="checkbox"
                  checked={showPointsOfInterest}
                  onChange={(e) => setShowPointsOfInterest(e.target.checked)}
                />
                Centres d'intérêt
              </label>
              <label>
                <input type="checkbox" checked={showRoutes} onChange={(e) => setShowRoutes(e.target.checked)} />
                Itinéraires
              </label>
            </div>

            {isLoading && <div className="map-status">Chargement de la cartographie…</div>}
            {error && <div className="map-status map-status-error">{error}</div>}
          </div>
        )}
        {visitedTabs.has('details') && (
          <div className="map-page-map-wrapper" style={{ display: tab === 'details' ? undefined : 'none' }}>
            <MapContainer key="details-tab" center={mapView.center} zoom={mapView.zoom} maxZoom={DETAILS_MAP_MAX_ZOOM} className="map-page-map">
              <TileLayer
                attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                url={TILE_SERVER_URL}
                maxZoom={DETAILS_MAP_MAX_ZOOM}
                maxNativeZoom={19}
                updateWhenZooming={false}
                updateWhenIdle
              />
              <InvalidateSizeOnShow active={tab === 'details'} />
              <SyncMapView viewState={mapView} onViewChange={setMapView} />
              <StreetSearchControl inputId="map-street-suggestions-details" />
              <DetectionClusterLayer points={visibleMeasurementPoints} />
            </MapContainer>

            {detectionTypeLegend.length > 0 && (
              <div className="detection-type-legend">
                {detectionTypeLegend.map((type) => (
                  <label key={type.typeCode} className="detection-type-legend-item">
                    <input
                      type="checkbox"
                      checked={!hiddenTypeCodes.has(type.typeCode)}
                      onChange={() => toggleTypeVisibility(type.typeCode)}
                    />
                    <span className="detection-type-legend-swatch" style={{ background: colorForType(type.typeCode) }} />
                    {type.typeName}
                  </label>
                ))}
              </div>
            )}

            {isLoading && <div className="map-status">Chargement de la cartographie…</div>}
            {error && <div className="map-status map-status-error">{error}</div>}
            {!isLoading && !error && measurementPoints.length === 0 && (
              <div className="map-status">Aucune détection sur la période sélectionnée.</div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
