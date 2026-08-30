import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';

import { authService } from '../services/authService.js';

const AuthContext = createContext(null);

const TOKEN_KEY = 'virtualTryOn.token';
const REFRESH_TOKEN_KEY =
  'virtualTryOn.refreshToken';
const USER_KEY = 'virtualTryOn.user';

function readStoredSession() {
  for (const storage of [
    localStorage,
    sessionStorage,
  ]) {
    const token = storage.getItem(TOKEN_KEY);
    const refreshToken = storage.getItem(
      REFRESH_TOKEN_KEY,
    );
    const storedUser = storage.getItem(USER_KEY);

    if (token) {
      return {
        token,
        refreshToken,
        user: storedUser
          ? JSON.parse(storedUser)
          : null,
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
  for (const storage of [
    localStorage,
    sessionStorage,
  ]) {
    storage.removeItem(TOKEN_KEY);
    storage.removeItem(REFRESH_TOKEN_KEY);
    storage.removeItem(USER_KEY);
  }
}

function toStoredUser(user) {
  if (!user) {
    return null;
  }

  return {
    ...user,
    id: user.id,
    fullName: user.fullName,
    email: user.email,
    gender: user.gender,
    shoppingPreference:
      user.shoppingPreference,
    role: user.role,
    isEmailVerified:
      user.isEmailVerified,
  };
}

function persistStoredUser(storage, user) {
  storage.removeItem(USER_KEY);

  storage.setItem(
    USER_KEY,
    JSON.stringify(toStoredUser(user)),
  );
}

function persistSession(data, rememberMe) {
  clearStoredSession();

  const storage = rememberMe
    ? localStorage
    : sessionStorage;

  storage.setItem(TOKEN_KEY, data.token);

  storage.setItem(
    REFRESH_TOKEN_KEY,
    data.refreshToken,
  );

  persistStoredUser(storage, data.user);
}

export function AuthProvider({ children }) {
  const [session, setSession] = useState(
    () => readStoredSession(),
  );

  const [initializing, setInitializing] =
    useState(true);

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
        const user = await authService.me(
          session.token,
        );

        if (!isMounted) {
          return;
        }

        persistStoredUser(
          session.storage,
          user,
        );

        setSession((current) => ({
          ...current,
          user,
        }));
      } catch {
        if (!session.refreshToken) {
          if (isMounted) clearSession();
          return;
        }

        try {
          const refreshed = await authService.refresh(session.refreshToken);
          if (!isMounted) return;

          const rememberMe = session.storage === localStorage;
          persistSession(refreshed, rememberMe);
          setSession({
            token: refreshed.token,
            refreshToken: refreshed.refreshToken,
            user: refreshed.user,
            storage: rememberMe ? localStorage : sessionStorage,
          });
        } catch {
          if (isMounted) clearSession();
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

  const login = useCallback(
    async (payload) => {
      const data = await authService.login(
        payload,
      );

      persistSession(
        data,
        payload.rememberMe,
      );

      setSession({
        token: data.token,
        refreshToken: data.refreshToken,
        user: data.user,
        storage: payload.rememberMe
          ? localStorage
          : sessionStorage,
      });

      return data;
    },
    [],
  );

  const loginWithGoogle = useCallback(
    async (payload) => {
      const rememberMe = Boolean(
        payload.rememberMe,
      );

      const data =
        await authService.googleLogin(payload);

      persistSession(data, rememberMe);

      setSession({
        token: data.token,
        refreshToken: data.refreshToken,
        user: data.user,
        storage: rememberMe
          ? localStorage
          : sessionStorage,
      });

      return data;
    },
    [],
  );

  const signup = useCallback(
    async (payload) => {
      const data =
        await authService.register(payload);

      persistSession(data, false);

      setSession({
        token: data.token,
        refreshToken: data.refreshToken,
        user: data.user,
        storage: sessionStorage,
      });

      return data;
    },
    [],
  );

  const logout = useCallback(async () => {
    const token = session.token;
    const refreshToken =
      session.refreshToken;

    clearSession();

    if (token) {
      try {
        await authService.logout(
          token,
          refreshToken,
        );
      } catch {
        // Local logout must still complete.
      }
    }
  }, [
    clearSession,
    session.refreshToken,
    session.token,
  ]);

  const updateProfile = useCallback(
    async (payload) => {
      if (!session.token) {
        throw new Error(
          'You need to sign in again.',
        );
      }

      const user =
        await authService.updateProfile(
          session.token,
          payload,
        );

      persistStoredUser(
        session.storage,
        user,
      );

      setSession((current) => ({
        ...current,
        user,
      }));

      return user;
    },
    [session.storage, session.token],
  );

  const updateShoppingPreference =
    useCallback(
      async (preference) => {
        if (!session.token) {
          throw new Error(
            'You need to sign in again.',
          );
        }

        const user =
          await authService
            .updateShoppingPreference(
              session.token,
              preference,
            );

        persistStoredUser(
          session.storage,
          user,
        );

        setSession((current) => ({
          ...current,
          user,
        }));

        return user;
      },
      [
        session.storage,
        session.token,
      ],
    );

  const value = useMemo(
    () => ({
      token: session.token,
      refreshToken: session.refreshToken,
      user: session.user,
      initializing,
      isAuthenticated: Boolean(
        session.token && session.user,
      ),
      login,
      loginWithGoogle,
      signup,
      logout,
      updateProfile,
      updateShoppingPreference,
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
      updateShoppingPreference,
    ],
  );

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (!context) {
    throw new Error(
      'useAuth must be used inside AuthProvider.',
    );
  }

  return context;
}
