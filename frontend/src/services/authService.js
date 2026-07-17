const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5113';

export class ApiError extends Error {
  constructor(message, errors = []) {
    super(message);
    this.name = 'ApiError';
    this.errors = errors;
  }
}

async function request(path, { token, ...options } = {}) {
  const headers = new Headers(options.headers || {});

  if (!headers.has('Content-Type') && options.body) {
    headers.set('Content-Type', 'application/json');
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers,
  });

  let data = null;
  const contentType = response.headers.get('content-type') || '';
  if (contentType.includes('application/json')) {
    data = await response.json();
  }

  if (!response.ok) {
    const errors = Array.isArray(data?.errors) ? data.errors : [];
    const message = data?.message || cleanFallbackMessage(response.status);
    throw new ApiError(message, errors);
  }

  return data;
}

function cleanFallbackMessage(status) {
  if (status === 401) {
    return 'Invalid email or password.';
  }

  if (status === 409) {
    return 'Email already exists.';
  }

  return 'Something went wrong. Please try again.';
}

export function getErrorMessage(error, fallback = 'Something went wrong. Please try again.') {
  if (error instanceof ApiError) {
    return error.errors[0] || error.message || fallback;
  }

  if (error instanceof TypeError) {
    return 'Cannot connect to the server. Make sure the backend is running.';
  }

  return fallback;
}

export const authService = {
  register(payload) {
    return request('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },

  login(payload) {
    return request('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },

  googleLogin(payload) {
    return request('/api/auth/google', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },

  forgotPassword(payload) {
    return request('/api/auth/forgot-password', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },

  resetPassword(payload) {
    return request('/api/auth/reset-password', {
      method: 'POST',
      body: JSON.stringify(payload),
    });
  },

  logout(token, refreshToken) {
    return request('/api/auth/logout', {
      method: 'POST',
      token,
      body: JSON.stringify({ refreshToken }),
    });
  },

  me(token) {
    return request('/api/auth/me', {
      method: 'GET',
      token,
    });
  },

  updateProfile(token, payload) {
    return request('/api/auth/me', {
      method: 'PUT',
      token,
      body: JSON.stringify(payload),
    });
  },
};
