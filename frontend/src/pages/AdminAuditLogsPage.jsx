import { Fragment } from 'react';
import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react';

import {
  ChevronDown,
  ChevronUp,
  RefreshCw,
  Search,
} from 'lucide-react';

import AdminSidebar from '../components/admin/AdminSidebar.jsx';
import { useAuth } from '../context/AuthContext.jsx';
import { getErrorMessage } from '../services/authService.js';
import { adminManagementService } from '../services/adminManagementService.js';

import './AdminProductsPage.css';
import './AdminManagement.css';

const actionLabels = {
  Create: 'Created',
  Update: 'Updated',
  Delete: 'Deleted',
  Deactivate: 'Deactivated',
  Reactivate: 'Reactivated',
  Broadcast: 'Notification broadcast',
  UpdateStatus: 'Order status updated',
  AdjustInventory: 'Inventory adjusted',
  AdjustVariantInventory: 'Variant stock adjusted',
  SynchronizeVariants: 'Product variants synchronized',
  SuspendCustomer: 'Customer suspended',
  ReactivateCustomer: 'Customer reactivated',
  UpdateCustomerAdministration:
    'Customer controls updated',
  RevokeSessions: 'Customer sessions revoked',
  UpdateRole: 'Customer role updated',
};

const entityLabels = {
  ProductSize: 'Product size',
  ProductVariant: 'Product variant',
  Promotion: 'Promotion',
  Notification: 'Notification',
  Order: 'Order',
  Customer: 'Customer account',
  User: 'User account',
  Category: 'Category',
  Product: 'Product',
};

function friendlyAction(value) {
  return actionLabels[value] || value || 'Administrative action';
}

function friendlyEntity(value) {
  return entityLabels[value] || value || 'System';
}

function friendlyIp(value) {
  if (!value) {
    return 'Not recorded';
  }

  if (
    value === '::1' ||
    value === '127.0.0.1' ||
    value === '::ffff:127.0.0.1'
  ) {
    return 'Local computer';
  }

  return value.replace('::ffff:', '');
}

function friendlyDetails(value) {
  if (!value) {
    return 'No additional description was recorded.';
  }

  return value
    .replaceAll(' -> ', ' → ')
    .replaceAll('Status=', 'Status: ')
    .replaceAll('Tracking=', 'Tracking: ')
    .replaceAll('SKU=', 'SKU: ')
    .replaceAll('Stock ', 'Stock changed from ')
    .replaceAll('; Reason=', ' — Reason: ')
    .replaceAll('; ', ' · ');
}

