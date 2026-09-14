class CentralEventBus {
  constructor() {
    this.listeners = new Map();
  }

  on(event, callback) {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, new Set());
    }
    this.listeners.get(event).add(callback);
  }

  off(event, callback) {
    if (this.listeners.has(event)) {
      this.listeners.get(event).delete(callback);
    }
  }

  emit(event, payload = {}) {
    if (this.listeners.has(event)) {
      for (const callback of this.listeners.get(event)) {
        try { callback(payload); } 
        catch (e) { console.error(`EventBus Error [${event}]:`, e); }
      }
    }
  }
}

export const EventBus = new CentralEventBus();