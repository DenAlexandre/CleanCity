import { useState } from 'react'
import { Outlet, useLocation } from 'react-router-dom'
import { Header } from './Header'
import { Sidebar } from './Sidebar'
import { SubHeader } from './SubHeader'
import { navItems } from './navItems'
import { PeriodProvider } from '../period/PeriodContext'
import backgroundImage from '../assets/fond.jpg'
import './DashboardLayout.css'

export function DashboardLayout() {
  const [sidebarExpanded, setSidebarExpanded] = useState(false)
  const location = useLocation()
  const activeItem = navItems.find((item) =>
    item.path === '/' ? location.pathname === '/' : location.pathname.startsWith(item.path),
  )

  return (
    <PeriodProvider>
      <div className="dashboard-layout">
        <Header onMenuClick={() => setSidebarExpanded((v) => !v)} />
        <div className="dashboard-body">
          <Sidebar expanded={sidebarExpanded} onToggle={() => setSidebarExpanded((v) => !v)} />
          <div className="dashboard-content">
            <SubHeader title={activeItem?.label ?? ''} />
            <main className="dashboard-main" style={{ backgroundImage: `url(${backgroundImage})` }}>
              <Outlet />
            </main>
          </div>
        </div>
      </div>
    </PeriodProvider>
  )
}
