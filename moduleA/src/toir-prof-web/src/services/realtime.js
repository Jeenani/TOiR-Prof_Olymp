import { ref } from 'vue';

export const connectionStatus = ref('reconnecting'); // 'connected' | 'reconnecting' | 'disconnected'
export const connectionError = ref(null);

class RealtimeService {
  constructor() {
    this.listeners = new Map();
    this.reconnectDelays = [1000, 2000, 5000, 10000];
    this.reconnectAttempt = 0;
    this.source = null;
    this.reconnectTimer = null;
    this.streamUrl = 'http://localhost:5000/api/analytics/stream';
  }

  connect() {
    if (this.source) {
      this.source.close();
      this.source = null;
    }
    try {
      this.source = new EventSource(this.streamUrl);
      this.source.onopen = () => {
        connectionStatus.value = 'connected';
        connectionError.value = null;
        this.reconnectAttempt = 0;
        this.emit('reconnected', {});
      };
      this.source.onmessage = (evt) => {
        try {
          const payload = JSON.parse(evt.data);
          this.emit(payload.type || 'heartbeat', payload);
        } catch {
          this.emit('heartbeat', { timestamp: new Date().toISOString() });
        }
      };
      this.source.onerror = () => {
        connectionStatus.value = 'disconnected';
        this.source?.close();
        this.source = null;
        this.scheduleReconnect();
      };
    } catch {
      this.scheduleReconnect();
    }
  }

  scheduleReconnect() {
    if (this.reconnectTimer) return;
    const idx = Math.min(this.reconnectAttempt, this.reconnectDelays.length - 1);
    const delay = this.reconnectDelays[idx];
    connectionStatus.value = 'reconnecting';
    this.reconnectTimer = setTimeout(() => {
      this.reconnectTimer = null;
      this.reconnectAttempt++;
      this.connect();
    }, delay);
  }

  on(event, callback) {
    if (!this.listeners.has(event)) this.listeners.set(event, new Set());
    this.listeners.get(event).add(callback);
    return () => this.listeners.get(event).delete(callback);
  }

  emit(event, data) {
    if (!this.listeners.has(event)) return;
    for (const cb of this.listeners.get(event)) {
      try { cb(data); } catch (e) { console.error(`[Realtime Error] ${event}:`, e); }
    }
  }
}

export const realtime = new RealtimeService();
realtime.connect();

