const API_BASE = '/api';

export async function request(endpoint, options = {}) {
  const token = localStorage.getItem('toir_token');
  const headers = {
    'Content-Type': 'application/json',
    ...(options.headers || {})
  };

  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  const url = endpoint.startsWith('http') ? endpoint : `${API_BASE}${endpoint}`;
  const startTime = performance.now();

  try {
    const response = await fetch(url, {
      ...options,
      headers
    });

    const duration = Math.round(performance.now() - startTime);

    if (response.status === 401) {
      localStorage.removeItem('toir_token');
      localStorage.removeItem('toir_user');
      if (!window.location.pathname.includes('/login')) {
        window.location.href = '/login';
      }
      throw new Error('Сессия истекла. Пожалуйста, выполните вход заново.');
    }

    if (!response.ok) {
      let errorData;
      try {
        errorData = await response.json();
      } catch {
        errorData = { message: `Ошибка сервера (${response.status})` };
      }

      console.error(`[API Error ${response.status}] ${endpoint} (${duration}ms):`, errorData);
      const err = new Error(errorData.message || `Ошибка запроса ${response.status}`);
      err.status = response.status;
      err.data = errorData;
      throw err;
    }

    // Обработка 204 No Content
    if (response.status === 204) return null;

    const data = await response.json();
    return data;
  } catch (error) {
    console.error(`[Network/Fetch Error] ${endpoint}:`, error);
    throw error;
  }
}

export const api = {
  get: (endpoint, params = {}) => {
    const query = new URLSearchParams();
    for (const [k, v] of Object.entries(params)) {
      if (v !== undefined && v !== null && v !== '') {
        query.append(k, v);
      }
    }
    const qStr = query.toString();
    const url = qStr ? `${endpoint}?${qStr}` : endpoint;
    return request(url, { method: 'GET' });
  },

  post: (endpoint, body) => request(endpoint, {
    method: 'POST',
    body: JSON.stringify(body)
  }),

  put: (endpoint, body) => request(endpoint, {
    method: 'PUT',
    body: JSON.stringify(body)
  }),

  delete: (endpoint) => request(endpoint, {
    method: 'DELETE'
  })
};
