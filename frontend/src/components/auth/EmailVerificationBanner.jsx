import { useState } from 'react';
import { Mail, X } from 'lucide-react';

import { useAuth } from
  '../../context/AuthContext.jsx';

import {
  authService,
  getErrorMessage,
} from '../../services/authService.js';

import './EmailVerificationBanner.css';

export default function EmailVerificationBanner() {
  const { isAuthenticated, user } = useAuth();

  const [hidden, setHidden] = useState(false);
  const [sending, setSending] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  if (
    !isAuthenticated ||
    !user ||
    user.role !== 'Customer' ||
    user.isEmailVerified ||
    hidden
  ) {
    return null;
  }

  async function resend() {
    try {
      setSending(true);
      setError('');
      setMessage('');

      const response =
        await authService.resendVerification(
          user.email
        );

      setMessage(
        response?.message ||
          'Verification email sent.'
      );
    } catch (requestError) {
      setError(
        getErrorMessage(
          requestError,
          'Could not send verification email.'
        )
      );
    } finally {
      setSending(false);
    }
  }

  return (
    <aside
      className="email-verification-banner"
      aria-live="polite"
    >
      <Mail size={20} aria-hidden="true" />

      <div>
        <strong>Verify your email address</strong>

        <span>
          Verify {user.email} to receive promotions
          and secure your account.
        </span>

        {message ? (
          <small className="is-success">
            {message}
          </small>
        ) : null}

        {error ? (
          <small className="is-error">
            {error}
          </small>
        ) : null}
      </div>

      <button
        type="button"
        className="verification-resend"
        disabled={sending}
        onClick={resend}
      >
        {sending ? 'Sending...' : 'Resend email'}
      </button>

      <button
        type="button"
        className="verification-dismiss"
        aria-label="Dismiss verification reminder"
        onClick={() => setHidden(true)}
      >
        <X size={18} />
      </button>
    </aside>
  );
}
