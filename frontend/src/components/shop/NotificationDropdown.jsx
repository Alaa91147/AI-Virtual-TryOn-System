import {
  useEffect,
  useRef,
  useState,
} from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Bell,
  CheckCheck,
  Sparkles,
} from 'lucide-react';

import { notificationService } from '../../services/notificationService.js';

const emptyData = {
  items: [],
  unreadCount: 0,
};

export default function NotificationDropdown({
  token,
}) {
  const navigate = useNavigate();
  const containerRef = useRef(null);

  const [data, setData] = useState(emptyData);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

 useEffect(() => {
  let cancelled = false;

  async function loadNotifications(
    showLoading = true,
  ) {
    if (!token) {
      setData(emptyData);
      setLoading(false);
      return;
    }

    try {
      if (showLoading) {
        setLoading(true);
      }

      setError('');

      const response =
        await notificationService.get(token);

      if (!cancelled) {
        setData(response);
      }
    } catch {
      if (!cancelled) {
        setError(
          'Could not load notifications.',
        );
      }
    } finally {
      if (!cancelled && showLoading) {
        setLoading(false);
      }
    }
  }

  function refreshNotifications() {
    loadNotifications(false);
  }

  loadNotifications();

  window.addEventListener(
    'notification-updated',
    refreshNotifications,
  );

  // ProductDetailsPage already sends this event
  // immediately after adding to the cart.
  window.addEventListener(
    'cart-updated',
    refreshNotifications,
  );

  return () => {
    cancelled = true;

    window.removeEventListener(
      'notification-updated',
      refreshNotifications,
    );

    window.removeEventListener(
      'cart-updated',
      refreshNotifications,
    );
  };
}, [token]);


  useEffect(() => {
    function handleOutsideClick(event) {
      if (
        containerRef.current &&
        !containerRef.current.contains(
          event.target,
        )
      ) {
        setOpen(false);
      }
    }

    document.addEventListener(
      'mousedown',
      handleOutsideClick,
    );

    return () => {
      document.removeEventListener(
        'mousedown',
        handleOutsideClick,
      );
    };
  }, []);

  async function openNotification(notification) {
    if (!notification.isRead) {
      setData((current) => ({
        items: current.items.map((item) =>
          item.id === notification.id
            ? {
                ...item,
                isRead: true,
                readAt: new Date().toISOString(),
              }
            : item,
        ),
        unreadCount: Math.max(
          0,
          current.unreadCount - 1,
        ),
      }));

      try {
        await notificationService.markRead(
          token,
          notification.id,
        );
      } catch {
        // The next API refresh restores server state.
      }
    }

    setOpen(false);

    if (notification.link) {
      navigate(notification.link);
    }
  }

  async function markAllRead() {
    const previous = data;

    setData((current) => ({
      items: current.items.map((item) => ({
        ...item,
        isRead: true,
        readAt:
          item.readAt ||
          new Date().toISOString(),
      })),
      unreadCount: 0,
    }));

    try {
      await notificationService.markAllRead(
        token,
      );
    } catch {
      setData(previous);
      setError(
        'Could not update notifications.',
      );
    }
  }

  function formatTime(value) {
    const createdAt = new Date(value);
    const difference =
      Date.now() - createdAt.getTime();

    const minutes = Math.floor(
      difference / 60000,
    );

    if (minutes < 1) {
      return 'Just now';
    }

    if (minutes < 60) {
      return `${minutes}m ago`;
    }

    const hours = Math.floor(minutes / 60);

    if (hours < 24) {
      return `${hours}h ago`;
    }

    const days = Math.floor(hours / 24);

    if (days < 7) {
      return `${days}d ago`;
    }

    return createdAt.toLocaleDateString();
  }

  return (
    <div
      className="notification-dropdown"
      ref={containerRef}
    >
      <button
        className="notification-trigger"
        type="button"
        aria-label={`Notifications, ${data.unreadCount} unread`}
        aria-expanded={open}
        onClick={() =>
          setOpen((current) => !current)
        }
      >
        <Bell size={21} />

        {data.unreadCount > 0 ? (
          <span>
            {data.unreadCount > 9
              ? '9+'
              : data.unreadCount}
          </span>
        ) : null}
      </button>

      {open ? (
        <section className="notification-panel">
          <header>
            <div>
              <p>Style updates</p>
              <h2>Notifications</h2>
            </div>

            {data.unreadCount > 0 ? (
              <button
                type="button"
                onClick={markAllRead}
              >
                <CheckCheck size={15} />
                Mark all read
              </button>
            ) : null}
          </header>

          <div className="notification-list">
            {loading ? (
              <div className="notification-state">
                <span className="spinner small" />
                Loading...
              </div>
            ) : error ? (
              <div className="notification-state is-error">
                {error}
              </div>
            ) : data.items.length ? (
              data.items.map((notification) => (
                <button
                  className={
                    notification.isRead
                      ? 'notification-item'
                      : 'notification-item is-unread'
                  }
                  type="button"
                  key={notification.id}
                  onClick={() =>
                    openNotification(
                      notification,
                    )
                  }
                >
                  <span className="notification-icon">
                    <Sparkles size={17} />
                  </span>

                  <span className="notification-copy">
                    <strong>
                      {notification.title}
                    </strong>

                    <span>
                      {notification.message}
                    </span>

                    <small>
                      {formatTime(
                        notification.createdAt,
                      )}
                    </small>
                  </span>

                  {!notification.isRead ? (
                    <span className="notification-unread-dot" />
                  ) : null}
                </button>
              ))
            ) : (
              <div className="notification-state">
                <Bell size={26} />
                <strong>You’re all caught up</strong>
                <span>
                  New style updates will appear here.
                </span>
              </div>
            )}
          </div>
        </section>
      ) : null}
    </div>
  );
}