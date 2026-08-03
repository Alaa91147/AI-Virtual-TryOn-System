import { Navigate } from 'react-router-dom';
import AuthLayout from '../../components/auth/AuthLayout.jsx';
import LoginForm from '../../components/auth/LoginForm.jsx';
import { useAuth } from '../../context/AuthContext.jsx';

export default function LoginPage() {
  const { isAuthenticated, initializing, user } = useAuth();

  if (!initializing && isAuthenticated) {
    return (
      <Navigate
        to={
          user?.role === 'Admin'
            ? '/admin/products'
            : '/profile'
        }
        replace
      />
    );
  }

  return (
    <AuthLayout title="Sign in" subtitle="Continue to your fit profile and saved try-on sessions.">
      <LoginForm />
    </AuthLayout>
  );
}
