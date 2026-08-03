import { request } from './authService.js';

export const adminProductService = {
  getCatalog(token) {
    return request('/api/admin/products', {
      method: 'GET',
      token,
    });
  },

  create(token, product) {
    return request('/api/admin/products', {
      method: 'POST',
      token,
      body: JSON.stringify(product),
    });
  },

  update(token, productId, product) {
    return request(
      `/api/admin/products/${encodeURIComponent(productId)}`,
      {
        method: 'PUT',
        token,
        body: JSON.stringify(product),
      },
    );
  },

  uploadImage(token, file) {
    const body = new FormData();
    body.append('image', file);
    return request('/api/admin/products/image', {
      method: 'POST',
      token,
      body,
    });
  },
};
