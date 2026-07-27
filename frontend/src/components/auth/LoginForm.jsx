import { useCallback, useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { LogIn } from 'lucide-react';
import GoogleSignInButton, {
  isGoogleSignInConfigured,
} from './GoogleSignInButton.jsx';
import PasswordInput from './PasswordInput.jsx';
import SocialAuthButtons from './SocialAuthButtons.jsx';
import { useAuth } from '../../context/AuthContext.jsx';
import { getErrorMessage } from '../../services/authService.js';

const initialValues = {
  email: '',
  password: '',
  rememberMe: false,
};

function validate(values) {
  const errors = {};

  if (!values.email.trim()) {
    errors.email = 'Email is required.';
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email)) {
    errors.email = 'Invalid email format.';
  }

  if (!values.password) {
    errors.password = 'Password is required.';
  }

  return errors;
}

export default function LoginForm() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, loginWithGoogle } = useAuth();

  const [values, setValues] = useState(initialValues);
  const [errors, setErrors] = useState({});
  const [touched, setTouched] = useState({});
  const [formError, setFormError] = useState('');
  const [success, setSuccess] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [googleSubmitting, setGoogleSubmitting] = useState(false);

  const visibleErrors = Object.fromEntries(
    Object.entries(errors).filter(
      ([key]) => touched[key] || submitting,
    ),
  );

  function updateValue(event) {
    const { name, value, type, checked } = event.target;

    const nextValues = {
      ...values,
      [name]: type === 'checkbox' ? checked : value,
    };

    setValues(nextValues);
    setErrors(validate(nextValues));
  }

  function handleBlur(event) {
    setTouched((current) => ({
      ...current,
      [event.target.name]: true,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();

    const nextErrors = validate(values);

    setErrors(nextErrors);
    setTouched({
      email: true,
      password: true,
    });
    setFormError('');
    setSuccess('');

    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    try {
      setSubmitting(true);

      await login({
        email: values.email.trim(),
        password: values.password,
        rememberMe: values.rememberMe,
      });

      setSuccess('Signed in successfully.');

      const redirectTo =
        location.state?.from?.pathname || '/profile';

      window.setTimeout(() => {
        navigate(redirectTo, { replace: true });
      }, 350);
    } catch (error) {
      setFormError(
        getErrorMessage(error, 'Invalid email or password.'),
      );
    } finally {
      setSubmitting(false);
    }
  }

  const handleGoogleCredential = useCallback(
    async (credential) => {
      if (googleSubmitting) {
        return;
      }

      try {
        setGoogleSubmitting(true);
        setFormError('');
        setSuccess('');

        await loginWithGoogle({
          credential,
          rememberMe: values.rememberMe,
        });

        setSuccess('Signed in with Google.');

        const redirectTo =
location.state?.from?.pathname || '/profile';
        window.setTimeout(() => {
          navigate(redirectTo, { replace: true });
        }, 350);
      } catch (error) {
        setFormError(
          getErrorMessage(
            error,
            'Could not sign in with Google.',
          ),
        );
      } finally {
        setGoogleSubmitting(false);
      }
    },
    [
      googleSubmitting,
      location.state?.from?.pathname,
      loginWithGoogle,
      navigate,
      values.rememberMe,
    ],
  );

  return (
    <form
      className="auth-form"
      onSubmit={handleSubmit}
      noValidate
    >
      {formError ? (
        <div className="alert alert-error" role="alert">
          {formError}
        </div>
      ) : null}

      {success ? (
        <div className="alert alert-success" role="status">
          {success}
        </div>
      ) : null}

      <div className="field">
        <label htmlFor="login-email">Email</label>

        <input
          id="login-email"
          name="email"
          type="email"
          value={values.email}
          onChange={updateValue}
          onBlur={handleBlur}
          autoComplete="email"
          aria-invalid={Boolean(visibleErrors.email)}
          aria-describedby={
            visibleErrors.email
              ? 'login-email-error'
              : undefined
          }
        />

        {visibleErrors.email ? (
          <p
            className="field-error"
            id="login-email-error"
          >
            {visibleErrors.email}
          </p>
        ) : null}
      </div>

      <PasswordInput
        id="login-password"
        label="Password"
        name="password"
        value={values.password}
        onChange={updateValue}
        onBlur={handleBlur}
        error={visibleErrors.password}
        autoComplete="current-password"
      />

      <div className="form-row">
        <label className="checkbox-label">
          <input
            type="checkbox"
            name="rememberMe"
            checked={values.rememberMe}
            onChange={updateValue}
          />

          <span>Remember me</span>
        </label>

        <Link to="/auth/forgot-password">
          Forgot password?
        </Link>
      </div>

      <button
        className="primary-button"
        type="submit"
        disabled={submitting || googleSubmitting}
      >
        {submitting ? (
          <span className="spinner small" />
        ) : (
          <LogIn size={18} aria-hidden="true" />
        )}

        <span>
          {submitting ? 'Signing in...' : 'Sign in'}
        </span>
      </button>

      <div className="auth-divider">
        <span>or</span>
      </div>

      <div className="social-auth">
        <SocialAuthButtons
          action="Sign in"
          googleButton={
            isGoogleSignInConfigured ? (
              <GoogleSignInButton
                onCredential={handleGoogleCredential}
              />
            ) : null
          }
        />
      </div>

      <p className="switch-link">
        New here?{' '}
        <Link to="/auth/signup">Create an account</Link>
      </p>
    </form>
  );
}