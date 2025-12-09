import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import './Skills.css';

interface CharacterSkill {
  skillId: number;
  skillName: string;
  trainedSkillLevel: number;
  skillPointsInSkill: number;
  activeSkillLevel: number;
}

interface SkillGroup {
  groupId: number;
  groupName: string;
  skills: CharacterSkill[];
}

interface SkillsResponse {
  totalSp: number;
  unallocatedSp: number;
  skillGroups: SkillGroup[];
}

const Skills = () => {
  const [skills, setSkills] = useState<SkillsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [expandedGroups, setExpandedGroups] = useState<Set<number>>(new Set());
  const [characterName, setCharacterName] = useState<string>('');

  const navigate = useNavigate();
  const API_BASE = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5056';

  useEffect(() => {
    const authData = localStorage.getItem('eveAuth');
    if (!authData) {
      navigate('/');
      return;
    }

    const { characterId, accessToken, characterName: name } = JSON.parse(authData);
    setCharacterName(name);

    const fetchSkills = async () => {
      try {
        const response = await fetch(
          `${API_BASE}/api/characters/${characterId}/skills?accessToken=${encodeURIComponent(accessToken)}`
        );

        if (!response.ok) {
          throw new Error(`Failed to fetch skills: ${response.statusText}`);
        }

        const data = await response.json();
        setSkills(data);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    fetchSkills();
  }, [API_BASE, navigate]);

  const toggleGroup = (groupId: number) => {
    setExpandedGroups((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(groupId)) {
        newSet.delete(groupId);
      } else {
        newSet.add(groupId);
      }
      return newSet;
    });
  };

  const formatSP = (sp: number): string => {
    return sp.toLocaleString();
  };

  const renderSkillBar = (level: number) => {
    return (
      <div className="skill-bar">
        {[1, 2, 3, 4, 5].map((i) => (
          <div key={i} className={`skill-bar-segment ${i <= level ? 'filled' : ''}`} />
        ))}
      </div>
    );
  };

  if (loading) {
    return (
      <div className="skills-container">
        <div className="loading">Loading skills...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="skills-container">
        <div className="error">Error: {error}</div>
        <button onClick={() => navigate('/')}>Back to Auth</button>
      </div>
    );
  }

  if (!skills) {
    return null;
  }

  return (
    <div className="skills-container">
      <div className="skills-header">
        <h1>Character Skills</h1>
        <div className="character-info">
          <div className="character-name">{characterName}</div>
          <div className="sp-summary">
            <span className="sp-label">Total SP:</span>
            <span className="sp-value">{formatSP(skills.totalSp)}</span>
            <span className="sp-label">Unallocated:</span>
            <span className="sp-value">{formatSP(skills.unallocatedSp)}</span>
          </div>
        </div>
      </div>

      <div className="skills-content">
        {skills.skillGroups.map((group) => (
          <div key={group.groupId} className="skill-group">
            <div
              className="skill-group-header"
              onClick={() => toggleGroup(group.groupId)}
            >
              <span className={`expand-icon ${expandedGroups.has(group.groupId) ? 'expanded' : ''}`}>
                ▶
              </span>
              <span className="group-name">{group.groupName}</span>
              <span className="skill-count">({group.skills.length} skills)</span>
            </div>

            {expandedGroups.has(group.groupId) && (
              <div className="skill-list">
                {group.skills.map((skill) => (
                  <div key={skill.skillId} className="skill-item">
                    <div className="skill-info">
                      <div className="skill-name">{skill.skillName}</div>
                      <div className="skill-level">Level {skill.trainedSkillLevel}</div>
                    </div>
                    {renderSkillBar(skill.trainedSkillLevel)}
                    <div className="skill-sp">{formatSP(skill.skillPointsInSkill)} SP</div>
                  </div>
                ))}
              </div>
            )}
          </div>
        ))}
      </div>
    </div>
  );
};

export default Skills;