export default function AdminAuditLogsPage() {
  const { token, user } = useAuth();

  const [logs, setLogs] = useState([]);
  const [query, setQuery] = useState('');
  const [action, setAction] = useState('All');
  const [expandedId, setExpandedId] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError('');

      const result =
        await adminManagementService.getAuditLogs(
          token,
          200
        );

      setLogs(Array.isArray(result) ? result : []);
    } catch (requestError) {
      setError(
        getErrorMessage(
          requestError,
          'Audit logs could not be loaded.'
        )
      );
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    load();
  }, [load]);

  const actions = useMemo(
    () => [
      'All',
      ...new Set(
        logs.map((log) => friendlyAction(log.action))
      ),
    ],
    [logs]
  );

  const visible = useMemo(() => {
    const normalizedQuery =
      query.trim().toLowerCase();

    return logs.filter((log) => {
      const readableAction =
        friendlyAction(log.action);

      const matchesAction =
        action === 'All' ||
        readableAction === action;

      const searchableText = [
        readableAction,
        friendlyEntity(log.entityType),
        log.details,
        log.administratorName,
        log.administratorEmail,
        friendlyIp(log.ipAddress),
      ]
        .filter(Boolean)
        .join(' ')
        .toLowerCase();

      return (
        matchesAction &&
        searchableText.includes(normalizedQuery)
      );
    });
  }, [logs, query, action]);

  return (
    <div className="admin-shell">
      <AdminSidebar user={user} />

      <main className="admin-main">
        <header className="admin-topbar">
          <div>
            <span>Security</span>
            <b>/</b>
            <strong>Audit logs</strong>
          </div>
        </header>

        <div className="admin-content">
          <section className="admin-page-title">
            <div>
              <span className="admin-eyebrow">
                ADMINISTRATIVE ACTIVITY
              </span>

              <h1>Audit logs</h1>

              <p>
                Review sensitive actions performed by
                administrators.
              </p>
            </div>

            <button
              type="button"
              className="admin-secondary-btn"
              onClick={load}
              disabled={loading}
            >
              <RefreshCw
                size={16}
                className={loading ? 'spin' : ''}
              />

              Refresh
            </button>
          </section>

          {error ? (
            <div className="admin-notice">
              {error}
            </div>
          ) : null}

          <section className="admin-products-panel audit-panel">
            <div className="admin-panel-toolbar">
              <strong>{visible.length} events</strong>

              <div className="admin-toolbar-actions">
                <select
                  value={action}
                  onChange={(event) =>
                    setAction(event.target.value)
                  }
                >
                  {actions.map((value) => (
                    <option key={value}>
                      {value}
                    </option>
                  ))}
                </select>

                <label>
                  <Search size={17} />

                  <input
                    value={query}
                    onChange={(event) =>
                      setQuery(event.target.value)
                    }
                    placeholder="Action, administrator or item"
                  />
                </label>
              </div>
            </div>

            <div className="admin-table-wrap">
              <table className="admin-products-table audit-table">
                <thead>
                  <tr>
                    <th>Date and time</th>
                    <th>Action</th>
                    <th>Item</th>
                    <th>Administrator</th>
                    <th>Description</th>
                    <th>Location</th>
                    <th />
                  </tr>
                </thead>

                <tbody>
                  {loading ? (
                    <tr>
                      <td
                        colSpan="7"
                        className="admin-empty"
                      >
                        Loading audit history…
                      </td>
                    </tr>
                  ) : visible.length === 0 ? (
                    <tr>
                      <td
                        colSpan="7"
                        className="admin-empty"
                      >
                        No audit events found.
                      </td>
                    </tr>
                  ) : (
                    visible.map((log) => {
                      const expanded =
                        expandedId === log.id;

                      return (
                        
                          <Fragment key={log.id}>
                          <tr>
                            <td>
                              <strong>
                                {new Date(
                                  log.createdAt
                                ).toLocaleDateString()}
                              </strong>

                              <small>
                                {new Date(
                                  log.createdAt
                                ).toLocaleTimeString()}
                              </small>
                            </td>

                            <td>
                              <span className="audit-action">
                                {friendlyAction(
                                  log.action
                                )}
                              </span>
                            </td>

                            <td>
                              <strong>
                                {friendlyEntity(
                                  log.entityType
                                )}
                              </strong>
                            </td>

                            <td>
                              <div className="audit-admin">
                                <span>
                                  {log.administratorName
                                    ?.slice(0, 2)
                                    .toUpperCase() ||
                                    'AD'}
                                </span>

                                <div>
                                  <strong>
                                    {log.administratorName ||
                                      'Administrator'}
                                  </strong>

                                  <small>
                                    {
                                      log.administratorEmail
                                    }
                                  </small>
                                </div>
                              </div>
                            </td>

                            <td>
                              <span>
                                {friendlyDetails(
                                  log.details
                                )}
                              </span>
                            </td>

                            <td>
                              <small>
                                {friendlyIp(
                                  log.ipAddress
                                )}
                              </small>
                            </td>

                            <td>
                              <button
                                type="button"
                                className="audit-details-button"
                                onClick={() =>
                                  setExpandedId(
                                    expanded
                                      ? null
                                      : log.id
                                  )
                                }
                                aria-label="Toggle technical details"
                              >
                                {expanded ? (
                                  <ChevronUp
                                    size={16}
                                  />
                                ) : (
                                  <ChevronDown
                                    size={16}
                                  />
                                )}
                              </button>
                            </td>
                          </tr>

                          {expanded ? (
                            <tr
                              key={`${log.id}-details`}
                              className="audit-technical-row"
                            >
                              <td colSpan="7">
                                <strong>
                                  Technical reference
                                </strong>

                                <div>
                                  <span>
                                    Event ID: {log.id}
                                  </span>

                                  <span>
                                    Administrator ID:{' '}
                                    {log.adminUserId}
                                  </span>

                                  <span>
                                    Entity ID:{' '}
                                    {log.entityId ||
                                      'Not available'}
                                  </span>
                                </div>
                              </td>
                            </tr>
                          ) : null}
                        </Fragment>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </div>
      </main>
    </div>
  );
}




