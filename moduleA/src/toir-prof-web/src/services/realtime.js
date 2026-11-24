import { ref } from 'vue';

export const connectionStatus = ref('connected'); // 'connected' | 'reconnecting' | 'disconnected'
export const connectionError = ref(null);

class RealtimeService {
  constructor() {
    this.listeners = new Map();
    this.reconnectDelays = [1000, 2000, 5000, 10000];
    this.reconnectAttempt = 0;
    this.timer = null;
    this.isSimulated = true;
  }

  connect() {
    connectionStatus.value = 'connected';
    console.log('[Realtime] Подключение установлено к потоку событий ТОиР-Проф');
    this.startHeartbeat();
  }

  startHeartbeat() {
    if (this.timer) clearInterval(this.timer);
    this.timer = setInterval(() => {
      // Имитация фоновых real-time обновлений системы
      this.emit('heartbeat', { timestamp: new Date().toISOString() });
    }, 15000);
  }

  simulateDisconnect() {
    connectionStatus.value = 'disconnected';
    console.warn('[Realtime] Связь с сервером событий прервана. Запуск экспоненциального переподключения...');
    this.scheduleReconnect();
  }

  scheduleReconnect() {
    if (this.reconnectAttempt >= this.reconnectDelays.length) {
      this.reconnectAttempt = this.reconnectDelays.length - 1;
    }
    const delay = this.reconnectDelays[this.reconnectAttempt];
    connectionStatus.value = 'reconnecting';
    console.log(`[Realtime] Попытка переподключения #${this.reconnectAttempt + 1} через ${delay / 1000} сек...`);

    setTimeout(() => {
      this.reconnectAttempt++;
      // Восстановление соединения
      connectionStatus.value = 'connected';
      this.reconnectAttempt = 0;
      console.log('[Realtime] Связь успешно восстановлена!');
      this.emit('reconnected', {});
    }, delay);
  }

  on(event, callback) {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    this.listeners.get(event).add(callback);
    return () => this.listeners.get(event).delete(callback);
  }

  emit(event, data) {
    if (this.listeners.has(event)) {
      for (const cb of this.listeners.get(event)) {
        try {
          cb(data);
        } catch (e) {
          console.error(`[Realtime Error] Ошибка в обработчике события '${event}':`, e);
        }
      }
    }
  }
}

export const realtime = new RealtimeService();
realtime.connect();
