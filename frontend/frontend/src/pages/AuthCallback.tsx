import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import './AuthCallback.css';

type AuthStart = { url: string; state: string };
type AuthExchange = {
  characterId: number;
  characterName: string;
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  profile: {
    characterId: number;
    name: string;
    corporationId: number;
    allianceId?: number | null;
    securityStatus?: number | null;
    birthday?: string | null;
  };
};

const defaultApi = import.meta.env.VITE_API_URL || 'http://localhost:5056';

function AuthCallback() {
  const [apiBase, setApiBase] = useState(defaultApi);
  const [authStart, setAuthStart] = useState<AuthStart | null>(null);
  const [status, setStatus] = useState('Idle');
  const [error, setError] = useState<string | null>(null);

  const navigate = useNavigate();
  const query = useMemo(() => new URLSearchParams(window.location.search), []);
  const code = query.get('code');
  const state = query.get('state');

  const exchange = useCallback(
    async (authCode: string, authState: string) => {
      setStatus('Exchanging code...');
      setError(null);
      try {
        const res = await fetch(`${apiBase}/api/auth/exchange`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ code: authCode, state: authState }),
        });
        
        if (!res.ok) {
          let errorMsg = `Exchange failed (${res.status})`;
          try {
            const errorData = await res.json();
            if (errorData.message) {
              errorMsg = errorData.message;
              
              // If invalid state, clear query params to allow restart
              if (errorData.message.includes('invalid or expired') || errorData.message.includes('invalid_state')) {
                window.history.replaceState({}, document.title, window.location.pathname);
                errorMsg += ' Please click "Start EVE SSO Login" to try again.';
              }
            }
          } catch {
            errorMsg = await res.text() || errorMsg;
          }
          throw new Error(errorMsg);
        }
        
        const data = (await res.json()) as AuthExchange;
        
        console.log('Auth exchange successful:', {
          characterId: data.characterId,
          characterName: data.characterName,
          hasAccessToken: !!data.accessToken,
          hasRefreshToken: !!data.refreshToken,
          expiresAt: data.expiresAtUtc,
        });
        
        // Store auth data in localStorage
        // The backend manages tokens in the database, so we primarily need characterId
        // But we store tokens for reference and to help detect when re-auth is needed
        localStorage.setItem('eveAuth', JSON.stringify({
          characterId: data.characterId,
          characterName: data.characterName,
          accessToken: data.accessToken,
          refreshToken: data.refreshToken,
          expiresAt: data.expiresAtUtc,
        }));

        setStatus('Authenticated - Redirecting...');
        
        // Clear query params before redirect
        window.history.replaceState({}, document.title, window.location.pathname);
        
        // Redirect to character select page
        setTimeout(() => {
          navigate('/characters');
        }, 500);
      } catch (err) {
        setError((err as Error).message);
        setStatus('Error');
      }
    },
    [apiBase, navigate]
  );

  useEffect(() => {
    if (code && state) {
      void exchange(code, state);
    }
  }, [code, state, exchange]);

  const startLogin = useCallback(async () => {
    setStatus('Requesting login URL...');
    setError(null);
    try {
      const res = await fetch(`${apiBase}/api/auth/start`);
      if (!res.ok) throw new Error(`Start failed: ${res.status}`);
      const data = (await res.json()) as AuthStart;
      setAuthStart(data);
      setStatus('Redirecting to EVE Online...');
      window.location.href = data.url;
    } catch (err) {
      setError((err as Error).message);
      setStatus('Idle');
    }
  }, [apiBase]);

  return (
    <div className="page">
      <header>
        <div className="brand">EVE IPH – Authentication</div>
        <div className="status">Status: {status}</div>
      </header>

      <section className="panel">
        <div className="field">
          <label htmlFor="api">API base</label>
          <input
            id="api"
            value={apiBase}
            onChange={(e) => setApiBase(e.target.value)}
            placeholder="http://localhost:5056"
          />
          <small>Must match backend host/port; dev default is 5056.</small>
        </div>

        <div className="actions">
          <button onClick={startLogin}>Start EVE SSO Login</button>
          {code && state && !error?.includes('invalid') && (
            <button onClick={() => exchange(code, state)}>Retry Exchange</button>
          )}
        </div>

        {error && (
          <div className="error">
            <strong>Error:</strong> {error}
          </div>
        )}

        <div className="callout">
          <strong>Flow:</strong> Start → redirected to EVE → back with code → exchange → redirect to Skills page
        </div>
      </section>

      {authStart && (
        <section className="panel">
          <h3>Auth Start</h3>
          <pre className="mono">{JSON.stringify(authStart, null, 2)}</pre>
        </section>
      )}
    </div>
  );
}

export default AuthCallback;
