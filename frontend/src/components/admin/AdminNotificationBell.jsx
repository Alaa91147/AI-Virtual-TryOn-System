import { useLocation } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext.jsx';
import NotificationDropdown from '../shop/NotificationDropdown.jsx';

export default function AdminNotificationBell() {
  const location = useLocation();
  const { token, isAuthenticated, user } = useAuth();

  const pagesWithoutTopbarBell = new Set([
    '/admin/broadcast',
    '/admin/categories',
    '/admin/audit-logs',
  ]);

  if (!pagesWithoutTopbarBell.has(location.pathname) || !isAuthenticated || user?.role !== 'Admin') {
    return null;
  }

  return (
    <div className="admin-global-notification" aria-label="Admin notifications">
      <NotificationDropdown token={token} />
    </div>
  );
}
