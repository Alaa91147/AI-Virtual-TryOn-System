import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import AuthLayout from '../../components/auth/AuthLayout.jsx';
import PasswordInput from '../../components/auth/PasswordInput.jsx';
import PasswordStrength, { getPasswordStrength } from '../../components/auth/PasswordStrength.jsx';
import { authService, getErrorMessage } from '../../services/authService.js';

const initialValues = {
  password: '',
  confirmPassword: '',
};

function validate(values) {
  const errors = {};

  if (!values.password) {
    errors.password = 'Password is required.';
  } else if (values.password.length < 8) {
    errors.password = 'Password must be at least 8 characters.';
  } else if (getPasswordStrength(values.password).score < 2) {
    errors.password = 'Use a stronger password.';
  }

  if (!values.confirmPassword) {
    errors.confirmPassword = 'Confirm password is required.';
  } else if (values.password !== values.confirmPassword) {
    errors.confirmPassword = 'Passwords do not match.';
  }

  return errors;
}

export default function ResetPasswordPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const resetToken = searchParams.get('token') || '';
  const [values, setValues] = useState(initialValues);
  const [errors, setErrors] = useState({});
  const [touched, setTouched] = useState({});
  const [formError, setFormError] = useState(() => (
    resetToken ? '' : 'This reset link is invalid or expired. Please request a new one.'
  ));
  const [success, setSuccess] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const visibleErrors = Object.fromEntries(
    Object.entries(errors).filter(([key]) => touched[key] || submitting),
  );

  function updateValue(event) {
    const nextValues = {
      ...values,
      [event.target.name]: event.target.value,
    };

    setValues(nextValues);
    setErrors(validate(nextValues));
    setFormError('');
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
    setTouched({ password: true, confirmPassword: true });
    setFormError('');
    setSuccess('');

    if (Object.keys(nextErrors).length > 0 || !resetToken) {
      return;
    }

    try {
      setSubmitting(true);
      await authService.resetPassword({
        token: resetToken,
        password: values.password,
        confirmPassword: values.confirmPassword,
      });
      setSuccess('Your password has been reset successfully. You can now log in.');
      window.setTimeout(() => navigate('/auth/login', { replace: true }), 1800);
    } catch (error) {
      setFormError(getErrorMessage(error, 'This reset link is invalid, expired, or already used.'));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AuthLayout title="Reset password" subtitle="Choose a new password for your account.">
      <form className="auth-form" onSubmit={handleSubmit} noValidate>
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

        <PasswordInput
          id="reset-password"
          label="New password"
          name="password"
          value={values.password}
          onChange={updateValue}
          onBlur={handleBlur}
          error={visibleErrors.password}
          autoComplete="new-password"
        />
        <PasswordStrength password={values.password} />

        <PasswordInput
          id="reset-confirm-password"
          label="Confirm new password"
          name="confirmPassword"
          value={values.confirmPassword}
          onChange={updateValue}
          onBlur={handleBlur}
          error={visibleErrors.confirmPassword}
          autoComplete="new-password"
        />

        <button
          className="primary-button"
          type="submit"
          disabled={submitting || Boolean(formError)}
        >
          <span>{submitting ? 'Resetting...' : 'Reset Password'}</span>
        </button>

        <Link className="secondary-button" to="/auth/login">
          Back to sign in
        </Link>
      </form>
    </AuthLayout>
  );
}
