import './HistoryBarChart.css'

export interface HistoryBarPoint {
  key: string
  /** null = jour sans itinéraire : le slot occupe sa place dans la chronologie, sans barre. */
  value: number | null
  valueLabel: string
  dateLabel: string
  tooltip: string
  showDateLabel: boolean
}

const SCALE_MAX = 5
// Au-delà de ce nombre de barres (ex. période "Tous les itinéraires" sur plusieurs mois), l'espace
// cumulé des espacements fixes peut à lui seul consommer toute la largeur de la carte et réduire
// chaque barre à 0px : on resserre donc l'espacement à mesure qu'il y a plus de barres à afficher.
const BAR_GAP_PX = 4
const MANY_BARS_THRESHOLD = 60

export function HistoryBarChart({ points }: { points: HistoryBarPoint[] }) {
  if (points.length === 0) return null

  const gridLines = Array.from({ length: SCALE_MAX + 1 }, (_, i) => SCALE_MAX - i)
  const barGapPx = points.length > MANY_BARS_THRESHOLD ? 0 : BAR_GAP_PX

  return (
    <div className="history-chart">
      <span className="history-chart-legend">
        <span className="history-chart-legend-dot" /> CCI
      </span>

      <div className="history-chart-body">
        <div className="history-chart-axis">
          {gridLines.map((value) => (
            <span key={value}>{value}</span>
          ))}
        </div>

        <div className="history-chart-plot">
          <div className="history-chart-scroll">
            <div className="history-chart-gridlines">
              {gridLines.map((value) => (
                <div key={value} className="history-chart-gridline" />
              ))}
            </div>

            <div className="history-chart-bars" style={{ gap: `${barGapPx}px` }}>
              {points.map((point) => (
                <div key={point.key} className="history-chart-bar-wrapper" title={point.value !== null ? point.tooltip : undefined}>
                  {point.value !== null && (
                    <div className="history-chart-bar" style={{ height: `${Math.min(100, (point.value / SCALE_MAX) * 100)}%` }} />
                  )}
                  <span className="history-chart-bar-date">{point.showDateLabel ? point.dateLabel : ''}</span>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  )
}
