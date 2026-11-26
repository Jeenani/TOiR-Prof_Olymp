const API_BASE = '/api';

const HTTP_ERRORS = {
  400: 'Проверьте корректность введённых данных',
  403: 'Недостаточно прав для выполнения операции',
  404: 'Запрашиваемый объект не найден',
  409: 'Конфликт данных или расписания',
  429: 'Слишком много запросов. Повторите чуть позже',
  500: 'Внутренняя ошибка сервера'
};

export async function request(endpoint, options = {}) {
  const token = localStorage.getItem('toir_token');
  const headers = { 'Content-Type': 'application/json', ...(options.headers || {}) };
  if (token) headers.Authorization = `Bearer ${token}`;

  const url = endpoint.startsWith('http') ? endpoint : `${API_BASE}${endpoint}`;
  const t0 = performance.now();

  try {
    const res = await fetch(url, { ...options, headers });
    const ms = Math.round(performance.now() - t0);

    if (res.status === 401) {
      localStorage.removeItem('toir_token');
      localStorage.removeItem('toir_user');
      if (!location.pathname.includes('/login')) location.href = '/login';
      throw new Error('Сессия истекла. Выполните вход заново.');
    }

    if (!res.ok) {
      const data = await res.json().catch(() => ({}));
      const msg = data.message || HTTP_ERRORS[res.status] || `Ошибка сервера (${res.status})`;
      const err = new Error(msg);
      err.status = res.status;
      err.data = data;
      console.error('[API Error]', { status: res.status, endpoint, ms, data, stack: err.stack });
      throw err;
    }

    return res.status === 204 ? null : await res.json();
  } catch (err) {
    if (!err.status) console.error('[Network Error]', { endpoint, message: err.message, stack: err.stack });
    throw err;
  }
}

export const api = {
  get: (endpoint, params = {}) => {
    const q = new URLSearchParams();
    Object.entries(params).forEach(([k, v]) => { if (v !== undefined && v !== null && v !== '') q.append(k, v); });
    return request(q.toString() ? `${endpoint}?${q}` : endpoint, { method: 'GET' });
  },
  post: (endpoint, body) => request(endpoint, { method: 'POST', body: JSON.stringify(body) }),
  put: (endpoint, body) => request(endpoint, { method: 'PUT', body: JSON.stringify(body) }),
  delete: (endpoint) => request(endpoint, { method: 'DELETE' })
};

