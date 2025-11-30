import { useState, useEffect, useRef } from 'react';
import { lockingApi } from '../api/client';
import type { LockStatusResponse, LockExecuteResponse } from '../api/types';
import axios from 'axios';

export function DistributedLocking() {
  const [status, setStatus] = useState<LockStatusResponse | null>(null);
  const [executeResult, setExecuteResult] = useState<LockExecuteResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [executing, setExecuting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const pollIntervalRef = useRef<number | null>(null);

  const fetchStatus = async () => {
    try {
      const response = await lockingApi.getStatus();
      setStatus(response.data);
    } catch (err) {
      console.error('Failed to fetch lock status:', err);
    }
  };

  useEffect(() => {
    fetchStatus();
    pollIntervalRef.current = window.setInterval(fetchStatus, 1000);
    return () => {
      if (pollIntervalRef.current) {
        clearInterval(pollIntervalRef.current);
      }
    };
  }, []);

  const executeLock = async () => {
    setExecuting(true);
    setError(null);
    setExecuteResult(null);
    try {
      const response = await lockingApi.execute();
      setExecuteResult(response.data);
    } catch (err) {
      if (axios.isAxiosError(err) && err.response?.status === 409) {
        setExecuteResult(err.response.data as LockExecuteResponse);
      } else {
        setError(err instanceof Error ? err.message : 'Failed to execute');
      }
    } finally {
      setExecuting(false);
    }
  };

  const executeMultiple = async () => {
    setLoading(true);
    setError(null);
    setExecuteResult(null);

    type ExecuteResult =
      | { success: true; data: LockExecuteResponse }
      | { success: false; data: LockExecuteResponse; status: number };

    // Launch 3 concurrent requests
    const promises = Array(3).fill(null).map((): Promise<ExecuteResult> =>
      lockingApi.execute()
        .then((response) => ({ success: true as const, data: response.data }))
        .catch((err) => {
          if (axios.isAxiosError(err) && err.response) {
            return { success: false as const, data: err.response.data, status: err.response.status };
          }
          throw err;
        })
    );

    try {
      const results = await Promise.all(promises);
      const firstSuccess = results.find((r) => r.success);
      if (firstSuccess) {
        setExecuteResult(firstSuccess.data);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to execute');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="demo-section">
      <h2>Distributed Locking</h2>
      <p className="description">
        Demonstrates Redis distributed locks for exclusive access. The lock lasts 5 seconds.
        Concurrent requests will be blocked until the lock is released.
      </p>

      <div className="lock-status">
        <span className={`status-dot ${status?.isLocked ? 'locked' : 'unlocked'}`} />
        Lock Status: {status?.isLocked ? 'Locked' : 'Available'}
        {status?.isLocked && status.remainingLockTime !== undefined && (
          <span className="remaining-time">
            ({status.remainingLockTime.toFixed(1)}s remaining)
          </span>
        )}
      </div>

      <div className="actions">
        <button onClick={executeLock} disabled={executing || loading}>
          {executing ? 'Executing...' : 'Execute with Lock'}
        </button>
        <button onClick={executeMultiple} disabled={executing || loading} className="secondary">
          {loading ? 'Running...' : 'Try 3 Concurrent'}
        </button>
      </div>

      {error && <div className="error">{error}</div>}

      {executeResult && (
        <div className={`result ${executeResult.executionTime ? 'success' : 'conflict'}`}>
          <strong>{executeResult.message}</strong>
          {executeResult.executionTime && (
            <p>Execution time: {executeResult.executionTime}</p>
          )}
          {executeResult.remainingLockTime !== undefined && (
            <p>Remaining lock time: {executeResult.remainingLockTime.toFixed(1)}s</p>
          )}
        </div>
      )}
    </div>
  );
}
