import { useState } from 'react';
import useSWR, { useSWRConfig } from 'swr';
import { api } from '../../core/api/apiClient';
import { LoadingSpinner } from '@/shared/components/LoadingSpinner';
import EmptyState from '@/shared/components/EmptyState';

interface GISIntegrationSettings {
  id?: string;
  portalBaseUrl: string;
  iworkApiBaseUrl: string;
  ssoEndpoint: string;
  maskedUsername?: string;
  departmentId?: number;
  departmentName?: string;
  isEnabled: boolean;
  syncIntervalMinutes: number;
  lastConnectionTestAt?: string;
  lastConnectionStatus?: string;
  lastConnectionError?: string;
  usePlaywrightAuth: boolean;
  playwrightTimeoutSeconds: number;
  maxRetryAttempts: number;
  updatedAt?: string;
  updatedBy?: string;
}

interface ConnectionTestResult {
  isSuccess: boolean;
  status: string;
  message?: string;
  errorMessage?: string;
  responseTimeMs: number;
  sessionToken?: string;
  tokenExpiresAt?: string;
  testedAt: string;
}

interface AuthStatus {
  isAuthenticated: boolean;
  status: string;
  authenticatedAt?: string;
  tokenExpiresAt?: string;
  tokenExpired: boolean;
  username?: string;
  departmentName?: string;
}

