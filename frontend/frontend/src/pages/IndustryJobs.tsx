import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api, { type IndustryJob, type TypeInfo } from '../utils/api';
import './IndustryJobs.css';

const IndustryJobs = () => {
  const [jobs, setJobs] = useState<IndustryJob[]>([]);
  const [typeInfoCache, setTypeInfoCache] = useState<Record<number, TypeInfo>>({});
  const [activityNames, setActivityNames] = useState<Record<number, string>>({});
  const [locationNames, setLocationNames] = useState<Record<number, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [includeCompleted, setIncludeCompleted] = useState(true);
  const [filterStatus, setFilterStatus] = useState<string>('all');
  const navigate = useNavigate();

  const getCharacterId = (): number | null => {
    const authStr = localStorage.getItem('eveAuth');
    if (!authStr) return null;
    try {
      const auth = JSON.parse(authStr);
      return auth.characterId;
    } catch {
      return null;
    }
  };

  useEffect(() => {
    const characterId = getCharacterId();
    if (!characterId) {
      navigate('/characters');
      return;
    }

    const fetchJobs = async () => {
      try {
        setLoading(true);
        const data = await api.industry.getJobs(characterId, includeCompleted);
        setJobs(data);

        // Fetch type info for all unique type IDs (blueprints and products)
        const uniqueTypeIds = [...new Set([
          ...data.map(j => j.blueprintTypeId),
          ...data.map(j => j.productTypeId).filter(id => id !== undefined) as number[]
        ])];
        if (uniqueTypeIds.length > 0) {
          const typeInfos = await api.sde.getTypeInfoBatch(uniqueTypeIds);
          setTypeInfoCache(typeInfos);
        }

        // Fetch activity names for all unique activity IDs
        const uniqueActivityIds = [...new Set(data.map(j => j.activityId))];
        const activityNamesMap: Record<number, string> = {};
        for (const activityId of uniqueActivityIds) {
          const response = await api.sde.getActivityName(activityId);
          activityNamesMap[activityId] = response.name;
        }
        setActivityNames(activityNamesMap);

        // Fetch location names for all unique location IDs
        const uniqueLocationIds = [...new Set(data.map(j => j.locationId))];
        const locationNamesMap: Record<number, string> = {};
        for (const locationId of uniqueLocationIds) {
          const response = await api.sde.getLocationName(locationId);
          locationNamesMap[locationId] = response.name;
        }
        setLocationNames(locationNamesMap);

      } catch (err) {
        console.error('Error fetching industry jobs:', err);
        setError(err instanceof Error ? err.message : 'Failed to fetch industry jobs');
      } finally {
        setLoading(false);
      }
    };

    fetchJobs();
  }, [navigate, includeCompleted]);

  const getTypeName = (typeId: number): string => {
    return typeInfoCache[typeId]?.typeName || `Type ${typeId}`;
  };

  const getActivityName = (activityId: number): string => {
    return activityNames[activityId] || `Activity ${activityId}`;
  };

  const getLocationName = (locationId: number): string => {
    return locationNames[locationId] || `Location ${locationId}`;
  };

  const formatDate = (dateStr: string): string => {
    return new Date(dateStr).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const formatDuration = (seconds: number): string => {
    const days = Math.floor(seconds / 86400);
    const hours = Math.floor((seconds % 86400) / 3600);
    const mins = Math.floor((seconds % 3600) / 60);
    
    if (days > 0) return `${days}d ${hours}h`;
    if (hours > 0) return `${hours}h ${mins}m`;
    return `${mins}m`;
  };

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

  const getStatusBadgeClass = (status: string): string => {
    switch (status.toLowerCase()) {
      case 'active':
        return 'status-active';
      case 'ready':
      case 'delivered':
        return 'status-ready';
      case 'paused':
        return 'status-paused';
      case 'cancelled':
        return 'status-cancelled';
      default:
        return 'status-unknown';
    }
  };

  const filteredJobs = jobs.filter(job => {
    if (filterStatus === 'all') return true;
    return job.status.toLowerCase() === filterStatus.toLowerCase();
  });

  const activeJobs = jobs.filter(j => j.status.toLowerCase() === 'active').length;
  const completedJobs = jobs.filter(j => ['ready', 'delivered'].includes(j.status.toLowerCase())).length;

  if (loading) {
    return (
      <div className="industry-jobs-container">
        <div className="loading">Loading industry jobs...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="industry-jobs-container">
        <div className="error-box">
          <h2>Error</h2>
          <p>{error}</p>
          <button onClick={() => navigate('/characters')}>Back to Characters</button>
        </div>
      </div>
    );
  }

  return (
    <div className="industry-jobs-container">
      <div className="jobs-header">
        <h1>Industry Jobs</h1>
        <div className="jobs-stats">
          <span className="stat-badge active-stat">Active: {activeJobs}</span>
          <span className="stat-badge completed-stat">Completed: {completedJobs}</span>
          <span className="stat-badge total-stat">Total: {jobs.length}</span>
        </div>
      </div>

      <div className="jobs-controls">
        <div className="control-group">
          <label>
            <input
              type="checkbox"
              checked={includeCompleted}
              onChange={(e) => setIncludeCompleted(e.target.checked)}
            />
            Include Completed Jobs
          </label>
        </div>

        <div className="filter-group">
          <label htmlFor="status-filter">Filter by Status:</label>
          <select
            id="status-filter"
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value)}
            className="filter-select"
          >
            <option value="all">All Statuses</option>
            <option value="active">Active</option>
            <option value="ready">Ready</option>
            <option value="delivered">Delivered</option>
            <option value="paused">Paused</option>
            <option value="cancelled">Cancelled</option>
          </select>
        </div>
      </div>

      <div className="jobs-table-container">
        {filteredJobs.length === 0 ? (
          <div className="no-jobs">
            <p>No industry jobs found</p>
          </div>
        ) : (
          <table className="jobs-table">
            <thead>
              <tr>
                <th>Activity</th>
                <th>Blueprint</th>
                <th>Runs</th>
                <th>Status</th>
                <th>Cost</th>
                <th>Duration</th>
                <th>Start Date</th>
                <th>End Date</th>
                <th>Location</th>
              </tr>
            </thead>
            <tbody>
              {filteredJobs.map(job => (
                <tr key={job.jobId}>
                  <td>
                    <span className="activity-badge">
                      {getActivityName(job.activityId)}
                    </span>
                  </td>
                  <td className="blueprint-cell">
                    <div className="item-name-cell">
                      <img 
                        src={`https://images.evetech.net/types/${job.blueprintTypeId}/icon?size=32`}
                        alt={getTypeName(job.blueprintTypeId)}
                        className="item-icon"
                        onError={(e) => { e.currentTarget.style.display = 'none'; }}
                      />
                      <span>{getTypeName(job.blueprintTypeId)}</span>
                    </div>
                  </td>
                  <td className="runs-cell">{job.runs}</td>
                  <td>
                    <span className={`status-badge ${getStatusBadgeClass(job.status)}`}>
                      {job.status}
                    </span>
                  </td>
                  <td className="cost-cell">{formatISK(job.cost)}</td>
                  <td>{formatDuration(job.timeInSeconds)}</td>
                  <td className="date-cell">{formatDate(job.startDate)}</td>
                  <td className="date-cell">{formatDate(job.endDate)}</td>
                  <td>{getLocationName(job.locationId)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

export default IndustryJobs;
