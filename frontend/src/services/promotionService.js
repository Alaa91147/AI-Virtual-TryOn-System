import { request } from './authService.js';

export const promotionService = {
  getAll(token) { return request('/api/admin/promotions', { method: 'GET', token }); },
  create(token, payload) { return request('/api/admin/promotions', { method: 'POST', token, body: JSON.stringify(payload) }); },
  send(token, id) { return request(`/api/admin/promotions/${id}/send-email`, { method: 'POST', token }); },
  deactivate(token, id) { return request(`/api/admin/promotions/${id}/deactivate`, { method: 'POST', token }); },
  reactivate(token, id) { return request(`/api/admin/promotions/${id}/reactivate`, { method: 'POST', token }); },
  update(token, id, payload) { return request(`/api/admin/promotions/${id}`, { method: 'PUT', token, body: JSON.stringify(payload) }); },
};
