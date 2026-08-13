import { useEffect, useState, type FormEvent } from 'react'
import { useAuth } from '../auth/AuthContext'
import { PERMISSION_LABELS, NO_PERMISSIONS, type UserPermissions } from '../auth/permissions'
import {
  AdminActionError,
  createAccount,
  deleteAccount,
  listAccounts,
  resetPassword,
  updateAccount,
  type AccountSummary,
  type AdminCredentials,
  type UpdateAccountInput,
} from '../api/authApi'
import { createRole, deleteRole, listRoles, updateRole, type Role, type SaveRoleInput } from '../api/rolesApi'
import { Modal } from '../components/Modal'
import './AdminPage.css'

const PERMISSION_KEYS = Object.keys(PERMISSION_LABELS) as (keyof UserPermissions)[]

export function AdminPage() {
  const { username, permissions, adminCredentials } = useAuth()
  const [accounts, setAccounts] = useState<AccountSummary[]>([])
  const [roles, setRoles] = useState<Role[]>([])

  async function refreshAccounts() {
    if (!adminCredentials) return
    setAccounts(await listAccounts(adminCredentials))
  }

  async function refreshRoles() {
    if (!adminCredentials) return
    setRoles(await listRoles(adminCredentials))
  }

  useEffect(() => {
    refreshAccounts()
    refreshRoles()
  }, [adminCredentials])

  if (!adminCredentials) return null

  return (
    <div className="admin-page">
      <RolesManager admin={adminCredentials} roles={roles} onChanged={refreshRoles} />
      <AccountsTable
        admin={adminCredentials}
        accounts={accounts}
        roles={roles}
        currentUsername={username}
        showCortexiaColumn={permissions.manageAccounts}
        onChanged={refreshAccounts}
      />
    </div>
  )
}

const EMPTY_ROLE_FORM: SaveRoleInput = { name: '', permissions: { ...NO_PERMISSIONS } }
const EMPTY_EDIT_FORM: UpdateAccountInput = { username: '', email: '', cortexiaUsername: '', cortexiaPassword: '', roleId: 0 }

function RolesManager({ admin, roles, onChanged }: { admin: AdminCredentials; roles: Role[]; onChanged: () => void }) {
  const [form, setForm] = useState<SaveRoleInput>({ ...EMPTY_ROLE_FORM })
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editForm, setEditForm] = useState<SaveRoleInput>({ ...EMPTY_ROLE_FORM })
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleCreate(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await createRole(admin, form)
      setForm({ ...EMPTY_ROLE_FORM })
      setShowCreateModal(false)
      onChanged()
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    } finally {
      setIsSubmitting(false)
    }
  }

  function startEdit(role: Role) {
    setEditingId(role.id)
    setEditForm({ name: role.name, permissions: { ...role.permissions } })
  }

  async function handleSaveEdit(id: number) {
    setError(null)
    try {
      await updateRole(admin, id, editForm)
      setEditingId(null)
      onChanged()
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    }
  }

  async function handleDelete(role: Role) {
    if (!window.confirm(`Supprimer le rôle "${role.name}" ?`)) return
    setError(null)
    try {
      await deleteRole(admin, role.id)
      onChanged()
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    }
  }

  return (
    <div className="admin-card">
      <div className="admin-card-header">
        <h3>Rôles</h3>
        <button type="button" onClick={() => setShowCreateModal(true)}>
          Créer un rôle
        </button>
      </div>
      {error && !showCreateModal && <p className="admin-error">{error}</p>}

      <div className="admin-table-wrapper">
        <table className="admin-table">
          <thead>
            <tr>
              <th>Nom</th>
              {PERMISSION_KEYS.map((key) => (
                <th key={key}>{PERMISSION_LABELS[key]}</th>
              ))}
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {roles.map((role) =>
              editingId === role.id ? (
                <tr key={role.id}>
                  <td>
                    <input value={editForm.name} onChange={(e) => setEditForm({ ...editForm, name: e.target.value })} />
                  </td>
                  {PERMISSION_KEYS.map((key) => (
                    <td key={key} className="admin-table-checkbox">
                      <input
                        type="checkbox"
                        checked={editForm.permissions[key]}
                        onChange={(e) =>
                          setEditForm({ ...editForm, permissions: { ...editForm.permissions, [key]: e.target.checked } })
                        }
                      />
                    </td>
                  ))}
                  <td className="admin-table-actions">
                    <button type="button" onClick={() => handleSaveEdit(role.id)}>
                      Enregistrer
                    </button>
                    <button type="button" onClick={() => setEditingId(null)}>
                      Annuler
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={role.id}>
                  <td>{role.name}</td>
                  {PERMISSION_KEYS.map((key) => (
                    <td key={key} className="admin-table-checkbox">
                      <input type="checkbox" checked={role.permissions[key]} disabled />
                    </td>
                  ))}
                  <td className="admin-table-actions">
                    <button type="button" onClick={() => startEdit(role)}>
                      Modifier
                    </button>
                    <button type="button" className="admin-danger" onClick={() => handleDelete(role)}>
                      Supprimer
                    </button>
                  </td>
                </tr>
              ),
            )}
          </tbody>
        </table>
      </div>

      {showCreateModal && (
        <Modal title="Créer un rôle" onClose={() => setShowCreateModal(false)}>
          <form className="admin-role-create" onSubmit={handleCreate}>
            <label>
              <span>Nouveau rôle</span>
              <input value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} required />
            </label>

            <fieldset className="admin-permissions">
              <legend>Droits</legend>
              {PERMISSION_KEYS.map((key) => (
                <label key={key}>
                  <input
                    type="checkbox"
                    checked={form.permissions[key]}
                    onChange={(e) =>
                      setForm({ ...form, permissions: { ...form.permissions, [key]: e.target.checked } })
                    }
                  />
                  {PERMISSION_LABELS[key]}
                </label>
              ))}
            </fieldset>

            {error && <p className="admin-error">{error}</p>}

            <button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Création…' : 'Créer le rôle'}
            </button>
          </form>
        </Modal>
      )}
    </div>
  )
}

