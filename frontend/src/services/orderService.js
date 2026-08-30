import { request } from './authService.js';

export const orderService = {
  checkout(token) { return request('/api/orders/checkout', { method: 'POST', token }); },
  mine(token) { return request('/api/orders/mine', { method: 'GET', token }); },
  getAll(token) { return request('/api/admin/orders', { method: 'GET', token }); },
  updateStatus(token, id, status, trackingNumber = '', shippingCarrier = '', fulfillmentNotes = '') {
    return request(`/api/admin/orders/${id}/status`, {
      method: 'PUT', token, body: JSON.stringify({ status, trackingNumber: trackingNumber || null,
        shippingCarrier: shippingCarrier || null, fulfillmentNotes: fulfillmentNotes || null }),
    });
  },
};
