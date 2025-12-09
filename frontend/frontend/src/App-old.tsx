import { useCallback, useEffect, useMemo, useState } from 'react'
import './App.css'

type AuthStart = { url: string; state: string }
type AuthExchange = {
  characterId: number
  characterName: string
  accessToken: string
  refreshToken: string
  expiresAtUtc: string
  profile: {
    characterId: number
    name: string
    corporationId: number
    allianceId?: number | null
    securityStatus?: number | null
    birthday?: string | null
  }
}

type SyncResponse = AuthExchange['profile']

const defaultApi = import.meta.env.VITE_API_URL || 'http://localhost:5056'

function App() {
  const [apiBase, setApiBase] = useState(defaultApi)
  const [authStart, setAuthStart] = useState<AuthStart | null>(null)
  const [authResult, setAuthResult] = useState<AuthExchange | null>(null)
  const [syncResult, setSyncResult] = useState<SyncResponse | null>(null)
  const [status, setStatus] = useState('Idle')
  const [error, setError] = useState<string | null>(null)

  const query = useMemo(() => new URLSearchParams(window.location.search), [])
  const code = query.get('code')
  const state = query.get('state')

  useEffect(() => {
    if (code && state && !authResult) {
      void exchange(code, state)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [code, state])

  const startLogin = useCallback(async () => {
    setStatus('Requesting login URL...')
    setError(null)
    try {
      const res = await fetch(`${apiBase}/api/auth/start`)
      if (!res.ok) throw new Error(`Start failed: ${res.status}`)
      const data = (await res.json()) as AuthStart
      setAuthStart(data)
      setStatus('Redirecting to EVE Online...')
      window.location.href = data.url
    } catch (err) {
      setError((err as Error).message)
      setStatus('Idle')
    }
  }, [apiBase])

  const exchange = useCallback(
    async (authCode: string, authState: string) => {
      setStatus('Exchanging code...')
      setError(null)
      try {
        const res = await fetch(`${apiBase}/api/auth/exchange`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ code: authCode, state: authState }),
        })
        if (!res.ok) throw new Error(`Exchange failed: ${res.status}`)
        const data = (await res.json()) as AuthExchange
        setAuthResult(data)
        setStatus('Authenticated')
        // clear query params so refresh is clean
        window.history.replaceState({}, document.title, window.location.pathname)
      } catch (err) {
        setError((err as Error).message)
        setStatus('Idle')
      }
    },
    [apiBase],
  )

  const syncCharacter = useCallback(async () => {
    if (!authResult) return
    setStatus('Syncing character...')
    setError(null)
    try {
      const res = await fetch(`${apiBase}/api/characters/${authResult.characterId}/sync`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ accessToken: authResult.accessToken }),
      })
      if (!res.ok) throw new Error(`Sync failed: ${res.status}`)
      const data = (await res.json()) as SyncResponse
      setSyncResult(data)
      setStatus('Synced')
    } catch (err) {
      setError((err as Error).message)
      setStatus('Idle')
    }
  }, [apiBase, authResult])

  return (
    <div className="page">
      <header>
        <div className="brand">EVE IPH – Auth Test</div>
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
          <button onClick={startLogin}>Start EVE SSO</button>
          {code && state && !authResult && (
            <button onClick={() => exchange(code, state)}>Retry Exchange</button>
          )}
          {authResult && (
            <button onClick={syncCharacter}>Sync Character Profile</button>
          )}
        </div>

        {error && <div className="error">{error}</div>}

        <div className="callout">
          <strong>Flow:</strong> Start → redirected to EVE → back to this page with
          <code>?code&state</code> → exchange → optional profile sync.
        </div>
      </section>

      <section className="panel">
        <h3>Auth Start</h3>
        <pre className="mono">{authStart ? JSON.stringify(authStart, null, 2) : '—'}</pre>
      </section>

      <section className="panel">
        <h3>Auth Result</h3>
        <pre className="mono">{authResult ? JSON.stringify(authResult, null, 2) : '—'}</pre>
      </section>

      <section className="panel">
        <h3>Character Sync</h3>
        <pre className="mono">{syncResult ? JSON.stringify(syncResult, null, 2) : '—'}</pre>
      </section>
    </div>
  )
}

export default App
