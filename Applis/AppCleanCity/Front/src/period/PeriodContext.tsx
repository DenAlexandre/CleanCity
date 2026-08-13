import { createContext, useContext, useMemo, useState, type ReactNode } from 'react'

export interface Period {
  start: string
  end: string
}

interface PeriodContextValue {
  period: Period
  setPeriod: (period: Period) => void
}

// Par défaut : les 6 derniers mois jusqu'à aujourd'hui (heure locale).
function toLocalInput(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`
}

function defaultPeriod(): Period {
  const now = new Date()
  const sixMonthsAgo = new Date(now.getFullYear(), now.getMonth() - 6, now.getDate(), 0, 0)
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59)
  return { start: toLocalInput(sixMonthsAgo), end: toLocalInput(today) }
}

const PeriodContext = createContext<PeriodContextValue | undefined>(undefined)

export function PeriodProvider({ children }: { children: ReactNode }) {
  const [period, setPeriod] = useState<Period>(defaultPeriod)
  const value = useMemo(() => ({ period, setPeriod }), [period])
  return <PeriodContext.Provider value={value}>{children}</PeriodContext.Provider>
}

export function usePeriod(): PeriodContextValue {
  const context = useContext(PeriodContext)
  if (!context) {
    throw new Error('usePeriod doit être utilisé à l\'intérieur de <PeriodProvider>.')
  }
  return context
}
