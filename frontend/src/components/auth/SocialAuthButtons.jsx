function GoogleIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 48 48" className="social-button__brand-icon">
      <path fill="#FFC107" d="M43.61 20.08H42V20H24v8h11.3C33.65 32.66 29.19 36 24 36c-6.63 0-12-5.37-12-12s5.37-12 12-12c3.06 0 5.84 1.15 7.96 3.04l5.66-5.66C34.05 6.05 29.27 4 24 4 12.95 4 4 12.95 4 24s8.95 20 20 20 20-8.95 20-20c0-1.34-.14-2.65-.39-3.92Z" />
      <path fill="#FF3D00" d="M6.31 14.69l6.57 4.82A11.96 11.96 0 0 1 24 12c3.06 0 5.84 1.15 7.96 3.04l5.66-5.66C34.05 6.05 29.27 4 24 4c-7.68 0-14.35 4.34-17.69 10.69Z" />
      <path fill="#4CAF50" d="M24 44c5.17 0 9.86-1.98 13.41-5.2l-6.19-5.24A11.93 11.93 0 0 1 24 36c-5.17 0-9.62-3.32-11.25-7.94l-6.52 5.02C9.54 39.56 16.26 44 24 44Z" />
      <path fill="#1976D2" d="M43.61 20.08H42V20H24v8h11.3a12.04 12.04 0 0 1-4.09 5.56l.01-.01 6.19 5.24C37 39.12 44 34 44 24c0-1.34-.14-2.65-.39-3.92Z" />
    </svg>
  );
}

function AppleIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" className="social-button__brand-icon">
      <path fill="currentColor" d="M17.05 20.28c-.98.95-2.05.8-3.08.35-1.09-.46-2.09-.48-3.24 0-1.44.62-2.2.44-3.06-.35C2.78 15.25 3.5 7.59 9.05 7.31c1.35.07 2.29.74 3.08.8 1.18-.24 2.31-.93 3.57-.84 1.51.12 2.65.72 3.4 1.8-3.12 1.87-2.38 5.98.48 7.13-.57 1.5-1.31 2.99-2.53 4.08ZM12.03 7.25C11.88 5.03 13.68 3.2 15.75 3c.29 2.58-2.34 4.5-3.72 4.25Z" />
    </svg>
  );
}

function FacebookIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 24 24" className="social-button__brand-icon">
      <path fill="#1877F2" d="M24 12.07C24 5.4 18.63 0 12 0S0 5.4 0 12.07c0 6.03 4.39 11.02 10.13 11.93v-8.44H7.08v-3.49h3.05V9.41c0-3.03 1.79-4.7 4.53-4.7 1.31 0 2.68.24 2.68.24v2.96h-1.51c-1.49 0-1.96.93-1.96 1.89v2.27h3.33l-.53 3.49h-2.8V24C19.61 23.09 24 18.1 24 12.07Z" />
    </svg>
  );
}

export default function SocialAuthButtons({ action = 'Sign in', googleButton = null }) {
  const labelPrefix = action === 'Sign up' ? 'Sign up' : 'Sign in';

  return (
    <div className="social-auth__stack">
      {googleButton ? (
        googleButton
      ) : (
        <button
          className="social-button social-button--google"
          type="button"
          aria-label={`${labelPrefix} with Google`}
        >
          <span className="social-button__icon-wrap">
            <GoogleIcon />
          </span>
          <span>{labelPrefix} with Google</span>
        </button>
      )}
      <button
        className="social-button social-button--apple"
        type="button"
        aria-label={`${labelPrefix} with Apple`}
      >
        <span className="social-button__icon-wrap">
          <AppleIcon />
        </span>
        <span>{labelPrefix} with Apple</span>
      </button>
      <button
        className="social-button social-button--facebook"
        type="button"
        aria-label={`${labelPrefix} with Facebook`}
      >
        <span className="social-button__icon-wrap">
          <FacebookIcon />
        </span>
        <span>{labelPrefix} with Facebook</span>
      </button>
    </div>
  );
}
