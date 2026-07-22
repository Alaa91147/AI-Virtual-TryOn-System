import { request } from './authService.js';

export const reviewService = {
  get(token, productId) {
    return request(
      `/api/products/${encodeURIComponent(productId)}/reviews`,
      {
        method: 'GET',
        token,
      },
    );
  },

  create(token, productId, payload) {
    return request(
      `/api/products/${encodeURIComponent(productId)}/reviews`,
      {
        method: 'POST',
        token,
        body: JSON.stringify(payload),
      },
    );
  },

  update(token, productId, reviewId, payload) {
    return request(
      `/api/products/${encodeURIComponent(productId)}` +
        `/reviews/${encodeURIComponent(reviewId)}`,
      {
        method: 'PUT',
        token,
        body: JSON.stringify(payload),
      },
    );
  },

  remove(token, productId, reviewId) {
    return request(
      `/api/products/${encodeURIComponent(productId)}` +
        `/reviews/${encodeURIComponent(reviewId)}`,
      {
        method: 'DELETE',
        token,
      },
    );
  },
};