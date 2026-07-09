import { useEffect, useRef, useState } from 'react';

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID;
const GOOGLE_SCRIPT_SRC = 'https://accounts.google.com/gsi/client';

let googleScriptPromise = null;

export const isGoogleSignInConfigured = Boolean(GOOGLE_CLIENT_ID);

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

function loadGoogleScript() {
  if (typeof window === 'undefined') {
    return Promise.reject(new Error('Google sign-in is only available in the browser.'));
  }

  if (window.google?.accounts?.id) {
    return Promise.resolve();
  }

  if (googleScriptPromise) {
    return googleScriptPromise;
  }

  googleScriptPromise = new Promise((resolve, reject) => {
    const existingScript = document.querySelector(`script[src="${GOOGLE_SCRIPT_SRC}"]`);
    if (existingScript) {
      existingScript.addEventListener('load', resolve, { once: true });
      existingScript.addEventListener('error', reject, { once: true });
      return;
    }

    const script = document.createElement('script');
    script.src = GOOGLE_SCRIPT_SRC;
    script.async = true;
    script.defer = true;
    script.onload = resolve;
    script.onerror = reject;
    document.head.appendChild(script);
  });

  return googleScriptPromise;
}

export default function GoogleSignInButton({ onCredential, text = 'signin_with' }) {
  const buttonRef = useRef(null);
  const onCredentialRef = useRef(onCredential);
  const [ready, setReady] = useState(() => (
    typeof window !== 'undefined' && Boolean(window.google?.accounts?.id)
  ));
  const [loadFailed, setLoadFailed] = useState(false);

  useEffect(() => {
    onCredentialRef.current = onCredential;
  }, [onCredential]);

  useEffect(() => {
    if (!GOOGLE_CLIENT_ID) {
      return undefined;
    }

    let isMounted = true;

    loadGoogleScript()
      .then(() => {
        if (isMounted) {
          setReady(true);
        }
      })
      .catch(() => {
        if (isMounted) {
          setLoadFailed(true);
        }
      });

    return () => {
      isMounted = false;
    };
  }, []);

  useEffect(() => {
    if (!GOOGLE_CLIENT_ID || !ready || !buttonRef.current) {
      return;
    }

    window.google.accounts.id.initialize({
      client_id: GOOGLE_CLIENT_ID,
      callback: (response) => {
        if (response.credential) {
          onCredentialRef.current(response.credential);
        }
      },
      use_fedcm_for_prompt: true,
    });

    buttonRef.current.innerHTML = '';
    window.google.accounts.id.renderButton(buttonRef.current, {
      type: 'standard',
      theme: 'outline',
      size: 'large',
      shape: 'pill',
      text,
      width: Math.min(buttonRef.current.parentElement?.offsetWidth || 400, 400),
    });
  }, [ready, text]);

  if (!GOOGLE_CLIENT_ID) {
    return null;
  }

  if (loadFailed) {
    return (
      <button className="google-fallback-button" type="button" disabled>
        Google sign-in unavailable
      </button>
    );
  }

  return (
    <div className="google-button-wrap">
      <div className="social-button google-button-face" aria-hidden="true">
        <span className="social-button__icon-wrap">
          <GoogleIcon />
        </span>
        <span>Sign in with Google</span>
      </div>
      <div className="google-button-native" ref={buttonRef} />
    </div>
  );
}
