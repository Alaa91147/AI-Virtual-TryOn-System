import { request } from './authService.js';

export const customerService = {
  getAll(token) {
    return request('/api/admin/customers', { method: 'GET', token });
  },
  updateRole(token, id, role) {
    return request(`/api/admin/customers/${id}/role`, {
      method: 'PUT', token, body: JSON.stringify({ role }),
    });
  },
  sendNotification(token, id, title, message) {
    return request(`/api/admin/customers/${id}/notifications`, {
      method: 'POST', token, body: JSON.stringify({ title, message }),
    });
  },
};
