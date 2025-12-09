import { useEffect, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import './Navigation.css';

interface NavigationProps {
  onLogout?: () => void;
}

const Navigation = ({ onLogout }: NavigationProps) => {
  const navigate = useNavigate();
  const location = useLocation();
  const [characterName, setCharacterName] = useState<string | null>(null);
  const [characterId, setCharacterId] = useState<number | null>(null);

  useEffect(() => {
    const authData = localStorage.getItem('eveAuth');
    if (authData) {
      try {
        const { characterName: name, characterId: id } = JSON.parse(authData);
        setCharacterName(name);
        setCharacterId(id);
      } catch (err) {
        console.error('Failed to parse auth data:', err);
      }
    } else {
      setCharacterName(null);
      setCharacterId(null);
    }
  }, [location.pathname]);

  const handleLogout = () => {
    localStorage.removeItem('eveAuth');
    setCharacterName(null);
    setCharacterId(null);
    if (onLogout) {
      onLogout();
    }
    navigate('/');
  };

  const handleLogin = () => {
    navigate('/');
  };

  const isActive = (path: string) => {
    return location.pathname === path ? 'active' : '';
  };

  return (
    <nav className="navigation">
      <div className="nav-container">
        <div className="nav-brand" onClick={() => navigate('/')}>
          <img 
            src="https://images.evetech.net/alliances/1/logo?size=64" 
            alt="EVE IPH" 
            className="nav-logo"
          />
          <span className="nav-title">EVE IPH</span>
        </div>

        {characterName ? (
          <>
            <div className="nav-links">
              <button 
                className={`nav-link ${isActive('/characters')}`}
                onClick={() => navigate('/characters')}
              >
                Characters
              </button>
              <button 
                className={`nav-link ${isActive('/skills')}`}
                onClick={() => navigate('/skills')}
              >
                Skills
              </button>
              <button 
                className={`nav-link ${isActive(`/characters/${characterId}`)}`}
                onClick={() => navigate(`/characters/${characterId}`)}
              >
                Profile
              </button>
              <button 
                className={`nav-link ${isActive('/assets')}`}
                onClick={() => navigate('/assets')}
              >
                Assets
              </button>
              <button 
                className={`nav-link ${isActive('/industry')}`}
                onClick={() => navigate('/industry')}
              >
                Industry
              </button>
              <button 
                className={`nav-link ${isActive('/blueprints')}`}
                onClick={() => navigate('/blueprints')}
              >
                Blueprints
              </button>
              <button 
                className={`nav-link ${isActive('/market-orders')}`}
                onClick={() => navigate('/market-orders')}
              >
                Market
              </button>
              <button 
                className={`nav-link ${isActive('/market-data')}`}
                onClick={() => navigate('/market-data')}
              >
                Market Data
              </button>
              <button 
                className={`nav-link ${isActive('/wallet')}`}
                onClick={() => navigate('/wallet')}
              >
                Wallet
              </button>
            </div>

            <div className="nav-user">
              <div className="user-info">
                {characterId && (
                  <img 
                    src={`https://images.evetech.net/characters/${characterId}/portrait?size=32`}
                    alt={characterName}
                    className="user-avatar"
                  />
                )}
                <span className="user-name">{characterName}</span>
              </div>
              <button className="btn-logout" onClick={handleLogout}>
                Logout
              </button>
            </div>
          </>
        ) : (
          <div className="nav-auth">
            <button className="btn-login" onClick={handleLogin}>
              Login with EVE
            </button>
          </div>
        )}
      </div>
    </nav>
  );
};

export default Navigation;
