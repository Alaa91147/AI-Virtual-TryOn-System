import { request } from './authService.js';

export const customerService = {
  getAll(token) {
    return request(
      '/api/admin/customers',
      {
        method: 'GET',
        token,
      }
    );
  },

  updateRole(token, id, role) {
    return request(
      `/api/admin/customers/${id}/role`,
      {
        method: 'PUT',
        token,
        body: JSON.stringify({ role }),
      }
    );
  },

  sendNotification(token, id, title, message) {
    return request(
      `/api/admin/customers/${id}/notifications`,
      {
        method: 'POST',
        token,
        body: JSON.stringify({
          title,
          message,
        }),
      }
    );
  },

  updateAdministration(token, id, payload) {
    return request(
      `/api/admin/customers/${id}/administration`,
      {
        method: 'PUT',
        token,
        body: JSON.stringify(payload),
      }
    );
  },

  resendVerification(token, id) {
    return request(
      `/api/admin/customers/${id}/resend-verification`,
      {
        method: 'POST',
        token,
      }
    );
  },

  verifyManually(token, id, reason) {
    return request(
      `/api/admin/customers/${id}/verification`,
      {
        method: 'PUT',
        token,
        body: JSON.stringify({ reason }),
      }
    );
  },};

