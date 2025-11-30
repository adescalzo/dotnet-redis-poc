// Caching types
export interface CachedItem {
  id: number;
  name: string;
  value: number;
}

export interface CachedDataResponse {
  generatedAt: string;
  items: CachedItem[];
}

export interface CacheInvalidateResponse {
  message: string;
}

// PubSub types
export interface PubSubTriggerResponse {
  message: string;
  channel: string;
  publishedAt: string;
}

export interface NotificationMessage {
  channel: string;
  message: string;
  receivedAt: string;
}

// Locking types
export interface LockExecuteResponse {
  message: string;
  lockKey: string;
  executionTime?: string;
  remainingLockTime?: number;
}

export interface LockStatusResponse {
  lockKey: string;
  isLocked: boolean;
  remainingLockTime?: number;
}

// Rate limiting types
export interface RateLimitResponse {
  message: string;
  processedAt: string;
}

export interface RateLimitError {
  type: string;
  title: string;
  status: number;
  detail: string;
  instance: string;
}

// Health check
export interface HealthResponse {
  status: string;
  timestamp: string;
}
