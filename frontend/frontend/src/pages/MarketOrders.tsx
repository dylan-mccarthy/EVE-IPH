import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api, { type MarketOrder, type TypeInfo } from '../utils/api';
import './MarketOrders.css';

const MarketOrders = () => {
  const [orders, setOrders] = useState<MarketOrder[]>([]);
  const [typeInfoCache, setTypeInfoCache] = useState<Record<number, TypeInfo>>({});
  const [locationNames, setLocationNames] = useState<Record<number, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterType, setFilterType] = useState<string>('all');
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

    const fetchOrders = async () => {
      try {
        setLoading(true);
        const data = await api.market.getOrders(characterId);
        setOrders(data);

        // Fetch type info for all unique type IDs
        const uniqueTypeIds = [...new Set(data.map(o => o.typeId))];
        if (uniqueTypeIds.length > 0) {
          const typeInfos = await api.sde.getTypeInfoBatch(uniqueTypeIds);
          setTypeInfoCache(typeInfos);
        }

        // Fetch location names for all unique location IDs
        const uniqueLocationIds = [...new Set(data.map(o => o.locationId))];
        const locationNamesMap: Record<number, string> = {};
        for (const locationId of uniqueLocationIds) {
          const response = await api.sde.getLocationName(locationId);
          locationNamesMap[locationId] = response.name;
        }
        setLocationNames(locationNamesMap);

      } catch (err) {
        console.error('Error fetching market orders:', err);
        setError(err instanceof Error ? err.message : 'Failed to fetch market orders');
      } finally {
        setLoading(false);
      }
    };

    fetchOrders();
  }, [navigate]);

  const getTypeName = (typeId: number): string => {
    return typeInfoCache[typeId]?.typeName || `Type ${typeId}`;
  };

  const getLocationName = (locationId: number): string => {
    return locationNames[locationId] || `Location ${locationId}`;
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

  const formatDate = (dateStr: string): string => {
    return new Date(dateStr).toLocaleString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const filteredOrders = orders.filter(order => {
    if (filterType === 'all') return true;
    if (filterType === 'buy') return order.isBuyOrder;
    if (filterType === 'sell') return !order.isBuyOrder;
    return true;
  });

  const buyOrders = orders.filter(o => o.isBuyOrder).length;
  const sellOrders = orders.filter(o => !o.isBuyOrder).length;
  const totalValue = orders.reduce((sum, order) => {
    const remaining = parseInt(order.volumeRemain);
    return sum + (order.price * remaining);
  }, 0);

  if (loading) {
    return (
      <div className="market-orders-container">
        <div className="loading">Loading market orders...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="market-orders-container">
        <div className="error-box">
          <h2>Error</h2>
          <p>{error}</p>
          <button onClick={() => navigate('/characters')}>Back to Characters</button>
        </div>
      </div>
    );
  }

  return (
    <div className="market-orders-container">
      <div className="orders-header">
        <h1>Market Orders</h1>
        <div className="orders-stats">
          <span className="stat-badge buy-stat">Buy Orders: {buyOrders}</span>
          <span className="stat-badge sell-stat">Sell Orders: {sellOrders}</span>
          <span className="stat-badge value-stat">Total Value: {formatISK(totalValue)}</span>
        </div>
      </div>

      <div className="orders-controls">
        <div className="filter-group">
          <label htmlFor="type-filter">Filter by Type:</label>
          <select
            id="type-filter"
            value={filterType}
            onChange={(e) => setFilterType(e.target.value)}
            className="filter-select"
          >
            <option value="all">All Orders</option>
            <option value="buy">Buy Orders</option>
            <option value="sell">Sell Orders</option>
          </select>
        </div>
      </div>

      <div className="orders-table-container">
        {filteredOrders.length === 0 ? (
          <div className="no-orders">
            <p>No market orders found</p>
          </div>
        ) : (
          <table className="orders-table">
            <thead>
              <tr>
                <th>Type</th>
                <th>Item</th>
                <th>Price</th>
                <th>Volume</th>
                <th>Total Value</th>
                <th>Min Volume</th>
                <th>Range</th>
                <th>Duration</th>
                <th>Issued</th>
                <th>Location</th>
              </tr>
            </thead>
            <tbody>
              {filteredOrders.map(order => {
                const remaining = parseInt(order.volumeRemain);
                const orderValue = order.price * remaining;
                return (
                  <tr key={order.orderId}>
                    <td>
                      <span className={`order-type-badge ${order.isBuyOrder ? 'buy-badge' : 'sell-badge'}`}>
                        {order.isBuyOrder ? 'BUY' : 'SELL'}
                      </span>
                    </td>
                    <td className="item-cell">
                      <div className="item-name-cell">
                        <img 
                          src={`https://images.evetech.net/types/${order.typeId}/icon?size=32`}
                          alt={getTypeName(order.typeId)}
                          className="item-icon"
                          onError={(e) => { e.currentTarget.style.display = 'none'; }}
                        />
                        <span>{getTypeName(order.typeId)}</span>
                      </div>
                    </td>
                    <td className="price-cell">{formatISK(order.price)}</td>
                    <td className="volume-cell">
                      <span className="volume-remaining">{order.volumeRemain}</span>
                      <span className="volume-total"> / {order.volumeTotal}</span>
                    </td>
                    <td className="value-cell">{formatISK(orderValue)}</td>
                    <td className="min-volume-cell">{order.minVolume}</td>
                    <td className="range-cell">{order.range}</td>
                    <td className="duration-cell">{order.duration} days</td>
                    <td className="date-cell">{formatDate(order.issued)}</td>
                    <td className="location-cell">{getLocationName(order.locationId)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};

export default MarketOrders;
