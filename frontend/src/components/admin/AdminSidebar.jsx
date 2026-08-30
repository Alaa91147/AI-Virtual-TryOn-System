import { useState } from 'react';

import {
  NavLink,
  useNavigate,
} from 'react-router-dom';

import {
  Bell,
  Box,
  CircleDollarSign,
  History,
  LayoutDashboard,
  LogOut,
  Megaphone,
  Menu,
  ScrollText,
  ShieldCheck,
  ShoppingBag,
  Tags,
  Users,
  X,
} from 'lucide-react';

import { useAuth } from '../../context/AuthContext.jsx';
import AdminNotifications from './AdminNotifications.jsx';

const links = [
  {
    to: '/admin',
    label: 'Dashboard',
    icon: LayoutDashboard,
    end: true,
    section: 'WORKSPACE',
  },
  {
    to: '/admin/products',
    label: 'Products',
    icon: ShoppingBag,
  },
  {
    to: '/admin/orders',
    label: 'Orders',
    icon: Box,
  },
  {
    to: '/admin/customers',
    label: 'Customers',
    icon: Users,
  },
  {
    to: '/admin/promotions',
    label: 'Promotions',
    icon: CircleDollarSign,
    section: 'MANAGE',
  },
  {
    to: '/admin/broadcast',
    label: 'Broadcast',
    icon: Megaphone,
  },
  {
    to: '/admin/categories',
    label: 'Categories',
    icon: Tags,
  },
  {
    to: '/admin/notifications',
    label: 'Notifications',
    icon: Bell,
  },
  {
    to: '/admin/inventory-history',
    label: 'Inventory history',
    icon: History,
    section: 'SECURITY & DATA',
  },
  {
    to: '/admin/audit-logs',
    label: 'Audit logs',
    icon: ScrollText,
  },
  {
    to: '/admin/security',
    label: 'Login security',
    icon: ShieldCheck,
  },
  {
    to: '/admin/customer-controls',
    label: 'Account controls',
    icon: Users,
  },
];

export default function AdminSidebar({
  user,
  counts = {},
}) {
  const navigate = useNavigate();
  const { logout, token } = useAuth();

  const [mobileOpen, setMobileOpen] =
    useState(false);

  const [accountOpen, setAccountOpen] =
    useState(false);

  async function handleLogout(event) {
    event?.stopPropagation();

    await logout();

    navigate('/auth/login', {
      replace: true,
    });
  }

  return (
    <>
      <button
        className="admin-mobile-menu"
        type="button"
        onClick={() => setMobileOpen(true)}
        aria-label="Open admin navigation"
      >
        <Menu size={21} />
      </button>

      {mobileOpen ? (
        <button
          className="admin-sidebar-backdrop"
          type="button"
          onClick={() => setMobileOpen(false)}
          aria-label="Close admin navigation"
        />
      ) : null}

      <aside
        className={
          mobileOpen
            ? 'admin-sidebar is-mobile-open'
            : 'admin-sidebar'
        }
      >
        <button
          className="admin-sidebar-close"
          type="button"
          onClick={() => setMobileOpen(false)}
          aria-label="Close menu"
        >
          <X size={19} />
        </button>

        <NavLink
          className="admin-brand"
          to="/admin"
          onClick={() => setMobileOpen(false)}
        >
          <span>V</span>

          <div>
            VIRTUAL
            <strong>TRY-ON</strong>
          </div>
        </NavLink>

        <nav aria-label="Admin navigation">
          {links.map(
            ({
              to,
              label,
              icon: Icon,
              end,
              section,
            }) => (
              <div
                className="admin-nav-entry"
                key={to}
              >
                {section ? (
                  <small>{section}</small>
                ) : null}

                <NavLink
                  to={to}
                  end={end}
                  onClick={() =>
                    setMobileOpen(false)
                  }
                  className={({ isActive }) =>
                    isActive
                      ? 'is-active'
                      : undefined
                  }
                >
                  <Icon size={19} />

                  <span>{label}</span>

                  {counts[to] !== undefined ? (
                    <b>{counts[to]}</b>
                  ) : null}
                </NavLink>
              </div>
            ),
          )}
        </nav>

        <div className="admin-sidebar-notifications">
          <AdminNotifications token={token} />
        </div>
        <div className="admin-sidebar-account-area">
          {accountOpen ? (
            <div className="admin-account-menu">
              <strong>
                Administrator account
              </strong>

              <small>{user?.email}</small>

              <button
                type="button"
                onClick={handleLogout}
              >
                <LogOut size={16} />
                Sign out
              </button>
            </div>
          ) : null}

          <div
            className="admin-account"
            role="button"
            tabIndex={0}
            onClick={() =>
              setAccountOpen(
                (current) => !current,
              )
            }
            onKeyDown={(event) => {
              if (
                event.key === 'Enter' ||
                event.key === ' '
              ) {
                setAccountOpen(
                  (current) => !current,
                );
              }
            }}
          >
            <div>
              {user?.fullName
                ?.slice(0, 2)
                .toUpperCase() || 'AD'}
            </div>

            <span>
              <strong>
                {user?.fullName || 'Admin'}
              </strong>

              <small>{user?.email}</small>
            </span>

            <button
              type="button"
              className="admin-logout-button"
              onClick={handleLogout}
              title="Sign out"
              aria-label="Sign out"
            >
              <LogOut size={18} />
            </button>
          </div>
        </div>
      </aside>
    </>
  );
}


