import { useState } from 'react';
import { rateLimitApi } from '../api/client';
import axios from 'axios';

interface RequestResult {
  id: number;
  success: boolean;
  message: string;
  timestamp: string;
  retryAfter?: string;
}

export function RateLimiting() {
  const [results, setResults] = useState<RequestResult[]>([]);
  const [loading, setLoading] = useState(false);

  const sendSingleRequest = async () => {
    const id = Date.now();
    try {
      const response = await rateLimitApi.test();
      return {
        id,
        success: true,
        message: response.data.message,
        timestamp: new Date(response.data.processedAt).toLocaleTimeString('en-US', {
          hour12: false,
          hour: '2-digit',
          minute: '2-digit',
          second: '2-digit',
          fractionalSecondDigits: 3,
        }),
      };
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 429) {
        const retryAfter = err.response.headers['retry-after'];
        return {
          id,
          success: false,
          message: err.response.data?.detail || 'Rate limit exceeded',
          timestamp: new Date().toLocaleTimeString('en-US', {
            hour12: false,
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            fractionalSecondDigits: 3,
          }),
          retryAfter,
        };
      }
      return {
        id,
        success: false,
        message: err instanceof Error ? err.message : 'Request failed',
        timestamp: new Date().toLocaleTimeString(),
      };
    }
  };

  const sendBurstRequests = async () => {
    setLoading(true);
    setResults([]);

    const burstResults: RequestResult[] = [];

    // Send 5 requests in quick succession
    for (let i = 0; i < 5; i++) {
      const result = await sendSingleRequest();
      burstResults.push(result);
      setResults([...burstResults]);
      // Small delay to see results appearing
      await new Promise((r) => setTimeout(r, 100));
    }

    setLoading(false);
  };

  const clearResults = () => {
    setResults([]);
  };

  const successCount = results.filter((r) => r.success).length;
  const failCount = results.filter((r) => !r.success).length;

  return (
    <div className="demo-section">
      <h2>Rate Limiting</h2>
      <p className="description">
        Demonstrates Redis-backed rate limiting. The endpoint allows <strong>3 requests per 5-second window</strong>.
        Click the button to send 5 rapid requests and see which succeed vs get rate-limited.
      </p>

      <div className="actions">
        <button onClick={sendBurstRequests} disabled={loading}>
          {loading ? 'Sending...' : 'Send 5 Rapid Requests'}
        </button>
        <button onClick={clearResults} className="secondary" disabled={results.length === 0}>
          Clear
        </button>
      </div>

      {results.length > 0 && (
        <div className="rate-limit-summary">
          <span className="success-count">{successCount} succeeded</span>
          <span className="separator">|</span>
          <span className="fail-count">{failCount} rate-limited</span>
        </div>
      )}

      <div className="results-list">
        {results.map((result, index) => (
          <div
            key={result.id}
            className={`result-item ${result.success ? 'success' : 'rate-limited'}`}
          >
            <span className="request-number">#{index + 1}</span>
            <span className={`status-badge ${result.success ? 'ok' : 'limited'}`}>
              {result.success ? '200 OK' : '429 Too Many'}
            </span>
            <span className="result-message">{result.message}</span>
            <span className="result-time">{result.timestamp}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
