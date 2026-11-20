import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from './core/auth/useAuth';
import { ProtectedRoute } from './core/auth/ProtectedRoute';
import Layout from './shared/components/Layout';
import LoginPage from './features/auth/LoginPage';
import RegisterPage from './features/auth/RegisterPage';
import DashboardPage from './features/dashboard/DashboardPage';
import EmployeeListPage from './features/employees/EmployeeListPage';
import EmployeeDetailPage from './features/employees/EmployeeDetailPage';
import ClockInPage from './features/attendance/ClockInPage';
import AttendancePage from './features/attendance/AttendancePage';
import ExceptionsPage from './features/exceptions/ExceptionsPage';
import WarningsPage from './features/warnings/WarningsPage';
import EscalationsPage from './features/escalations/EscalationsPage';
import ReportsPage from './features/reports/ReportsPage';
import AuditLogPage from './features/audit/AuditLogPage';
import UserManagementPage from './features/admin/UserManagementPage';
import DepartmentSettingsPage from './features/admin/DepartmentSettingsPage';
import SystemSettingsPage from './features/admin/SystemSettingsPage';
import GISConfigurationPage from './features/admin/GISConfigurationPage';
import ManagerDashboardPage from './features/manager-dashboard/ManagerDashboardPage';
import HRCommandCentrePage from './features/hr-command-centre/HRCommandCentrePage';
import ExecutiveDashboardPage from './features/executive-dashboard/ExecutiveDashboardPage';

const HomePage = () => {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <Navigate to="/dashboard" replace /> : <Navigate to="/login" replace />;
};

const NotFoundPage = () => (
  <div className="flex items-center justify-center min-h-screen bg-background">
    <div className="text-center space-y-4">
      <h1 className="text-6xl font-bold text-primary">404</h1>
      <p className="text-xl text-muted-foreground">Page not found</p>
      <a href="/dashboard" className="inline-block px-6 py-2 rounded-md bg-primary text-primary-foreground hover:opacity-90 transition-opacity">
        Go to Dashboard
      </a>
    </div>
  </div>
);

export default function App() {
  return (
    <Routes>
      {/* Public routes */}
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />

      {/* Redirect root */}
      <Route path="/" element={<HomePage />} />

      {/* Authenticated routes wrapped in Layout */}
      <Route
        element={
          <ProtectedRoute requiredRole={[] as any}>
            <Layout />
          </ProtectedRoute>
        }
      >
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/employees" element={<EmployeeListPage />} />
        <Route path="/employees/:id" element={<EmployeeDetailPage />} />
        <Route path="/clock-in" element={<ClockInPage />} />
        <Route path="/attendance" element={<AttendancePage />} />
        <Route path="/exceptions" element={<ExceptionsPage />} />
        <Route path="/warnings" element={<WarningsPage />} />
        <Route path="/escalations" element={<EscalationsPage />} />
        <Route path="/reports" element={<ReportsPage />} />
        <Route path="/audit" element={<AuditLogPage />} />
        <Route path="/manager-dashboard" element={<ManagerDashboardPage />} />
        <Route path="/hr-command-centre" element={<HRCommandCentrePage />} />
        <Route path="/executive-dashboard" element={<ExecutiveDashboardPage />} />

        {/* Admin-only routes */}
        <Route
          path="/admin/users"
          element={
            <ProtectedRoute requiredRole={['SuperAdmin', 'Admin'] as any}>
              <UserManagementPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/departments"
          element={
            <ProtectedRoute requiredRole={['SuperAdmin', 'Admin'] as any}>
              <DepartmentSettingsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/settings"
          element={
            <ProtectedRoute requiredRole={['SuperAdmin'] as any}>
              <SystemSettingsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/gis-integration"
          element={
            <ProtectedRoute requiredRole={['SuperAdmin', 'Admin'] as any}>
              <GISConfigurationPage />
            </ProtectedRoute>
          }
        />
      </Route>

      {/* 404 fallback */}
      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}
