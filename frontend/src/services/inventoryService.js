import { request } from './authService.js';

export const inventoryService = {
  getVariants(token) {
    return request(
      '/api/admin/variant-inventory/variants',
      { method: 'GET', token }
    );
  },

  synchronize(token) {
    return request(
      '/api/admin/variant-inventory/synchronize',
      { method: 'POST', token }
    );
  },

  getHistory(token, limit = 200) {
    return request(
      `/api/admin/variant-inventory/history?limit=${limit}`,
      { method: 'GET', token }
    );
  },

  adjust(token, variantId, payload) {
    return request(
      `/api/admin/variant-inventory/variants/${variantId}/adjust`,
      {
        method: 'POST',
        token,
        body: JSON.stringify(payload),
      }
    );
  },
};
