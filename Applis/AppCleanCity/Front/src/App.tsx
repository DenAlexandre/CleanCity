import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuthProvider } from './auth/AuthContext'
import { RequireAuth } from './auth/RequireAuth'
import { RequirePermission } from './auth/RequirePermission'
import { LoginPage } from './pages/LoginPage'
import { MapPage } from './pages/MapPage'
import { AdminPage } from './pages/AdminPage'
import { AlertesPage } from './pages/AlertesPage'
import { AccueilPage } from './pages/AccueilPage'
import { SystemePage } from './pages/SystemePage'
import { MeasuresListPage } from './pages/MeasuresListPage'
import { ParametresPage } from './pages/ParametresPage'
import { DashboardLayout } from './layout/DashboardLayout'

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          <Route
            path="/"
            element={
              <RequireAuth>
                <DashboardLayout />
              </RequireAuth>
            }
          >
            <Route
              index
              element={
                <RequirePermission permission="viewMesures">
                  <AccueilPage />
                </RequirePermission>
              }
            />
            <Route
              path="mesures"
              element={
                <RequirePermission permission="viewMesures">
                  <MapPage />
                </RequirePermission>
              }
            />
            <Route
              path="liste-mesures"
              element={
                <RequirePermission permission="viewListeMesures">
                  <MeasuresListPage />
                </RequirePermission>
              }
            />
            <Route
              path="alertes"
              element={
                <RequirePermission permission="viewAlertes">
                  <AlertesPage />
                </RequirePermission>
              }
            />
            <Route
              path="parametres"
              element={
                <RequirePermission permission="viewParametres">
                  <ParametresPage />
                </RequirePermission>
              }
            />
            <Route
              path="systeme"
              element={
                <RequirePermission permission="viewSysteme">
                  <SystemePage />
                </RequirePermission>
              }
            />
            <Route
              path="administration"
              element={
                <RequirePermission permission="manageAccounts">
                  <AdminPage />
                </RequirePermission>
              }
            />
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
