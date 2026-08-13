import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { login, LoginError } from '../api/authApi'
import palaiseauLogo from '../assets/palaiseau-logo.png'
import semeruLogo from '../assets/semeru-logo.png'
import './LoginPage.css'

export function LoginPage() {
  const [username, setUsername] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const auth = useAuth()
  const navigate = useNavigate()

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      const token = await login(username, password)
      auth.login(username, password, token.accessToken, token.tokenType, token.permissions)
      navigate('/', { replace: true })
    } catch (err) {
      setError(err instanceof LoginError ? err.message : 'Une erreur inattendue est survenue.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="login-page">
      <form className="login-card" onSubmit={handleSubmit}>
        <div className="login-logos">
          <img src={palaiseauLogo} alt="Palaiseau" className="login-logo" />
          <span className="login-logo-separator" />
          <img src={semeruLogo} alt="Semeru" className="login-logo" />
        </div>

        <h1 className="login-title">Observatoire de propreté urbaine</h1>
        <p className="login-subtitle">Connectez-vous pour accéder à votre espace</p>

        <label className="login-field">
          <span>Utilisateur</span>
          <input
            type="text"
            autoComplete="username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            required
            autoFocus
          />
        </label>

        <label className="login-field">
          <span>Mot de passe</span>
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </label>

        {error && <p className="login-error">{error}</p>}

        <button type="submit" className="login-submit" disabled={isSubmitting}>
          {isSubmitting ? 'Connexion…' : 'Se connecter'}
        </button>
      </form>
    </div>
  )
}
