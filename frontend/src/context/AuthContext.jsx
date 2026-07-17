import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { authService } from '../services/authService.js';

const AuthContext = createContext(null);

const TOKEN_KEY = 'virtualTryOn.token';
const REFRESH_TOKEN_KEY = 'virtualTryOn.refreshToken';
const USER_KEY = 'virtualTryOn.user';

function readStoredSession() {
  for (const storage of [localStorage, sessionStorage]) {
    const token = storage.getItem(TOKEN_KEY);
    const refreshToken = storage.getItem(REFRESH_TOKEN_KEY);
    const user = storage.getItem(USER_KEY);

    if (token) {
      return {
        token,
        refreshToken,
        user: user ? JSON.parse(user) : null,
        storage,
      };
    }
  }

  return {
    token: null,
    refreshToken: null,
    user: null,
    storage: sessionStorage,
  };
}

function clearStoredSession() {
  for (const storage of [localStorage, sessionStorage]) {
    storage.removeItem(TOKEN_KEY);
    storage.removeItem(REFRESH_TOKEN_KEY);
    storage.removeItem(USER_KEY);
  }
}

function persistSession(data, rememberMe) {
  clearStoredSession();

  const storage = rememberMe ? localStorage : sessionStorage;
  storage.setItem(TOKEN_KEY, data.token);
  storage.setItem(REFRESH_TOKEN_KEY, data.refreshToken);
  storage.setItem(USER_KEY, JSON.stringify(data.user));
}

export function AuthProvider({ children }) {
  const [session, setSession] = useState(() => readStoredSession());
  const [initializing, setInitializing] = useState(true);

  const clearSession = useCallback(() => {
    clearStoredSession();
    setSession({
      token: null,
      refreshToken: null,
      user: null,
      storage: sessionStorage,
    });
  }, []);

  useEffect(() => {
    let isMounted = true;

    async function loadCurrentUser() {
      if (!session.token) {
        setInitializing(false);
        return;
      }

      try {
        const user = await authService.me(session.token);
        if (!isMounted) {
          return;
        }

        const nextSession = {
          ...session,
          user,
        };
        session.storage.setItem(USER_KEY, JSON.stringify(user));
        setSession(nextSession);
      } catch {
        if (isMounted) {
          clearSession();
        }
      } finally {
        if (isMounted) {
          setInitializing(false);
        }
      }
    }

    loadCurrentUser();

    return () => {
      isMounted = false;
    };
  }, []);

  const login = useCallback(async (payload) => {
    const data = await authService.login(payload);
    persistSession(data, payload.rememberMe);
    setSession({
      token: data.token,
      refreshToken: data.refreshToken,
      user: data.user,
      storage: payload.rememberMe ? localStorage : sessionStorage,
    });
    return data;
  }, []);

  const loginWithGoogle = useCallback(async (payload) => {
    const rememberMe = Boolean(payload.rememberMe);
    const data = await authService.googleLogin(payload);
    persistSession(data, rememberMe);
    setSession({
      token: data.token,
      refreshToken: data.refreshToken,
      user: data.user,
      storage: rememberMe ? localStorage : sessionStorage,
    });
    return data;
  }, []);

  const signup = useCallback(async (payload) => {
    const data = await authService.register(payload);
    persistSession(data, false);
    setSession({
      token: data.token,
      refreshToken: data.refreshToken,
      user: data.user,
      storage: sessionStorage,
    });
    return data;
  }, []);

  const logout = useCallback(async () => {
    const token = session.token;
    const refreshToken = session.refreshToken;
    clearSession();

    if (token) {
      try {
        await authService.logout(token, refreshToken);
      } catch {
        // Local sign-out should still complete if the server token is already invalid.
      }
    }
  }, [clearSession, session.refreshToken, session.token]);

  const updateProfile = useCallback(async (payload) => {
    const user = await authService.updateProfile(session.token, payload);
    const nextSession = {
      ...session,
      user,
    };

    session.storage.setItem(USER_KEY, JSON.stringify(user));
    setSession(nextSession);
    return user;
  }, [session]);

  const value = useMemo(
    () => ({
      token: session.token,
      refreshToken: session.refreshToken,
      user: session.user,
      initializing,
      isAuthenticated: Boolean(session.token && session.user),
      login,
      loginWithGoogle,
      signup,
      logout,
      updateProfile,
    }),
    [
      initializing,
      login,
      loginWithGoogle,
      logout,
      session.refreshToken,
      session.token,
      session.user,
      signup,
      updateProfile,
    ],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider.');
  }

  return context;
}
