import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { api } from '../services/api';

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('toir_token') || '');
  const user = ref(JSON.parse(localStorage.getItem('toir_user') || 'null'));

  const isAuthenticated = computed(() => !!token.value && !!user.value);
  const isAdmin = computed(() => user.value?.roleCode === 'admin');
  const isChiefEngineer = computed(() => user.value?.roleCode === 'chief_engineer');
  const isMaster = computed(() => user.value?.roleCode === 'master');

  async function login(email, password, rememberMe = false) {
    const data = await api.post('/auth/login', { email, password, rememberMe });
    token.value = data.token;
    user.value = data.user;

    localStorage.setItem('toir_token', data.token);
    localStorage.setItem('toir_user', JSON.stringify(data.user));

    return data;
  }

  async function fetchMe() {
    if (!token.value) return null;
    try {
      const data = await api.get('/auth/me');
      user.value = data;
      localStorage.setItem('toir_user', JSON.stringify(data));
      return data;
    } catch (e) {
      logout();
      return null;
    }
  }

  function logout() {
    token.value = '';
    user.value = null;
    localStorage.removeItem('toir_token');
    localStorage.removeItem('toir_user');
  }

  return {
    token,
    user,
    isAuthenticated,
    isAdmin,
    isChiefEngineer,
    isMaster,
    login,
    fetchMe,
    logout
  };
});
