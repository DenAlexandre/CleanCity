import './PieChart.css'

export interface PieChartSlice {
  label: string
  value: number
}

const SIZE = 280
const RADIUS = 110
const STROKE_WIDTH = 56
const CIRCUMFERENCE = 2 * Math.PI * RADIUS

function colorForIndex(index: number, total: number): string {
  const hue = (index * 360) / Math.max(total, 1)
  return `hsl(${hue}, 65%, 50%)`
}

export function PieChart({ slices }: { slices: PieChartSlice[] }) {
  const total = slices.reduce((sum, slice) => sum + slice.value, 0)

  if (total === 0) {
    return <p className="pie-chart-empty">Aucune donnée sur la période sélectionnée.</p>
  }

  let cumulative = 0

  return (
    <div className="pie-chart">
      <svg viewBox={`0 0 ${SIZE} ${SIZE}`} className="pie-chart-svg">
        <g transform={`rotate(-90 ${SIZE / 2} ${SIZE / 2})`}>
          {slices.map((slice, index) => {
            const fraction = slice.value / total
            const segmentLength = fraction * CIRCUMFERENCE
            const dashArray = `${segmentLength} ${CIRCUMFERENCE - segmentLength}`
            const dashOffset = -cumulative
            cumulative += segmentLength
            return (
              <circle
                key={slice.label}
                cx={SIZE / 2}
                cy={SIZE / 2}
                r={RADIUS}
                fill="none"
                stroke={colorForIndex(index, slices.length)}
                strokeWidth={STROKE_WIDTH}
                strokeDasharray={dashArray}
                strokeDashoffset={dashOffset}
              >
                <title>{`${slice.label} : ${slice.value} (${(fraction * 100).toFixed(1)}%)`}</title>
              </circle>
            )
          })}
        </g>
      </svg>

      <ul className="pie-chart-legend">
        {slices.map((slice, index) => (
          <li key={slice.label}>
            <span className="pie-chart-legend-swatch" style={{ background: colorForIndex(index, slices.length) }} />
            <span className="pie-chart-legend-label">{slice.label}</span>
            <span className="pie-chart-legend-value">{((slice.value / total) * 100).toFixed(1)}%</span>
          </li>
        ))}
      </ul>
    </div>
  )
}
