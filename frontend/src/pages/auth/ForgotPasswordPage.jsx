import { useState } from 'react';
import { Link } from 'react-router-dom';
import AuthLayout from '../../components/auth/AuthLayout.jsx';
import { authService, getErrorMessage } from '../../services/authService.js';

function validateEmail(email) {
  if (!email.trim()) {
    return 'Email is required.';
  }

  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    return 'Invalid email format.';
  }

  return '';
}

export default function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(event) {
    event.preventDefault();

    const nextError = validateEmail(email);
    setError(nextError);
    setSuccess('');

    if (nextError) {
      return;
    }

    try {
      setSubmitting(true);
      await authService.forgotPassword({ email: email.trim() });
    } catch (requestError) {
      setError(getErrorMessage(requestError));
      return;
    } finally {
      setSubmitting(false);
    }

    setSuccess('If this email exists, we sent a reset link.');
  }

  return (
    <AuthLayout
      title="Reset your password"
      subtitle="Enter your email address and we will send you a reset link."
    >
      <form className="auth-form" onSubmit={handleSubmit} noValidate>
        {error ? (
          <div className="alert alert-error" role="alert">
            {error}
          </div>
        ) : null}
        {success ? (
          <div className="alert alert-success" role="status">
            {success}
          </div>
        ) : null}

        <div className="field">
          <label htmlFor="forgot-email">Email</label>
          <input
            id="forgot-email"
            name="email"
            type="email"
            value={email}
            onChange={(event) => {
              setEmail(event.target.value);
              setError('');
            }}
            autoComplete="email"
            aria-invalid={Boolean(error)}
            aria-describedby={error ? 'forgot-email-error' : undefined}
          />
          {error ? (
            <p className="field-error" id="forgot-email-error">
              {error}
            </p>
          ) : null}
        </div>

        <button className="primary-button" type="submit" disabled={submitting}>
          <span>{submitting ? 'Sending...' : 'Send Reset Link'}</span>
        </button>

        <Link className="secondary-button" to="/auth/login">
          Back to sign in
        </Link>
      </form>
    </AuthLayout>
  );
}
