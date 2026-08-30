import NotificationDropdown from '../shop/NotificationDropdown.jsx';

export default function AdminNotifications({ token }) {
  return <div className="admin-notifications"><NotificationDropdown token={token} /></div>;
}
