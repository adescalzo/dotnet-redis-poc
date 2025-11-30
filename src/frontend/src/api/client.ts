import axios from 'axios';
import type {
  CachedDataResponse,
  CacheInvalidateResponse,
  PubSubTriggerResponse,
  LockExecuteResponse,
  LockStatusResponse,
  RateLimitResponse,
  HealthResponse,
} from './types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Caching API
export const cachingApi = {
  getData: () => api.get<CachedDataResponse>('/api/caching/data'),
  invalidate: () => api.delete<CacheInvalidateResponse>('/api/caching/invalidate'),
};

// PubSub API
export const pubSubApi = {
  trigger: () => api.post<PubSubTriggerResponse>('/api/pubsub/trigger'),
};

// Locking API
export const lockingApi = {
  execute: () => api.post<LockExecuteResponse>('/api/locking/execute'),
  getStatus: () => api.get<LockStatusResponse>('/api/locking/status'),
};

// Rate Limiting API
export const rateLimitApi = {
  test: () => api.get<RateLimitResponse>('/api/ratelimit/test'),
  unlimited: () => api.get<RateLimitResponse>('/api/ratelimit/unlimited'),
};

// Health API
export const healthApi = {
  check: () => api.get<HealthResponse>('/health'),
};

export { API_BASE_URL };
export default api;
