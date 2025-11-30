import { useState } from 'react';
import { cachingApi } from '../api/client';
import type { CachedDataResponse } from '../api/types';

export function HybridCaching() {
  const [data, setData] = useState<CachedDataResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [requestCount, setRequestCount] = useState(0);

  const fetchData = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await cachingApi.getData();
      setData(response.data);
      setRequestCount((c) => c + 1);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch data');
    } finally {
      setLoading(false);
    }
  };

  const invalidateCache = async () => {
    setLoading(true);
    setError(null);
    try {
      await cachingApi.invalidate();
      setData(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to invalidate cache');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="demo-section">
      <h2>Hybrid Caching</h2>
      <p className="description">
        Demonstrates Redis hybrid caching (L1 in-memory + L2 Redis). The <code>GeneratedAt</code>{' '}
        timestamp shows when data was generated vs served from cache.
      </p>

      <div className="actions">
        <button onClick={fetchData} disabled={loading}>
          {loading ? 'Loading...' : 'Fetch Data'}
        </button>
        <button onClick={invalidateCache} disabled={loading} className="secondary">
          Invalidate Cache
        </button>
      </div>

      <div className="stats">
        <span>Requests made: {requestCount}</span>
      </div>

      {error && <div className="error">{error}</div>}

      {data && (
        <div className="result">
          <div className="timestamp">
            <strong>Generated At:</strong>{' '}
            {new Date(data.generatedAt).toLocaleTimeString('en-US', {
              hour12: false,
              hour: '2-digit',
              minute: '2-digit',
              second: '2-digit',
              fractionalSecondDigits: 3,
            })}
          </div>
          <table>
            <thead>
              <tr>
                <th>ID</th>
                <th>Name</th>
                <th>Value</th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((item) => (
                <tr key={item.id}>
                  <td>{item.id}</td>
                  <td>{item.name}</td>
                  <td>{item.value}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
