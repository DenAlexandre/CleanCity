import './PlaceholderPage.css'

interface PlaceholderPageProps {
  title: string
  message?: string
}

export function PlaceholderPage({ title, message }: PlaceholderPageProps) {
  return (
    <div className="placeholder-page">
      <h2>{title}</h2>
      <p>{message ?? 'Le contenu de cette section sera ajouté prochainement.'}</p>
    </div>
  )
}
