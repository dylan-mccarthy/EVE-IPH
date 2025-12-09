import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api, { type WalletTransaction, type TypeInfo } from '../utils/api';
import './WalletTransactions.css';

const WalletTransactions = () => {
  const [transactions, setTransactions] = useState<WalletTransaction[]>([]);
  const [typeInfoCache, setTypeInfoCache] = useState<Record<number, TypeInfo>>({});
  const [locationNames, setLocationNames] = useState<Record<number, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [filterType, setFilterType] = useState<string>('all');
  const [searchTerm, setSearchTerm] = useState('');
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

    const fetchTransactions = async () => {
      try {
        setLoading(true);
        const data = await api.wallet.getTransactions(characterId);
        setTransactions(data);

        // Fetch type info for all unique type IDs
        const uniqueTypeIds = [...new Set(data.map(tx => tx.typeId))];
        if (uniqueTypeIds.length > 0) {
          const typeInfos = await api.sde.getTypeInfoBatch(uniqueTypeIds);
          setTypeInfoCache(typeInfos);
        }

        // Fetch location names for all unique location IDs
        const uniqueLocationIds = [...new Set(data.map(tx => tx.locationId))];
        const locationNamesMap: Record<number, string> = {};
        for (const locationId of uniqueLocationIds) {
          const response = await api.sde.getLocationName(locationId);
          locationNamesMap[locationId] = response.name;
        }
        setLocationNames(locationNamesMap);

      } catch (err) {
        console.error('Error fetching wallet transactions:', err);
        setError(err instanceof Error ? err.message : 'Failed to fetch wallet transactions');
      } finally {
        setLoading(false);
      }
    };

    fetchTransactions();
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

  const filteredTransactions = transactions.filter(tx => {
    const matchesType = filterType === 'all' || 
      (filterType === 'buy' && tx.isBuy) ||
      (filterType === 'sell' && !tx.isBuy);
    
    const typeName = getTypeName(tx.typeId).toLowerCase();
    const matchesSearch = searchTerm === '' ||
      typeName.includes(searchTerm.toLowerCase()) ||
      tx.transactionId.toString().includes(searchTerm);
    
    return matchesType && matchesSearch;
  });

  const buyTransactions = transactions.filter(t => t.isBuy).length;
  const sellTransactions = transactions.filter(t => !t.isBuy).length;
  const totalSpent = transactions
    .filter(t => t.isBuy)
    .reduce((sum, t) => sum + (t.unitPrice * t.quantity), 0);
  const totalEarned = transactions
    .filter(t => !t.isBuy)
    .reduce((sum, t) => sum + (t.unitPrice * t.quantity), 0);
  const netProfit = totalEarned - totalSpent;

  if (loading) {
    return (
      <div className="wallet-transactions-container">
        <div className="loading">Loading wallet transactions...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="wallet-transactions-container">
        <div className="error-box">
          <h2>Error</h2>
          <p>{error}</p>
          <button onClick={() => navigate('/characters')}>Back to Characters</button>
        </div>
      </div>
    );
  }

  return (
    <div className="wallet-transactions-container">
      <div className="transactions-header">
        <h1>Wallet Transactions</h1>
        <div className="transactions-stats">
          <span className="stat-badge buy-stat">Purchases: {buyTransactions}</span>
          <span className="stat-badge sell-stat">Sales: {sellTransactions}</span>
          <span className={`stat-badge profit-stat ${netProfit >= 0 ? 'positive' : 'negative'}`}>
            Net: {formatISK(netProfit)}
          </span>
        </div>
      </div>

      <div className="transactions-controls">
        <div className="search-box">
          <input
            type="text"
            placeholder="Search by type ID or transaction ID..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="search-input"
          />
        </div>

        <div className="filter-group">
          <label htmlFor="type-filter">Filter by Type:</label>
          <select
            id="type-filter"
            value={filterType}
            onChange={(e) => setFilterType(e.target.value)}
            className="filter-select"
          >
            <option value="all">All Transactions</option>
            <option value="buy">Purchases</option>
            <option value="sell">Sales</option>
          </select>
        </div>
      </div>

      <div className="summary-cards">
        <div className="summary-card spent-card">
          <div className="card-label">Total Spent</div>
          <div className="card-value">{formatISK(totalSpent)}</div>
        </div>
        <div className="summary-card earned-card">
          <div className="card-label">Total Earned</div>
          <div className="card-value">{formatISK(totalEarned)}</div>
        </div>
        <div className={`summary-card profit-card ${netProfit >= 0 ? 'positive' : 'negative'}`}>
          <div className="card-label">Net Profit</div>
          <div className="card-value">{formatISK(netProfit)}</div>
        </div>
      </div>

      <div className="transactions-table-container">
        {filteredTransactions.length === 0 ? (
          <div className="no-transactions">
            <p>No transactions found</p>
          </div>
        ) : (
          <table className="transactions-table">
            <thead>
              <tr>
                <th>Date</th>
                <th>Type</th>
                <th>Item</th>
                <th>Quantity</th>
                <th>Unit Price</th>
                <th>Total</th>
                <th>Client</th>
                <th>Location</th>
              </tr>
            </thead>
            <tbody>
              {filteredTransactions.map(tx => {
                const total = tx.unitPrice * tx.quantity;
                return (
                  <tr key={tx.transactionId}>
                    <td className="date-cell">{formatDate(tx.date)}</td>
                    <td>
                      <span className={`tx-type-badge ${tx.isBuy ? 'buy-badge' : 'sell-badge'}`}>
                        {tx.isBuy ? 'BUY' : 'SELL'}
                      </span>
                    </td>
                    <td className="item-cell">
                      <div className="item-name-cell">
                        <img 
                          src={`https://images.evetech.net/types/${tx.typeId}/icon?size=32`}
                          alt={getTypeName(tx.typeId)}
                          className="item-icon"
                          onError={(e) => { e.currentTarget.style.display = 'none'; }}
                        />
                        <span>{getTypeName(tx.typeId)}</span>
                      </div>
                    </td>
                    <td className="quantity-cell">{tx.quantity.toLocaleString()}</td>
                    <td className="price-cell">{formatISK(tx.unitPrice)}</td>
                    <td className={`total-cell ${tx.isBuy ? 'negative' : 'positive'}`}>
                      {tx.isBuy ? '-' : '+'}{formatISK(total)}
                    </td>
                    <td className="client-cell">Client {tx.clientId}</td>
                    <td className="location-cell">{getLocationName(tx.locationId)}</td>
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

export default WalletTransactions;
