import {
  HubConnectionBuilder,
  LogLevel,
} from '@microsoft/signalr';

import { request } from './authService.js';

const API_BASE_URL =
  import.meta.env.VITE_API_BASE_URL ||
  'http://localhost:5113';

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
      `/api/notifications/${encodeURIComponent(
        notificationId,
      )}/read`,
      {
        method: 'PUT',
        token,
      },
    );
  },

  markAllRead(token) {
    return request(
      '/api/notifications/read-all',
      {
        method: 'PUT',
        token,
      },
    );
  },

  broadcast(token, payload) {
    return request(
      '/api/admin/notifications/broadcast',
      {
        method: 'POST',
        token,
        body: JSON.stringify(payload),
      },
    );
  },

  async connect(token, onNotification) {
    if (!token) {
      return null;
    }

    const connection =
      new HubConnectionBuilder()
        .withUrl(
          `${API_BASE_URL}/hubs/notifications`,
          {
            accessTokenFactory: () => token,
          },
        )
        .withAutomaticReconnect([
          0,
          2000,
          5000,
          10000,
          30000,
        ])
        .configureLogging(LogLevel.Warning)
        .build();

    connection.on(
      'notificationReceived',
      (notification) => {
        if (
          typeof onNotification === 'function'
        ) {
          onNotification(notification);
        }
      },
    );

    connection.on(
      'notificationConnectionReady',
      () => {
        window.dispatchEvent(
          new Event(
            'notification-connection-ready',
          ),
        );
      },
    );

    connection.onreconnecting(() => {
      window.dispatchEvent(
        new Event(
          'notification-connection-reconnecting',
        ),
      );
    });

    connection.onreconnected(() => {
      window.dispatchEvent(
        new Event(
          'notification-connection-ready',
        ),
      );
    });

    connection.onclose(() => {
      window.dispatchEvent(
        new Event(
          'notification-connection-closed',
        ),
      );
    });

    await connection.start();

    return connection;
  },

  async disconnect(connection) {
    if (!connection) {
      return;
    }

    connection.off(
      'notificationReceived',
    );

    connection.off(
      'notificationConnectionReady',
    );

    try {
      await connection.stop();
    } catch {
      // The connection may already be stopped.
    }
  },
};