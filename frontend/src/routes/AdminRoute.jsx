import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext.jsx';

export default function AdminRoute({ children }) {
  const location = useLocation();
  const {
    initializing,
    isAuthenticated,
    user,
  } = useAuth();

  if (initializing) {
    return (
      <main className="route-loader" aria-live="polite">
        <span className="spinner" />
      </main>
    );
  }

  if (!isAuthenticated) {
    return (
      <Navigate
        to="/auth/login"
        replace
        state={{ from: location }}
      />
    );
  }

  if (user?.role !== 'Admin') {
    return <Navigate to="/shop" replace />;
  }

  return children;
}
