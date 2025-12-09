import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import './CharacterSelect.css';

interface Character {
  characterId: number;
  characterName: string;
  corporationId: number;
  corporationName: string | null;
  isDefault: boolean;
  hasValidToken: boolean;
}

const CharacterSelect = () => {
  const [characters, setCharacters] = useState<Character[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const navigate = useNavigate();
  const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5056';

  useEffect(() => {
    const fetchCharacters = async () => {
      try {
        const response = await fetch(`${API_BASE}/api/characters`);

        if (!response.ok) {
          throw new Error(`Failed to fetch characters: ${response.statusText}`);
        }

        const data = await response.json();
        setCharacters(data.characters || []);

        // If only one character, auto-select it
        if (data.characters.length === 1) {
          selectCharacter(data.characters[0]);
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    fetchCharacters();
  }, [API_BASE]);

  const selectCharacter = (character: Character) => {
    // Save to localStorage
    localStorage.setItem('eveAuth', JSON.stringify({
      characterId: character.characterId,
      characterName: character.characterName,
      corporationName: character.corporationName
    }));

    // Navigate to skills page
    navigate('/skills');
  };

  const addNewCharacter = () => {
    // Navigate to login to add another character
    navigate('/');
  };

  if (loading) {
    return (
      <div className="character-select-container">
        <div className="loading">Loading characters...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="character-select-container">
        <div className="error-box">
          <h2>Error Loading Characters</h2>
          <p>{error}</p>
          <button onClick={() => navigate('/')}>Back to Login</button>
        </div>
      </div>
    );
  }

  if (characters.length === 0) {
    return (
      <div className="character-select-container">
        <div className="no-characters">
          <h2>No Characters Found</h2>
          <p>You need to authenticate with EVE Online first.</p>
          <button onClick={() => navigate('/')}>Login with EVE SSO</button>
        </div>
      </div>
    );
  }

  return (
    <div className="character-select-container">
      <div className="character-select-header">
        <h1>Select Character</h1>
        <p className="help-text">Choose a character to view their profile or skills</p>
        <button className="add-character-btn" onClick={addNewCharacter}>
          + Add Another Character
        </button>
      </div>

      <div className="character-grid">
        {characters.map((character) => (
          <div
            key={character.characterId}
            className={`character-card ${character.isDefault ? 'default' : ''} ${!character.hasValidToken ? 'invalid-token' : ''}`}
          >
            <div className="character-avatar">
              <img
                src={`https://images.evetech.net/characters/${character.characterId}/portrait?size=128`}
                alt={character.characterName}
              />
              {character.isDefault && <div className="default-badge">Default</div>}
            </div>
            <div className="character-info">
              <h3>{character.characterName}</h3>
              <p className="corporation">{character.corporationName || 'Unknown Corporation'}</p>
              {!character.hasValidToken && (
                <p className="token-warning">⚠️ Token expired - please re-authenticate</p>
              )}
            </div>
            <div className="character-actions">
              <button 
                className="view-profile-btn"
                onClick={() => navigate(`/characters/${character.characterId}`)}
              >
                View Profile
              </button>
              {character.hasValidToken && (
                <button 
                  className="select-character-btn"
                  onClick={() => selectCharacter(character)}
                >
                  View Skills
                </button>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default CharacterSelect;
