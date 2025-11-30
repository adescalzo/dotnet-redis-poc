import { useState, useEffect } from 'react';
import { HybridCaching } from './components/HybridCaching';
import { PubSub } from './components/PubSub';
import { DistributedLocking } from './components/DistributedLocking';
import { RateLimiting } from './components/RateLimiting';
import { healthApi } from './api/client';
import './App.css';

type Tab = 'caching' | 'pubsub' | 'locking' | 'ratelimit';

function App() {
  const [activeTab, setActiveTab] = useState<Tab>('caching');
  const [apiStatus, setApiStatus] = useState<'checking' | 'online' | 'offline'>('checking');

  useEffect(() => {
    const checkHealth = async () => {
      try {
        await healthApi.check();
        setApiStatus('online');
      } catch {
        setApiStatus('offline');
      }
    };

    checkHealth();
    const interval = setInterval(checkHealth, 30000);
    return () => clearInterval(interval);
  }, []);

  const tabs: { id: Tab; label: string; description: string }[] = [
    { id: 'caching', label: 'Hybrid Caching', description: 'L1 + L2 cache' },
    { id: 'pubsub', label: 'Pub/Sub', description: 'Real-time notifications' },
    { id: 'locking', label: 'Distributed Lock', description: 'Exclusive access' },
    { id: 'ratelimit', label: 'Rate Limiting', description: 'Request throttling' },
  ];

  return (
    <div className="app">
      <header className="header">
        <h1>.NET Redis PoC</h1>
        <div className={`api-status ${apiStatus}`}>
          <span className="status-dot" />
          API: {apiStatus === 'checking' ? 'Checking...' : apiStatus === 'online' ? 'Online' : 'Offline'}
        </div>
      </header>

      <nav className="tabs">
        {tabs.map((tab) => (
          <button
            key={tab.id}
            className={`tab ${activeTab === tab.id ? 'active' : ''}`}
            onClick={() => setActiveTab(tab.id)}
          >
            <span className="tab-label">{tab.label}</span>
            <span className="tab-description">{tab.description}</span>
          </button>
        ))}
      </nav>

      <main className="content">
        {activeTab === 'caching' && <HybridCaching />}
        {activeTab === 'pubsub' && <PubSub />}
        {activeTab === 'locking' && <DistributedLocking />}
        {activeTab === 'ratelimit' && <RateLimiting />}
      </main>

      <footer className="footer">
        <p>
          Demonstrating Redis use cases with ASP.NET Core 10 and React 19
        </p>
      </footer>
    </div>
  );
}

export default App;
