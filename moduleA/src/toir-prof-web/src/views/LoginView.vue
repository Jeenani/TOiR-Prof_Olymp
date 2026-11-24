<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { useToastStore } from '../stores/toastStore';

const router = useRouter();
const authStore = useAuthStore();
const toast = useToastStore();

const email = ref('admin@pmz.ru');
const password = ref('password123');
const rememberMe = ref(true);
const showPassword = ref(false);
const isLoading = ref(false);
const errorMessage = ref('');

const emailError = ref('');
const passwordError = ref('');

function validate() {
  let valid = true;
  emailError.value = '';
  passwordError.value = '';
  if (!email.value || !email.value.includes('@')) {
    emailError.value = 'Введите корректный email';
    valid = false;
  }
  if (!password.value || password.value.length < 4) {
    passwordError.value = 'Минимум 4 символа';
    valid = false;
  }
  return valid;
}

async function handleLogin() {
  if (!validate()) return;
  isLoading.value = true;
  errorMessage.value = '';

  try {
    const data = await authStore.login(email.value, password.value, rememberMe.value);
    toast.success(`Добро пожаловать, ${data.user.fullName}!`);
    router.push('/');
  } catch (err) {
    errorMessage.value = err.message || 'Ошибка входа в систему';
    toast.error(errorMessage.value);
  } finally {
    isLoading.value = false;
  }
}

function quickLogin(demoEmail) {
  email.value = demoEmail;
  password.value = 'password123';
  handleLogin();
}
</script>

<template>
  <div class="login-wrapper">
    <div class="login-card">
      <div class="login-header">
        <div class="login-badge">ТОиР-Проф</div>
        <h1 class="login-title">Вход в систему</h1>
        <p class="login-sub">Управление техническим обслуживанием и ремонтом оборудования</p>
      </div>

      <div v-if="errorMessage" class="login-alert">⚠ {{ errorMessage }}</div>

      <form @submit.prevent="handleLogin" class="login-form">
        <div class="form-group">
          <label class="form-label">Электронная почта</label>
          <input
            v-model="email"
            type="email"
            class="form-input"
            :class="{ 'is-invalid': emailError }"
            placeholder="operator@pmz.ru"
            :disabled="isLoading"
          />
          <div v-if="emailError" class="invalid-feedback">{{ emailError }}</div>
        </div>

        <div class="form-group">
          <label class="form-label">Пароль</label>
          <div class="password-wrapper">
            <input
              v-model="password"
              :type="showPassword ? 'text' : 'password'"
              class="form-input"
              :class="{ 'is-invalid': passwordError }"
              placeholder="••••••••"
              :disabled="isLoading"
            />
            <button type="button" class="toggle-pwd" @click="showPassword = !showPassword">
              {{ showPassword ? '🙈' : '👁' }}
            </button>
          </div>
          <div v-if="passwordError" class="invalid-feedback">{{ passwordError }}</div>
        </div>

        <div class="remember-row">
          <label class="checkbox-label">
            <input v-model="rememberMe" type="checkbox" :disabled="isLoading" />
            <span>Запомнить меня на этом устройстве</span>
          </label>
        </div>

        <button type="submit" class="btn btn-primary btn-block" :disabled="isLoading">
          <span v-if="isLoading" class="spinner"></span>
          <span>{{ isLoading ? 'Проверка данных...' : 'Войти в систему' }}</span>
        </button>
      </form>

      <div class="demo-logins">
        <div class="demo-title">Быстрый демонстрационный вход:</div>
        <div class="demo-buttons">
          <button class="demo-btn" @click="quickLogin('admin@pmz.ru')">Администратор</button>
          <button class="demo-btn" @click="quickLogin('ivanov.ii@pmz.ru')">Мастер Цеха №1</button>
          <button class="demo-btn" @click="quickLogin('sokolov.vm@pmz.ru')">Бригадир</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.login-wrapper {
  min-height: 100vh; display: flex; align-items: center; justify-content: center;
  background: radial-gradient(circle at top, #1e293b, #0f172a); padding: 20px;
}
.login-card {
  width: 100%; max-width: 440px; background: #1e293b; border: 1px solid #334155;
  border-radius: 14px; padding: 36px; box-shadow: 0 20px 50px rgba(0,0,0,0.5);
}
.login-header { text-align: center; margin-bottom: 24px; }
.login-badge {
  display: inline-block; background: linear-gradient(135deg, #0284c7, #0369a1);
  color: #fff; font-weight: 800; font-size: 13px; padding: 4px 12px; border-radius: 20px; margin-bottom: 12px;
}
.login-title { font-size: 22px; font-weight: 800; color: #f8fafc; margin-bottom: 6px; }
.login-sub { font-size: 13px; color: #94a3b8; }
.login-alert {
  background: rgba(239, 68, 68, 0.15); border: 1px solid rgba(239, 68, 68, 0.3);
  color: #f87171; padding: 10px 14px; border-radius: 8px; font-size: 13px; margin-bottom: 18px;
}
.password-wrapper { position: relative; }
.toggle-pwd { position: absolute; right: 10px; top: 7px; background: transparent; border: none; font-size: 14px; cursor: pointer; color: #94a3b8; }
.remember-row { margin: 16px 0 20px; }
.checkbox-label { display: flex; align-items: center; gap: 8px; font-size: 12px; color: #94a3b8; cursor: pointer; }
.btn-block { width: 100%; justify-content: center; padding: 11px; font-size: 14px; }
.demo-logins { margin-top: 24px; padding-top: 18px; border-top: 1px solid #334155; }
.demo-title { font-size: 11px; color: #64748b; text-transform: uppercase; font-weight: 700; margin-bottom: 10px; text-align: center; }
.demo-buttons { display: flex; gap: 8px; justify-content: center; flex-wrap: wrap; }
.demo-btn { background: #0f172a; border: 1px solid #334155; color: #94a3b8; padding: 5px 10px; border-radius: 6px; font-size: 11px; cursor: pointer; }
.demo-btn:hover { color: #f8fafc; border-color: #38bdf8; }
</style>
