import { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { pubSubApi, API_BASE_URL } from '../api/client';
import type { NotificationMessage, PubSubTriggerResponse } from '../api/types';

export function PubSub() {
  const [connected, setConnected] = useState(false);
  const [notifications, setNotifications] = useState<NotificationMessage[]>([]);
  const [triggerResponse, setTriggerResponse] = useState<PubSubTriggerResponse | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/notifications`)
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connection.on('Connected', (message: string) => {
      console.log('SignalR connected:', message);
    });

    connection.on('ReceiveNotification', (notification: NotificationMessage) => {
      setNotifications((prev) => [notification, ...prev]);
    });

    connection.onclose(() => setConnected(false));
    connection.onreconnecting(() => setConnected(false));
    connection.onreconnected(() => setConnected(true));

    connection
      .start()
      .then(() => {
        setConnected(true);
        connectionRef.current = connection;
      })
      .catch((err) => {
        setError(`SignalR connection failed: ${err.message}`);
      });

    return () => {
      connection.stop();
    };
  }, []);

  const triggerNotifications = async () => {
    setLoading(true);
    setError(null);
    try {
      const response = await pubSubApi.trigger();
      setTriggerResponse(response.data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to trigger notifications');
    } finally {
      setLoading(false);
    }
  };

  const clearNotifications = () => {
    setNotifications([]);
    setTriggerResponse(null);
  };

  return (
    <div className="demo-section">
      <h2>Publish/Subscribe with SignalR</h2>
      <p className="description">
        Demonstrates Redis Pub/Sub for real-time communication. When triggered, the backend publishes
        messages to Redis, a background service receives them, and pushes notifications via SignalR.
      </p>

      <div className="connection-status">
        <span className={`status-dot ${connected ? 'connected' : 'disconnected'}`} />
        SignalR: {connected ? 'Connected' : 'Disconnected'}
      </div>

      <div className="actions">
        <button onClick={triggerNotifications} disabled={loading || !connected}>
          {loading ? 'Triggering...' : 'Trigger Notifications'}
        </button>
        <button onClick={clearNotifications} className="secondary" disabled={notifications.length === 0}>
          Clear
        </button>
      </div>

      {error && <div className="error">{error}</div>}

      {triggerResponse && (
        <div className="trigger-response">
          <strong>Published:</strong> {triggerResponse.message} at{' '}
          {new Date(triggerResponse.publishedAt).toLocaleTimeString()}
        </div>
      )}

      <div className="notifications-list">
        <h3>Received Notifications ({notifications.length})</h3>
        {notifications.length === 0 ? (
          <p className="empty">No notifications yet. Click "Trigger Notifications" to start.</p>
        ) : (
          <ul>
            {notifications.map((n, i) => (
              <li key={i} className="notification-item">
                <span className="notification-message">{n.message}</span>
                <span className="notification-time">
                  {new Date(n.receivedAt).toLocaleTimeString('en-US', {
                    hour12: false,
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit',
                    fractionalSecondDigits: 3,
                  })}
                </span>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
