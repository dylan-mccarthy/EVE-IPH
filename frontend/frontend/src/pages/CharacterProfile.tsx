import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import './CharacterProfile.css';

interface SkillsSummary {
  totalSp: number;
  totalSkills: number;
  unallocatedSp: number;
}

interface CorporationInfo {
  corporationId: number;
  corporationName: string;
  ticker: string | null;
  memberCount: number | null;
  allianceId: number | null;
  allianceName: string | null;
}

interface WalletInfo {
  balance: number;
}

interface CharacterDetails {
  characterId: number;
  characterName: string;
  gender: string;
  birthday: string;
  raceId: number;
  bloodlineId: number;
  ancestryId: number;
  description: string | null;
  securityStatus: number | null;
  corporation: CorporationInfo;
  wallet: WalletInfo | null;
  skills: SkillsSummary;
  scopes: string[];
}

const CharacterProfile = () => {
  const { characterId } = useParams<{ characterId: string }>();
  const [character, setCharacter] = useState<CharacterDetails | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const navigate = useNavigate();
  const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5056';

  useEffect(() => {
    if (!characterId) {
      navigate('/characters');
      return;
    }

    const fetchCharacterDetails = async () => {
      try {
        console.log('Fetching character details for:', characterId);
        const response = await fetch(`${API_BASE}/api/characters/${characterId}`);

        console.log('Character details response status:', response.status);

        if (response.status === 404) {
          throw new Error('Character not found');
        }

        if (response.status === 401) {
          console.error('Unauthorized - redirecting to login');
          localStorage.removeItem('eveAuth');
          navigate('/');
          return;
        }

        if (response.status === 400) {
          const errorData = await response.json();
          console.error('Bad request:', errorData);
          throw new Error(`Bad request: ${JSON.stringify(errorData)}`);
        }

        if (!response.ok) {
          const errorText = await response.text();
          console.error('Failed to fetch character details:', response.status, errorText);
          throw new Error(`Failed to fetch character details: ${response.statusText}`);
        }

        const data = await response.json();
        console.log('Character details loaded successfully');
        setCharacter(data);
      } catch (err) {
        console.error('Error fetching character details:', err);
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    fetchCharacterDetails();
  }, [characterId, API_BASE, navigate]);

  const formatISK = (value: number): string => {
    if (value >= 1_000_000_000) {
      return `${(value / 1_000_000_000).toFixed(2)}B ISK`;
    } else if (value >= 1_000_000) {
      return `${(value / 1_000_000).toFixed(2)}M ISK`;
    } else if (value >= 1_000) {
      return `${(value / 1_000).toFixed(2)}K ISK`;
    }
    return `${value.toFixed(2)} ISK`;
  };

  const formatSP = (sp: number): string => {
    return sp.toLocaleString();
  };

  const formatDate = (dateStr: string): string => {
    return new Date(dateStr).toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'long',
      day: 'numeric'
    });
  };

  if (loading) {
    return (
      <div className="character-profile-container">
        <div className="loading">Loading character details...</div>
      </div>
    );
  }

  if (error || !character) {
    return (
      <div className="character-profile-container">
        <div className="error-box">
          <h2>Error</h2>
          <p>{error || 'Character not found'}</p>
          <button onClick={() => navigate('/characters')}>Back to Characters</button>
        </div>
      </div>
    );
  }

  return (
    <div className="character-profile-container">
      <div className="profile-content">
        <div className="profile-main">
          <div className="character-card-large">
            <div className="character-portrait">
              <img
                src={`https://images.evetech.net/characters/${character.characterId}/portrait?size=256`}
                alt={character.characterName}
              />
            </div>
            <div className="character-basic-info">
              <h1>{character.characterName}</h1>
              <div className="character-meta">
                <span className="gender">{character.gender}</span>
                <span className="separator">•</span>
                <span className="birthday">Born {formatDate(character.birthday)}</span>
              </div>
              {character.description && (
                <p className="character-description">{character.description}</p>
              )}
            </div>
          </div>

          <div className="info-grid">
            <div className="info-card">
              <h3>Corporation</h3>
              <div className="corp-info">
                <img
                  src={`https://images.evetech.net/corporations/${character.corporation.corporationId}/logo?size=64`}
                  alt={character.corporation.corporationName}
                  className="corp-logo"
                />
                <div>
                  <div className="corp-name">{character.corporation.corporationName}</div>
                  {character.corporation.ticker && (
                    <div className="corp-ticker">[{character.corporation.ticker}]</div>
                  )}
                  {character.corporation.memberCount && (
                    <div className="corp-members">{character.corporation.memberCount.toLocaleString()} members</div>
                  )}
                </div>
              </div>
              {character.corporation.allianceName && (
                <div className="alliance-info">
                  <span className="label">Alliance:</span>
                  <span className="value">{character.corporation.allianceName}</span>
                </div>
              )}
            </div>

            <div className="info-card">
              <h3>Skills</h3>
              <div className="stat-row">
                <span className="label">Total SP:</span>
                <span className="value">{formatSP(character.skills.totalSp)}</span>
              </div>
              <div className="stat-row">
                <span className="label">Known Skills:</span>
                <span className="value">{character.skills.totalSkills}</span>
              </div>
              {character.skills.unallocatedSp > 0 && (
                <div className="stat-row">
                  <span className="label">Unallocated SP:</span>
                  <span className="value highlight">{formatSP(character.skills.unallocatedSp)}</span>
                </div>
              )}
            </div>

            {character.wallet && (
              <div className="info-card">
                <h3>Wallet</h3>
                <div className="wallet-balance">{formatISK(character.wallet.balance)}</div>
              </div>
            )}

            {character.securityStatus !== null && (
              <div className="info-card">
                <h3>Security Status</h3>
                <div className={`security-status ${character.securityStatus >= 0 ? 'positive' : 'negative'}`}>
                  {character.securityStatus.toFixed(2)}
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="profile-sidebar">
          <div className="info-card">
            <h3>Authorized Scopes</h3>
            <div className="scopes-list">
              {character.scopes.map((scope, index) => (
                <div key={`${scope}-${index}`} className="scope-item">
                  {scope.replace('esi-', '').replace('.v1', '').replace(/_/g, ' ')}
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CharacterProfile;
