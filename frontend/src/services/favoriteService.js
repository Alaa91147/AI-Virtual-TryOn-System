import { request } from './authService.js';

export const favoriteService = {
  getAll(token) {
    return request('/api/favorites', {
      method: 'GET',
      token,
    });
  },

  add(token, productId) {
    return request(
      `/api/favorites/${productId}`,
      {
        method: 'POST',
        token,
      },
    );
  },

  remove(token, productId) {
    return request(
      `/api/favorites/${productId}`,
      {
        method: 'DELETE',
        token,
      },
    );
  },
};