import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from 'react';

import {
  Check,
  RefreshCw,
  Search,
  ShieldCheck,
  X,
} from 'lucide-react';

import AdminSidebar from '../components/admin/AdminSidebar.jsx';
import { useAuth } from '../context/AuthContext.jsx';
import { getErrorMessage } from '../services/authService.js';
import { customerService } from '../services/customerService.js';

import './AdminProductsPage.css';
import './AdminManagement.css';

export default function AdminCustomerControlsPage() {
  const { token, user } = useAuth();

  const [customers, setCustomers] = useState([]);
  const [query, setQuery] = useState('');
  const [filter, setFilter] = useState('All');
  const [draft, setDraft] = useState(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const load = useCallback(async () => {
    try {
      setLoading(true);
      setError('');

      setCustomers(
        await customerService.getAll(token)
      );
    } catch (requestError) {
      setError(
        getErrorMessage(
          requestError,
          'Accounts could not be loaded.'
        )
      );
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => {
    load();
  }, [load]);

  const visible = useMemo(() => {
    const text = query.trim().toLowerCase();

    return customers.filter((customer) => {
      const matchesSearch =
        `${customer.fullName} ${customer.email} ` +
        `${customer.adminNotes || ''}`
          .toLowerCase()
          .includes(text);

      const matchesFilter =
        filter === 'All' ||
        (filter === 'Active' &&
          !customer.isSuspended) ||
        (filter === 'Suspended' &&
          customer.isSuspended) ||
        (filter === 'Unverified' &&
          !customer.isEmailVerified);

      return matchesSearch && matchesFilter;
    });
  }, [customers, query, filter]);

  function manage(customer) {
    setError('');
    setSuccess('');

    setDraft({
      id: customer.id,
      fullName: customer.fullName,
      email: customer.email,
      isEmailVerified:
        customer.isEmailVerified,
      verificationReason: '',
      isSuspended: customer.isSuspended,
      suspensionReason:
        customer.suspensionReason || '',
      suspendedUntil: customer.suspendedUntil
        ? new Date(customer.suspendedUntil)
            .toISOString()
            .slice(0, 16)
        : '',
      adminNotes: customer.adminNotes || '',
      revokeSessions: false,
    });
  }

  async function resendVerification() {
    try {
      setSaving(true);
      setError('');
      setSuccess('');

      const response =
        await customerService
          .resendVerification(
            token,
            draft.id
          );

      setSuccess(
        response?.message ||
          'Verification email sent.'
      );
    } catch (actionError) {
      setError(
        getErrorMessage(
          actionError,
          'Could not send verification email.'
        )
      );
    } finally {
      setSaving(false);
    }
  }

  async function verifyManually() {
    const reason =
      draft.verificationReason.trim();

    if (reason.length < 5) {
      setError(
        'Enter a clear manual verification reason.'
      );
      return;
    }

    const confirmed = window.confirm(
      'Manually verify this email address? ' +
        'This bypasses customer email ownership proof.'
    );

    if (!confirmed) {
      return;
    }

    try {
      setSaving(true);
      setError('');
      setSuccess('');

      const response =
        await customerService.verifyManually(
          token,
          draft.id,
          reason
        );

      setSuccess(
        response?.message ||
          'Customer verified manually.'
      );

      await load();
      setDraft(null);
    } catch (actionError) {
      setError(
        getErrorMessage(
          actionError,
          'Could not verify this customer.'
        )
      );
    } finally {
      setSaving(false);
    }
  }
  async function save(event) {
    event.preventDefault();

    if (
      draft.isSuspended &&
      !draft.suspensionReason.trim()
    ) {
      setError(
        'Suspension reason is required.'
      );
      return;
    }

    try {
      setSaving(true);
      setError('');
      setSuccess('');

      const saved =
        await customerService.updateAdministration(
          token,
          draft.id,
          {
            isSuspended: draft.isSuspended,
            suspensionReason: draft.isSuspended
              ? draft.suspensionReason.trim()
              : null,
            suspendedUntil:
              draft.isSuspended &&
              draft.suspendedUntil
                ? new Date(
                    draft.suspendedUntil
                  ).toISOString()
                : null,
            adminNotes:
              draft.adminNotes.trim() || null,
            revokeSessions:
              draft.revokeSessions,
          }
        );

      setCustomers((current) =>
        current.map((customer) =>
          customer.id === saved.id
            ? saved
            : customer
        )
      );

      setSuccess(
        saved.isSuspended
          ? `${saved.fullName} was suspended.`
          : `${saved.fullName} was updated.`
      );

      setDraft(null);
    } catch (requestError) {
      setError(
        getErrorMessage(
          requestError,
          'Account controls could not be saved.'
        )
      );
    } finally {
      setSaving(false);
    }
  }

  const activeCount = customers.filter(
    (customer) =>
      customer.role === 'Customer' &&
      !customer.isSuspended
  ).length;

  const suspendedCount = customers.filter(
    (customer) => customer.isSuspended
  ).length;

  const unverifiedCount = customers.filter(
    (customer) => !customer.isEmailVerified
  ).length;

  return (
    <div className="admin-shell">
      <AdminSidebar user={user} />

      <main className="admin-main">
        <header className="admin-topbar">
          <div>
            <span>Security</span>
            <b>/</b>
            <strong>Account controls</strong>
          </div>
        </header>

        <div className="admin-content">
          <section className="admin-page-title">
            <div>
              <span className="admin-eyebrow">
                CUSTOMER SECURITY
              </span>

              <h1>Account controls</h1>

              <p>
                Suspend accounts, revoke sessions,
                and maintain private notes.
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

          {success ? (
            <div className="inventory-success">
              <Check size={17} />
              {success}
            </div>
          ) : null}

          <section className="account-summary">
            <article>
              <small>Active customers</small>
              <strong>{activeCount}</strong>
            </article>

            <article>
              <small>Suspended</small>
              <strong>{suspendedCount}</strong>
            </article>

            <article>
              <small>Unverified</small>
              <strong>{unverifiedCount}</strong>
            </article>
          </section>

          <section className="admin-products-panel">
            <div className="admin-panel-toolbar">
              <strong>
                {visible.length} accounts
              </strong>

              <div className="admin-toolbar-actions">
                <select
                  value={filter}
                  onChange={(event) =>
                    setFilter(event.target.value)
                  }
                >
                  <option>All</option>
                  <option>Active</option>
                  <option>Suspended</option>
                  <option>Unverified</option>
                </select>

                <label>
                  <Search size={17} />

                  <input
                    value={query}
                    onChange={(event) =>
                      setQuery(event.target.value)
                    }
                    placeholder="Search accounts"
                  />
                </label>
              </div>
            </div>

            <div className="admin-table-wrap">
              <table className="admin-products-table">
                <thead>
                  <tr>
                    <th>Customer</th>
                    <th>Verification</th>
                    <th>Status</th>
                    <th>Suspension reason</th>
                    <th>Private notes</th>
                    <th />
                  </tr>
                </thead>

                <tbody>
                  {loading ? (
                    <tr>
                      <td
                        colSpan="6"
                        className="admin-empty"
                      >
                        Loading accounts…
                      </td>
                    </tr>
                  ) : (
                    visible.map((customer) => (
                      <tr key={customer.id}>
                        <td>
                          <strong>
                            {customer.fullName}
                          </strong>
                          <small>
                            {customer.email}
                          </small>
                          <small>
                            {customer.role}
                          </small>
                        </td>

                        <td>
                          <strong>
                            {customer.isEmailVerified
                              ? 'Verified'
                              : 'Unverified'}
                          </strong>
                        </td>

                        <td>
                          <span
                            className={`admin-status ${
                              customer.isSuspended
                                ? 'is-hidden'
                                : 'is-active'
                            }`}
                          >
                            <i />
                            {customer.isSuspended
                              ? 'Suspended'
                              : 'Active'}
                          </span>

                          {customer.suspendedUntil ? (
                            <small>
                              Until{' '}
                              {new Date(
                                customer.suspendedUntil
                              ).toLocaleString()}
                            </small>
                          ) : null}
                        </td>

                        <td>
                          <span>
                            {customer.suspensionReason ||
                              '—'}
                          </span>
                        </td>

                        <td>
                          <span>
                            {customer.adminNotes ||
                              '—'}
                          </span>
                        </td>

                        <td>
                          <button
                            type="button"
                            className="admin-secondary-btn"
                            disabled={
                              customer.role === 'Admin'
                            }
                            onClick={() =>
                              manage(customer)
                            }
                          >
                            <ShieldCheck size={15} />
                            Manage
                          </button>
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </section>
        </div>
      </main>

      {draft ? (
        <div
          className="management-modal"
          onMouseDown={() => setDraft(null)}
        >
          <form
            className="account-control-modal"
            onSubmit={save}
            onMouseDown={(event) =>
              event.stopPropagation()
            }
          >
            <button
              type="button"
              className="modal-close"
              onClick={() => setDraft(null)}
            >
              <X size={18} />
            </button>

            <span className="admin-eyebrow">
              ACCOUNT ADMINISTRATION
            </span>

            <h2>{draft.fullName}</h2>
            <p>{draft.email}</p>

            {!draft.isEmailVerified ? (
              <section className="verification-control-panel">
                <div>
                  <strong>Email is unverified</strong>
                  <small>
                    Send the normal verification link or
                    use the audited manual override.
                  </small>
                </div>

                <button
                  type="button"
                  className="admin-secondary-btn"
                  disabled={saving}
                  onClick={resendVerification}
                >
                  Resend verification email
                </button>

                <label>
                  Manual verification reason
                  <textarea
                    rows="3"
                    maxLength="500"
                    value={
                      draft.verificationReason
                    }
                    placeholder="Example: Identity confirmed by customer support."
                    onChange={(event) =>
                      setDraft({
                        ...draft,
                        verificationReason:
                          event.target.value,
                      })
                    }
                  />
                </label>

                <button
                  type="button"
                  className="admin-secondary-btn"
                  disabled={saving}
                  onClick={verifyManually}
                >
                  Verify manually
                </button>
              </section>
            ) : (
              <div className="alert alert-success">
                This email address is verified.
              </div>
            )}
            <label className="account-control-toggle">
              <input
                type="checkbox"
                checked={draft.isSuspended}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    isSuspended:
                      event.target.checked,
                    revokeSessions:
                      event.target.checked
                        ? true
                        : draft.revokeSessions,
                  })
                }
              />

              <span>
                <strong>Suspend account</strong>
                <small>
                  Block access and revoke sessions.
                </small>
              </span>
            </label>

            {draft.isSuspended ? (
              <>
                <label>
                  Suspension reason
                  <textarea
                    required
                    rows="3"
                    maxLength="500"
                    value={draft.suspensionReason}
                    onChange={(event) =>
                      setDraft({
                        ...draft,
                        suspensionReason:
                          event.target.value,
                      })
                    }
                  />
                </label>

                <label>
                  Suspension expires
                  <input
                    type="datetime-local"
                    value={draft.suspendedUntil}
                    onChange={(event) =>
                      setDraft({
                        ...draft,
                        suspendedUntil:
                          event.target.value,
                      })
                    }
                  />
                  <small>
                    Leave empty for indefinite.
                  </small>
                </label>
              </>
            ) : null}

            <label className="account-control-toggle">
              <input
                type="checkbox"
                checked={draft.revokeSessions}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    revokeSessions:
                      event.target.checked,
                  })
                }
              />

              <span>
                <strong>Revoke all sessions</strong>
                <small>
                  Sign the customer out everywhere.
                </small>
              </span>
            </label>

            <label>
              Private administrator notes
              <textarea
                rows="5"
                maxLength="2000"
                value={draft.adminNotes}
                onChange={(event) =>
                  setDraft({
                    ...draft,
                    adminNotes:
                      event.target.value,
                  })
                }
              />
            </label>

            <button
              className="admin-primary-btn"
              disabled={saving}
            >
              {saving
                ? 'Saving…'
                : 'Save account controls'}
            </button>
          </form>
        </div>
      ) : null}
    </div>
  );
}




