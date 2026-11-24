import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useToastStore = defineStore('toast', () => {
  const toasts = ref([]);

  function add(type, message, duration = 4000) {
    const id = Date.now() + Math.random();
    toasts.value.push({ id, type, message });
    setTimeout(() => {
      remove(id);
    }, duration);
  }

  function remove(id) {
    toasts.value = toasts.value.filter(t => t.id !== id);
  }

  return {
    toasts,
    success: (msg) => add('success', msg),
    error: (msg) => add('error', msg),
    warning: (msg) => add('warning', msg),
    info: (msg) => add('info', msg),
    remove
  };
});
