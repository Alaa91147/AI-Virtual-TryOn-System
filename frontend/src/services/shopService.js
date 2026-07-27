import { request } from './authService.js';

export const shopService = {
  getHome(token) {
    return request('/api/shop/home', {
      method: 'GET',
      token,
    });
  },
};