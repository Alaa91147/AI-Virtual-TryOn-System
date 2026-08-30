import {
  useEffect,
  useRef,
  useState,
} from 'react';

import {
  CheckCircle2,
  Mail,
  XCircle,
} from 'lucide-react';

import {
  Link,
  useSearchParams,
} from 'react-router-dom';

import AuthLayout from
  '../../components/auth/AuthLayout.jsx';

import {
  authService,
  getErrorMessage,
} from '../../services/authService.js';

export default function VerifyEmailPage() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get('token') || '';

  const started = useRef(false);

  const [status, setStatus] = useState(
    token ? 'loading' : 'error',
  );

  const [message, setMessage] = useState(
    token
      ? 'Checking your verification link...'
      : 'This verification link is missing.',
  );

  const [email, setEmail] = useState('');
  const [resending, setResending] =
    useState(false);

  const [resendMessage, setResendMessage] =
    useState('');

  useEffect(() => {
    if (!token || started.current) {
      return;
    }

    started.current = true;

    async function verify() {
      try {
        const response =
          await authService.verifyEmail(token);

        setStatus('success');
        setMessage(
          response?.message ||
            'Your email was verified successfully.',
        );
      } catch (error) {
        setStatus('error');
        setMessage(
          getErrorMessage(
            error,
            'This verification link is invalid or expired.',
          ),
        );
      }
    }

    verify();
  }, [token]);

  async function resend(event) {
    event.preventDefault();
    setResendMessage('');

    if (!email.trim()) {
      setResendMessage(
        'Enter your email address.',
      );
      return;
    }

    try {
      setResending(true);

      const response =
        await authService.resendVerification(
          email.trim(),
        );

      setResendMessage(
        response?.message ||
          'If verification is required, a new email was sent.',
      );
    } catch (error) {
      setResendMessage(
        getErrorMessage(
          error,
          'Could not send the verification email.',
        ),
      );
    } finally {
      setResending(false);
    }
  }

  return (
    <AuthLayout
      title="Verify your email"
      subtitle="Confirm your email address to finish securing your account."
    >
      <div className="auth-form">
        <div
          className={
            status === 'success'
              ? 'alert alert-success'
              : status === 'error'
                ? 'alert alert-error'
                : 'alert'
          }
          role="status"
        >
          {status === 'success' ? (
            <CheckCircle2
              size={20}
              aria-hidden="true"
            />
          ) : status === 'error' ? (
            <XCircle
              size={20}
              aria-hidden="true"
            />
          ) : (
            <span className="spinner small" />
          )}

          <span>{message}</span>
        </div>

        {status === 'success' ? (
          <Link
            className="primary-button"
            to="/auth/login"
          >
            Continue to sign in
          </Link>
        ) : null}

        {status === 'error' ? (
          <form
            className="auth-form"
            onSubmit={resend}
          >
            <label
              className="field-group"
              htmlFor="verification-email"
            >
              <span>Email address</span>

              <div className="field-control">
                <Mail
                  size={18}
                  aria-hidden="true"
                />

                <input
                  id="verification-email"
                  type="email"
                  value={email}
                  autoComplete="email"
                  placeholder="you@example.com"
                  onChange={(event) =>
                    setEmail(event.target.value)
                  }
                />
              </div>
            </label>

            {resendMessage ? (
              <div
                className="alert"
                role="status"
              >
                {resendMessage}
              </div>
            ) : null}

            <button
              className="primary-button"
              type="submit"
              disabled={resending}
            >
              {resending
                ? 'Sending...'
                : 'Send a new verification email'}
            </button>

            <p className="auth-switch">
              <Link to="/auth/login">
                Return to sign in
              </Link>
            </p>
          </form>
        ) : null}
      </div>
    </AuthLayout>
  );
}
