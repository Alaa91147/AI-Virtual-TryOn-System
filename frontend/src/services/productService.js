import { request } from './authService.js';

export const productService = {
  getById(token, productId) {
    return request(
      `/api/products/${encodeURIComponent(productId)}`,
      {
        method: 'GET',
        token,
      },
    );
  },

  getRecentlyViewed(token, limit = 12) {
    const safeLimit = Math.min(
      Math.max(Number(limit) || 12, 1),
      30,
    );

    return request(
      `/api/products/recently-viewed?limit=${safeLimit}`,
      {
        method: 'GET',
        token,
      },
    );
  },
};