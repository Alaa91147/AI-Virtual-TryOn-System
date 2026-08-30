import { request } from './authService.js';

export const adminManagementService = {
  getCategories(token) {
    return request('/api/admin/categories', {
      method: 'GET',
      token,
    });
  },

  createCategory(token, payload) {
    return request('/api/admin/categories', {
      method: 'POST',
      token,
      body: JSON.stringify(payload),
    });
  },

  updateCategory(token, id, payload) {
    return request(
      `/api/admin/categories/${id}`,
      {
        method: 'PUT',
        token,
        body: JSON.stringify(payload),
      },
    );
  },

  deleteCategory(token, id) {
    return request(
      `/api/admin/categories/${id}`,
      {
        method: 'DELETE',
        token,
      },
    );
  },

  getAuditLogs(token, limit = 100) {
    return request(
      `/api/admin/audit-logs?limit=${limit}`,
      {
        method: 'GET',
        token,
      },
    );
  },

  getDashboard(token, from, to) {
    const parameters = new URLSearchParams({
      from,
      to,
    });

    return request(
      `/api/admin/dashboard?${parameters.toString()}`,
      {
        method: 'GET',
        token,
      },
    );
  },

  getLoginAttempts(token, limit = 200) {
    return request(
      `/api/admin/security/login-attempts?limit=${limit}`,
      {
        method: 'GET',
        token,
      },
    );
  },
};