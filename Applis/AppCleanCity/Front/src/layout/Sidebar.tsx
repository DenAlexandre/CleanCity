import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { navItems } from './navItems'
import './Sidebar.css'

interface SidebarProps {
  expanded: boolean
  onToggle: () => void
}

export function Sidebar({ expanded, onToggle }: SidebarProps) {
  const { permissions } = useAuth()
  const visibleItems = navItems.filter((item) => permissions[item.permission])

  return (
    <>
      {expanded && <div className="sidebar-backdrop" onClick={onToggle} />}
      <aside className={`sidebar ${expanded ? 'sidebar-expanded' : ''}`}>
        <nav className="sidebar-nav">
          {visibleItems.map((item) => (
            <NavLink
              key={item.key}
              to={item.path}
              end={item.path === '/'}
              className={({ isActive }) => `sidebar-item ${isActive ? 'sidebar-item-active' : ''}`}
              title={item.label}
            >
              <span className="sidebar-icon">{item.icon}</span>
              {expanded && <span className="sidebar-label">{item.label}</span>}
            </NavLink>
          ))}
        </nav>

        <button
          type="button"
          className="sidebar-toggle"
          onClick={onToggle}
          aria-label={expanded ? 'Réduire le menu' : 'Déplier le menu'}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
            {expanded ? <path d="M15 6l-6 6 6 6" /> : <path d="M9 6l6 6-6 6" />}
          </svg>
        </button>
      </aside>
    </>
  )
}
