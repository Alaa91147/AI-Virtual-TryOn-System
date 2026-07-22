import { request } from './authService.js';

export const cartService = {
  get(token) {
    return request('/api/cart', {
      method: 'GET',
      token,
    });
  },

  add(token, productSizeId, quantity = 1) {
    return request('/api/cart', {
      method: 'POST',
      token,
      body: JSON.stringify({
        productSizeId,
        quantity,
      }),
    });
  },

  update(token, cartItemId, quantity) {
    return request(
      `/api/cart/${encodeURIComponent(cartItemId)}`,
      {
        method: 'PUT',
        token,
        body: JSON.stringify({ quantity }),
      },
    );
  },

  remove(token, cartItemId) {
    return request(
      `/api/cart/${encodeURIComponent(cartItemId)}`,
      {
        method: 'DELETE',
        token,
      },
    );
  },

  clear(token) {
    return request('/api/cart', {
      method: 'DELETE',
      token,
    });
  },
};