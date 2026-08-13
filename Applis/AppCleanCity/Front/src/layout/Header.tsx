import { useState } from 'react'
import { useAuth } from '../auth/AuthContext'
import palaiseauLogo from '../assets/palaiseau-logo.png'
import semeruLogo from '../assets/semeru-logo.png'
import cleanCityLogo from '../assets/_LOGO_CLEAN-CITY-SEMERU.png'
import './Header.css'

interface HeaderProps {
  onMenuClick?: () => void
}

export function Header({ onMenuClick }: HeaderProps) {
  const { username, logout } = useAuth()
  const [menuOpen, setMenuOpen] = useState(false)

  return (
    <header className="app-header">
      <button type="button" className="app-header-menu-toggle" onClick={onMenuClick} aria-label="Ouvrir le menu">
        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
          <path d="M3 6h18M3 12h18M3 18h18" />
        </svg>
      </button>

      <div className="app-header-brand">
        <img src={palaiseauLogo} alt="Palaiseau" className="app-header-logo" />
        <img src={semeruLogo} alt="Semeru" className="app-header-logo" />
      </div>

      <div className="app-header-title">
        <img src={cleanCityLogo} alt="Clean City" className="app-header-badge" />
        <span>Observatoire de propreté urbaine</span>
      </div>

      <div className="app-header-user">
        <div className="app-header-user-menu">
          <button type="button" className="app-header-user-button" onClick={() => setMenuOpen((v) => !v)}>
            <span>{username}</span>
            <span className="app-header-avatar" aria-hidden="true">
              {username?.charAt(0).toUpperCase()}
            </span>
          </button>

          {menuOpen && (
            <div className="app-header-user-dropdown">
              <button type="button" onClick={logout}>
                Se déconnecter
              </button>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
