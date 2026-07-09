import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { UserPlus } from 'lucide-react';
import PasswordInput from './PasswordInput.jsx';
import PasswordStrength, { getPasswordStrength } from './PasswordStrength.jsx';
import SocialAuthButtons from './SocialAuthButtons.jsx';
import { useAuth } from '../../context/AuthContext.jsx';
import { getErrorMessage } from '../../services/authService.js';

const initialValues = {
  fullName: '',
  email: '',
  password: '',
  confirmPassword: '',
  gender: '',
  dateOfBirth: '',
  acceptTerms: false,
};

function validate(values) {
  const errors = {};

  if (!values.fullName.trim()) {
    errors.fullName = 'Full name is required.';
  } else if (values.fullName.trim().length < 2) {
    errors.fullName = 'Full name must be at least 2 characters.';
  }

  if (!values.email.trim()) {
    errors.email = 'Email is required.';
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email)) {
    errors.email = 'Invalid email format.';
  }

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

  if (!values.gender) {
    errors.gender = 'Gender is required.';
  }

  if (values.dateOfBirth) {
    const selectedDate = new Date(values.dateOfBirth);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (selectedDate > today) {
      errors.dateOfBirth = 'Date of birth cannot be in the future.';
    }
  }

  if (!values.acceptTerms) {
    errors.acceptTerms = 'You must accept the terms and conditions.';
  }

  return errors;
}

export default function SignupForm() {
  const navigate = useNavigate();
  const { signup } = useAuth();
  const [values, setValues] = useState(initialValues);
  const [errors, setErrors] = useState({});
  const [touched, setTouched] = useState({});
  const [formError, setFormError] = useState('');
  const [success, setSuccess] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const visibleErrors = Object.fromEntries(
    Object.entries(errors).filter(([key]) => touched[key] || submitting),
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
      fullName: true,
      email: true,
      password: true,
      confirmPassword: true,
      gender: true,
      dateOfBirth: true,
      acceptTerms: true,
    });
    setFormError('');
    setSuccess('');

    if (Object.keys(nextErrors).length > 0) {
      return;
    }

    try {
      setSubmitting(true);
      await signup({
        fullName: values.fullName.trim(),
        email: values.email.trim(),
        password: values.password,
        confirmPassword: values.confirmPassword,
        gender: values.gender,
        dateOfBirth: values.dateOfBirth || null,
        acceptTerms: values.acceptTerms,
      });

      setSuccess('Account created successfully.');
      window.setTimeout(() => navigate('/profile', { replace: true }), 450);
    } catch (error) {
      setFormError(getErrorMessage(error, 'Could not create your account.'));
    } finally {
      setSubmitting(false);
    }
  }

  return (
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

      <div className="field">
        <label htmlFor="signup-full-name">Full name</label>
        <input
          id="signup-full-name"
          name="fullName"
          type="text"
          value={values.fullName}
          onChange={updateValue}
          onBlur={handleBlur}
          autoComplete="name"
          aria-invalid={Boolean(visibleErrors.fullName)}
          aria-describedby={visibleErrors.fullName ? 'signup-full-name-error' : undefined}
        />
        {visibleErrors.fullName ? (
          <p className="field-error" id="signup-full-name-error">
            {visibleErrors.fullName}
          </p>
        ) : null}
      </div>

      <div className="field">
        <label htmlFor="signup-email">Email</label>
        <input
          id="signup-email"
          name="email"
          type="email"
          value={values.email}
          onChange={updateValue}
          onBlur={handleBlur}
          autoComplete="email"
          aria-invalid={Boolean(visibleErrors.email)}
          aria-describedby={visibleErrors.email ? 'signup-email-error' : undefined}
        />
        {visibleErrors.email ? (
          <p className="field-error" id="signup-email-error">
            {visibleErrors.email}
          </p>
        ) : null}
      </div>

      <PasswordInput
        id="signup-password"
        label="Password"
        name="password"
        value={values.password}
        onChange={updateValue}
        onBlur={handleBlur}
        error={visibleErrors.password}
        autoComplete="new-password"
      />
      <PasswordStrength password={values.password} />

      <PasswordInput
        id="signup-confirm-password"
        label="Confirm password"
        name="confirmPassword"
        value={values.confirmPassword}
        onChange={updateValue}
        onBlur={handleBlur}
        error={visibleErrors.confirmPassword}
        autoComplete="new-password"
      />

      <fieldset className="field segmented-field">
        <legend>Gender</legend>
        <div className="segmented-control">
          {['male', 'female', 'other'].map((gender) => (
            <label key={gender} className={values.gender === gender ? 'selected' : ''}>
              <input
                type="radio"
                name="gender"
                value={gender}
                checked={values.gender === gender}
                onChange={updateValue}
                onBlur={handleBlur}
              />
              <span>{gender}</span>
            </label>
          ))}
        </div>
        {visibleErrors.gender ? <p className="field-error">{visibleErrors.gender}</p> : null}
      </fieldset>

      <div className="field">
        <label htmlFor="signup-date-of-birth">Date of birth <span>Optional</span></label>
        <input
          id="signup-date-of-birth"
          name="dateOfBirth"
          type="date"
          value={values.dateOfBirth}
          onChange={updateValue}
          onBlur={handleBlur}
          aria-invalid={Boolean(visibleErrors.dateOfBirth)}
          aria-describedby={visibleErrors.dateOfBirth ? 'signup-date-error' : undefined}
        />
        {visibleErrors.dateOfBirth ? (
          <p className="field-error" id="signup-date-error">
            {visibleErrors.dateOfBirth}
          </p>
        ) : null}
      </div>

      <label className="checkbox-label terms-label">
        <input
          type="checkbox"
          name="acceptTerms"
          checked={values.acceptTerms}
          onChange={updateValue}
          onBlur={handleBlur}
          aria-invalid={Boolean(visibleErrors.acceptTerms)}
        />
        <span>I accept the terms and conditions.</span>
      </label>
      {visibleErrors.acceptTerms ? <p className="field-error">{visibleErrors.acceptTerms}</p> : null}

      <button className="primary-button" type="submit" disabled={submitting}>
        {submitting ? <span className="spinner small" /> : <UserPlus size={18} aria-hidden="true" />}
        <span>{submitting ? 'Creating account...' : 'Sign up'}</span>
      </button>

      <div className="auth-divider">
        <span>or</span>
      </div>
      <div className="social-auth">
        <SocialAuthButtons action="Sign up" />
      </div>

      <p className="switch-link">
        Already have an account? <Link to="/auth/login">Sign in</Link>
      </p>
    </form>
  );
}