function CreateAccountForm({
  admin,
  roles,
  onCreated,
}: {
  admin: AdminCredentials
  roles: Role[]
  onCreated: () => void
}) {
  const [form, setForm] = useState({ username: '', email: '', password: '', cortexiaUsername: '', cortexiaPassword: '' })
  const [roleId, setRoleId] = useState<number | ''>('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  // Seuls les rôles ayant le droit "Gestion Cortexia" ont besoin d'identifiants Cortexia distincts :
  // masqués pour les autres rôles (les valeurs restent en mémoire si on bascule temporairement sur
  // un tel rôle pour les renseigner avant de reposer le rôle définitif).
  const showCortexiaFields = roles.find((role) => role.id === roleId)?.permissions.manageCortexia ?? false

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    if (roleId === '') return
    setError(null)
    setIsSubmitting(true)

    try {
      await createAccount(admin, { ...form, roleId })
      setForm({ username: '', email: '', password: '', cortexiaUsername: '', cortexiaPassword: '' })
      setRoleId('')
      onCreated()
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <div className="admin-form-grid">
        <label>
          <span>Identifiant du site</span>
          <input
            value={form.username}
            onChange={(e) => setForm({ ...form, username: e.target.value })}
            autoComplete="off"
            required
          />
        </label>
        <label>
          <span>Adresse e-mail</span>
          <input
            type="email"
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            autoComplete="off"
            required
          />
        </label>
        <label>
          <span>Mot de passe du site</span>
          <input
            type="password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            autoComplete="new-password"
            required
          />
        </label>
        <label>
          <span>Rôle</span>
          <select value={roleId} onChange={(e) => setRoleId(Number(e.target.value))} required>
            <option value="" disabled>
              Choisir un rôle
            </option>
            {roles.map((role) => (
              <option key={role.id} value={role.id}>
                {role.name}
              </option>
            ))}
          </select>
        </label>
        {showCortexiaFields && (
          <>
            <label>
              <span>Identifiant Cortexia</span>
              <input
                value={form.cortexiaUsername}
                onChange={(e) => setForm({ ...form, cortexiaUsername: e.target.value })}
                autoComplete="off"
                required
              />
            </label>
            <label>
              <span>Mot de passe Cortexia</span>
              <input
                type="password"
                value={form.cortexiaPassword}
                onChange={(e) => setForm({ ...form, cortexiaPassword: e.target.value })}
                autoComplete="new-password"
                required
              />
            </label>
          </>
        )}
      </div>

      {error && <p className="admin-error">{error}</p>}

      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Création…' : 'Créer le compte'}
      </button>
    </form>
  )
}

function AccountsTable({
  admin,
  accounts,
  roles,
  currentUsername,
  showCortexiaColumn,
  onChanged,
}: {
  admin: AdminCredentials
  accounts: AccountSummary[]
  roles: Role[]
  currentUsername: string | null
  showCortexiaColumn: boolean
  onChanged: () => void
}) {
  const [error, setError] = useState<string | null>(null)
  const [showCreateModal, setShowCreateModal] = useState(false)
  const [editingUsername, setEditingUsername] = useState<string | null>(null)
  const [editForm, setEditForm] = useState<UpdateAccountInput>(EMPTY_EDIT_FORM)
  const [isSavingEdit, setIsSavingEdit] = useState(false)
  const [resetPasswordFor, setResetPasswordFor] = useState<AccountSummary | null>(null)

  function startEdit(account: AccountSummary) {
    setError(null)
    setEditingUsername(account.username)
    setEditForm({
      username: account.username,
      email: account.email,
      cortexiaUsername: account.cortexiaUsername,
      cortexiaPassword: '',
      roleId: account.roleId,
    })
  }

  async function handleSaveEdit(currentUsernameBeforeEdit: string) {
    setError(null)
    setIsSavingEdit(true)
    try {
      await updateAccount(admin, currentUsernameBeforeEdit, editForm)
      setEditingUsername(null)
      onChanged()
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    } finally {
      setIsSavingEdit(false)
    }
  }

  async function handleDelete(account: AccountSummary) {
    if (!window.confirm(`Supprimer le compte "${account.username}" ?`)) return
    setError(null)
    try {
      await deleteAccount(admin, account.username)
      onChanged()
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    }
  }

  return (
    <div className="admin-card">
      <div className="admin-card-header">
        <h3>Comptes existants</h3>
        <button type="button" onClick={() => setShowCreateModal(true)}>
          Créer un compte
        </button>
      </div>
      {error && <p className="admin-error">{error}</p>}

      <div className="admin-table-wrapper">
        <table className="admin-table">
          <thead>
            <tr>
              <th>Identifiant</th>
              <th>E-mail</th>
              {showCortexiaColumn && <th>Cortexia</th>}
              <th>Rôle</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {accounts.map((account) =>
              editingUsername === account.username ? (
                <tr key={account.username}>
                  <td>
                    <input
                      value={editForm.username}
                      disabled={account.username === currentUsername}
                      title={account.username === currentUsername ? 'Impossible de renommer votre propre compte connecté.' : undefined}
                      onChange={(e) => setEditForm({ ...editForm, username: e.target.value })}
                    />
                  </td>
                  <td>
                    <input
                      type="email"
                      value={editForm.email}
                      onChange={(e) => setEditForm({ ...editForm, email: e.target.value })}
                    />
                  </td>
                  {showCortexiaColumn && (
                    <td className="admin-table-edit-cell">
                      <input
                        placeholder="Identifiant Cortexia"
                        value={editForm.cortexiaUsername}
                        onChange={(e) => setEditForm({ ...editForm, cortexiaUsername: e.target.value })}
                      />
                      <input
                        type="password"
                        placeholder="Nouveau mot de passe Cortexia (laisser vide pour ne pas changer)"
                        value={editForm.cortexiaPassword}
                        onChange={(e) => setEditForm({ ...editForm, cortexiaPassword: e.target.value })}
                      />
                    </td>
                  )}
                  <td>
                    <select value={editForm.roleId} onChange={(e) => setEditForm({ ...editForm, roleId: Number(e.target.value) })}>
                      {roles.map((role) => (
                        <option key={role.id} value={role.id}>
                          {role.name}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td className="admin-table-actions">
                    <button type="button" onClick={() => handleSaveEdit(account.username)} disabled={isSavingEdit}>
                      {isSavingEdit ? 'Enregistrement…' : 'Enregistrer'}
                    </button>
                    <button type="button" onClick={() => setEditingUsername(null)}>
                      Annuler
                    </button>
                  </td>
                </tr>
              ) : (
                <tr key={account.username}>
                  <td>{account.username}</td>
                  <td>{account.email}</td>
                  {showCortexiaColumn && <td>{account.cortexiaUsername}</td>}
                  <td>{account.roleName}</td>
                  <td className="admin-table-actions">
                    <button type="button" onClick={() => startEdit(account)}>
                      Modifier
                    </button>
                    <button type="button" onClick={() => setResetPasswordFor(account)}>
                      Réinitialiser mdp
                    </button>
                    <button
                      type="button"
                      className="admin-danger"
                      disabled={account.username === currentUsername}
                      onClick={() => handleDelete(account)}
                    >
                      Supprimer
                    </button>
                  </td>
                </tr>
              ),
            )}
          </tbody>
        </table>
      </div>

      {resetPasswordFor && (
        <Modal title={`Réinitialiser le mot de passe de "${resetPasswordFor.username}"`} onClose={() => setResetPasswordFor(null)}>
          <ResetPasswordForm
            admin={admin}
            username={resetPasswordFor.username}
            onDone={() => setResetPasswordFor(null)}
          />
        </Modal>
      )}

      {showCreateModal && (
        <Modal title="Créer un compte" onClose={() => setShowCreateModal(false)}>
          <CreateAccountForm
            admin={admin}
            roles={roles}
            onCreated={() => {
              onChanged()
              setShowCreateModal(false)
            }}
          />
        </Modal>
      )}
    </div>
  )
}

function ResetPasswordForm({ admin, username, onDone }: { admin: AdminCredentials; username: string; onDone: () => void }) {
  const [newPassword, setNewPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)
    try {
      await resetPassword(admin, username, newPassword)
      onDone()
    } catch (err) {
      setError(err instanceof AdminActionError ? err.message : 'Erreur inattendue.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit}>
      <label>
        <span>Nouveau mot de passe</span>
        <input
          type="password"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
          autoComplete="new-password"
          required
        />
      </label>

      {error && <p className="admin-error">{error}</p>}

      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Enregistrement…' : 'Réinitialiser'}
      </button>
    </form>
  )
}