export default function GISConfigurationPage() {
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);
  const { mutate } = useSWRConfig();
  const [saving, setSaving] = useState(false);
  const [testing, setTesting] = useState(false);
  const [testResult, setTestResult] = useState<ConnectionTestResult | null>(null);
  const [showPassword, setShowPassword] = useState(false);

  const { data: settings, error, isLoading } = useSWR<GISIntegrationSettings>(
    ['gis-integration/settings'],
    () => api.get('/api/gis-integration/settings').then(res => res.data)
  );

  const { data: authStatus } = useSWR<AuthStatus>(
    ['gis-integration/status'],
    () => api.get('/api/gis-integration/status').then(res => res.data),
    { refreshInterval: 30000 } // Refresh every 30 seconds
  );

  const handleSave = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setSaving(true);
    
    try {
      const form = e.currentTarget;
      const formData = new FormData(form);
      
      const payload = {
        portalBaseUrl: formData.get('portalBaseUrl') as string,
        iworkApiBaseUrl: formData.get('iworkApiBaseUrl') as string,
        ssoEndpoint: formData.get('ssoEndpoint') as string,
        username: formData.get('username') as string,
        password: formData.get('password') as string,
        departmentId: formData.get('departmentId') ? parseInt(formData.get('departmentId') as string) : null,
        departmentName: formData.get('departmentName') as string,
        isEnabled: formData.get('isEnabled') === 'on',
        syncIntervalMinutes: parseInt(formData.get('syncIntervalMinutes') as string),
        usePlaywrightAuth: formData.get('usePlaywrightAuth') === 'on',
        playwrightTimeoutSeconds: parseInt(formData.get('playwrightTimeoutSeconds') as string),
        maxRetryAttempts: parseInt(formData.get('maxRetryAttempts') as string)
      };

      await api.put('/api/gis-integration/settings', payload);
      mutate(['gis-integration/settings']);
      setToast({ message: 'GIS integration settings saved successfully', type: 'success' });
    } catch (err) {
      setToast({ message: 'Failed to save GIS integration settings', type: 'error' });
    } finally {
      setSaving(false);
    }
  };

  const handleTestConnection = async () => {
    setTesting(true);
    setTestResult(null);
    
    try {
      const response = await api.post<ConnectionTestResult>('/api/gis-integration/test-connection', {});
      setTestResult(response.data);
      mutate(['gis-integration/status']);
      setToast({ 
        message: response.data.isSuccess ? 'Connection test successful' : 'Connection test failed', 
        type: response.data.isSuccess ? 'success' : 'error' 
      });
    } catch (err) {
      setToast({ message: 'Connection test failed', type: 'error' });
    } finally {
      setTesting(false);
    }
  };

  const handleAuthenticate = async () => {
    try {
      await api.post('/api/gis-integration/authenticate', {});
      mutate(['gis-integration/status']);
      setToast({ message: 'Authentication successful', type: 'success' });
    } catch (err) {
      setToast({ message: 'Authentication failed', type: 'error' });
    }
  };

  if (isLoading) return <LoadingSpinner />;

  const currentSettings = settings ?? {
    portalBaseUrl: 'https://gis.yfpo.com:9105',
    iworkApiBaseUrl: 'https://gis.yfpo.com:9120',
    ssoEndpoint: '/public/sp/sso/yfjwt5?enterpriseId=yf',
    isEnabled: false,
    syncIntervalMinutes: 60,
    usePlaywrightAuth: true,
    playwrightTimeoutSeconds: 30,
    maxRetryAttempts: 3
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">GIS Integration Configuration</h1>
          <p className="text-muted-foreground mt-1">
            Configure connection to GIS iWork attendance system
          </p>
        </div>
        
        {/* Connection Status Badge */}
        {authStatus && (
          <div className={`px-4 py-2 rounded-lg border ${
            authStatus.isAuthenticated 
              ? 'bg-green-500/10 border-green-500/20 text-green-400'
              : 'bg-red-500/10 border-red-500/20 text-red-400'
          }`}>
            <div className="flex items-center gap-2">
              <div className={`w-2 h-2 rounded-full ${
                authStatus.isAuthenticated ? 'bg-green-400' : 'bg-red-400'
              }`} />
              <span className="text-sm font-medium">
                {authStatus.isAuthenticated ? 'Connected' : 'Disconnected'}
              </span>
            </div>
          </div>
        )}
      </div>

      {/* Authentication Status Card */}
      {authStatus && (
        <div className="bg-card p-6 rounded-lg border">
          <h2 className="text-lg font-semibold mb-4">Authentication Status</h2>
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <div>
              <p className="text-sm text-muted-foreground">Status</p>
              <p className="text-sm font-medium">{authStatus.status}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Username</p>
              <p className="text-sm font-medium">{authStatus.username ?? 'N/A'}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Department</p>
              <p className="text-sm font-medium">{authStatus.departmentName ?? 'N/A'}</p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Token Expires</p>
              <p className="text-sm font-medium">
                {authStatus.tokenExpiresAt 
                  ? new Date(authStatus.tokenExpiresAt).toLocaleString()
                  : 'N/A'}
              </p>
              {authStatus.tokenExpired && (
                <p className="text-xs text-red-400 mt-1">Token expired</p>
              )}
            </div>
          </div>
          
          {!authStatus.isAuthenticated && (
            <div className="mt-4">
              <button
                onClick={handleAuthenticate}
                className="px-4 py-2 rounded-md bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
              >
                Authenticate Now
              </button>
            </div>
          )}
        </div>
      )}

      <form onSubmit={handleSave} className="space-y-8">
        {/* GIS Portal Configuration */}
        <section className="bg-card p-6 rounded-lg border space-y-4">
          <h2 className="text-lg font-semibold">GIS Portal Configuration</h2>
          
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="block text-sm font-medium mb-1">Portal Base URL</label>
              <input
                name="portalBaseUrl"
                type="url"
                defaultValue={currentSettings.portalBaseUrl}
                placeholder="https://gis.yfpo.com:9105"
                className="w-full px-3 py-2 rounded-md border bg-background text-foreground"
                required
              />
              <p className="text-xs text-muted-foreground mt-1">
                GIS SSO portal base URL
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">iWork API Base URL</label>
              <input
                name="iworkApiBaseUrl"
                type="url"
                defaultValue={currentSettings.iworkApiBaseUrl}
                placeholder="https://gis.yfpo.com:9120"
                className="w-full px-3 py-2 rounded-md border bg-background text-foreground"
                required
              />
              <p className="text-xs text-muted-foreground mt-1">
                GIS iWork API endpoint base URL
              </p>
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium mb-1">SSO Endpoint Path</label>
            <input
              name="ssoEndpoint"
              type="text"
              defaultValue={currentSettings.ssoEndpoint}
              placeholder="/public/sp/sso/yfjwt5?enterpriseId=yf"
              className="w-full px-3 py-2 rounded-md border bg-background text-foreground font-mono text-sm"
              required
            />
            <p className="text-xs text-muted-foreground mt-1">
              SSO login endpoint path (relative to portal base URL)
            </p>
          </div>
        </section>

        {/* Credentials */}
        <section className="bg-card p-6 rounded-lg border space-y-4">
          <h2 className="text-lg font-semibold">GIS Credentials</h2>
          
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="block text-sm font-medium mb-1">Username</label>
              <input
                name="username"
                type="text"
                placeholder={currentSettings.maskedUsername ?? 'Enter GIS username'}
                className="w-full px-3 py-2 rounded-md border bg-background text-foreground"
              />
              <p className="text-xs text-muted-foreground mt-1">
                {currentSettings.maskedUsername 
                  ? `Current: ${currentSettings.maskedUsername} (leave blank to keep)`
                  : 'GIS portal username'}
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">Password</label>
              <div className="relative">
                <input
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  placeholder="Enter GIS password"
                  className="w-full px-3 py-2 pr-10 rounded-md border bg-background text-foreground"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
                >
                  {showPassword ? '🙈' : '👁️'}
                </button>
              </div>
              <p className="text-xs text-muted-foreground mt-1">
                GIS portal password (encrypted at rest)
              </p>
            </div>
          </div>
        </section>

        {/* Department Scope */}
        <section className="bg-card p-6 rounded-lg border space-y-4">
          <h2 className="text-lg font-semibold">Department Scope</h2>
          
          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="block text-sm font-medium mb-1">Department ID</label>
              <input
                name="departmentId"
                type="number"
                defaultValue={currentSettings.departmentId ?? ''}
                placeholder="1752"
                className="w-full px-3 py-2 rounded-md border bg-background text-foreground"
              />
              <p className="text-xs text-muted-foreground mt-1">
                GIS department ID for data access scope
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">Department Name</label>
              <input
                name="departmentName"
                type="text"
                defaultValue={currentSettings.departmentName ?? ''}
                placeholder="Production"
                className="w-full px-3 py-2 rounded-md border bg-background text-foreground"
              />
              <p className="text-xs text-muted-foreground mt-1">
                Display name for the department
              </p>
            </div>
          </div>
        </section>

        {/* Sync Configuration */}
        <section className="bg-card p-6 rounded-lg border space-y-4">
          <h2 className="text-lg font-semibold">Synchronization Settings</h2>
          
          <div className="flex items-center gap-3">
            <input
              id="isEnabled"
              name="isEnabled"
              type="checkbox"
              defaultChecked={currentSettings.isEnabled}
              className="w-4 h-4 rounded"
            />
            <label htmlFor="isEnabled" className="text-sm font-medium">
              Enable GIS Integration
            </label>
          </div>
          <p className="text-xs text-muted-foreground">
            When disabled, no synchronization will occur
          </p>

          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="block text-sm font-medium mb-1">Sync Interval (minutes)</label>
              <input
                name="syncIntervalMinutes"
                type="number"
                min={15}
                max={1440}
                defaultValue={currentSettings.syncIntervalMinutes}
                className="w-32 px-3 py-2 rounded-md border bg-background text-foreground"
                required
              />
              <p className="text-xs text-muted-foreground mt-1">
                How often to sync attendance data (15-1440 minutes)
              </p>
            </div>
          </div>
        </section>

        {/* Playwright Configuration */}
        <section className="bg-card p-6 rounded-lg border space-y-4">
          <h2 className="text-lg font-semibold">Authentication Method</h2>
          
          <div className="flex items-center gap-3">
            <input
              id="usePlaywrightAuth"
              name="usePlaywrightAuth"
              type="checkbox"
              defaultChecked={currentSettings.usePlaywrightAuth}
              className="w-4 h-4 rounded"
            />
            <label htmlFor="usePlaywrightAuth" className="text-sm font-medium">
              Use Playwright Browser Automation
            </label>
          </div>
          <p className="text-xs text-muted-foreground">
            Required for GIS portal authentication (handles SSO and session management)
          </p>

          <div className="grid gap-4 md:grid-cols-2">
            <div>
              <label className="block text-sm font-medium mb-1">Browser Timeout (seconds)</label>
              <input
                name="playwrightTimeoutSeconds"
                type="number"
                min={10}
                max={120}
                defaultValue={currentSettings.playwrightTimeoutSeconds}
                className="w-32 px-3 py-2 rounded-md border bg-background text-foreground"
                required
              />
              <p className="text-xs text-muted-foreground mt-1">
                Maximum time to wait for browser operations
              </p>
            </div>

            <div>
              <label className="block text-sm font-medium mb-1">Max Retry Attempts</label>
              <input
                name="maxRetryAttempts"
                type="number"
                min={1}
                max={10}
                defaultValue={currentSettings.maxRetryAttempts}
                className="w-32 px-3 py-2 rounded-md border bg-background text-foreground"
                required
              />
              <p className="text-xs text-muted-foreground mt-1">
                Number of retry attempts for failed operations
              </p>
            </div>
          </div>
        </section>

        {/* Connection Test */}
        <section className="bg-card p-6 rounded-lg border space-y-4">
          <h2 className="text-lg font-semibold">Connection Test</h2>
          
          <div className="flex items-center gap-4">
            <button
              type="button"
              onClick={handleTestConnection}
              disabled={testing}
              className="px-6 py-2 rounded-md bg-secondary text-secondary-foreground hover:opacity-90 transition-opacity disabled:opacity-50"
            >
              {testing ? 'Testing...' : 'Test Connection'}
            </button>
            
            {currentSettings.lastConnectionTestAt && (
              <p className="text-xs text-muted-foreground">
                Last test: {new Date(currentSettings.lastConnectionTestAt).toLocaleString()}
                {currentSettings.lastConnectionStatus && (
                  <span className={`ml-2 ${
                    currentSettings.lastConnectionStatus === 'Connected' 
                      ? 'text-green-400' 
                      : 'text-red-400'
                  }`}>
                    ({currentSettings.lastConnectionStatus})
                  </span>
                )}
              </p>
            )}
          </div>

          {testResult && (
            <div className={`p-4 rounded-lg border ${
              testResult.isSuccess 
                ? 'bg-green-500/10 border-green-500/20'
                : 'bg-red-500/10 border-red-500/20'
            }`}>
              <p className={`text-sm font-medium ${
                testResult.isSuccess ? 'text-green-400' : 'text-red-400'
              }`}>
                {testResult.isSuccess ? '✓ Connection Successful' : '✗ Connection Failed'}
              </p>
              {testResult.message && (
                <p className="text-xs text-muted-foreground mt-1">{testResult.message}</p>
              )}
              {testResult.errorMessage && (
                <p className="text-xs text-red-400 mt-1">{testResult.errorMessage}</p>
              )}
              <p className="text-xs text-muted-foreground mt-2">
                Response time: {testResult.responseTimeMs.toFixed(0)}ms
              </p>
            </div>
          )}

          {currentSettings.lastConnectionError && (
            <div className="p-4 rounded-lg border bg-red-500/10 border-red-500/20">
              <p className="text-xs text-red-400">
                Last error: {currentSettings.lastConnectionError}
              </p>
            </div>
          )}
        </section>

        {/* Save button */}
        <div className="flex justify-end">
          <button
            type="submit"
            disabled={saving}
            className="px-6 py-2 rounded-md bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            {saving ? 'Saving...' : 'Save Configuration'}
          </button>
        </div>
      </form>

      {/* Toast */}
      {toast && (
        <div className={`fixed bottom-4 right-4 z-50 p-4 rounded-lg border shadow-lg ${
          toast.type === 'success' 
            ? 'bg-green-500/10 border-green-500/20 text-green-400'
            : 'bg-red-500/10 border-red-500/20 text-red-400'
        }`}>
          {toast.message}
          <button 
            onClick={() => setToast(null)} 
            className="ml-2 text-lg leading-none"
          >
            ×
          </button>
        </div>
      )}
    </div>
  );
}
