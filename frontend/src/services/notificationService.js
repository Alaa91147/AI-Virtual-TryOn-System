import { request } from './authService.js';

export const notificationService = {
  get(token, limit = 20) {
    return request(
      `/api/notifications?limit=${limit}`,
      {
        method: 'GET',
        token,
      },
    );
  },

  markRead(token, notificationId) {
    return request(
      `/api/notifications/${encodeURIComponent(notificationId)}/read`,
      {
        method: 'PUT',
        token,
      },
    );
  },

  markAllRead(token) {
    return request('/api/notifications/read-all', {
      method: 'PUT',
      token,
    });
  },
};