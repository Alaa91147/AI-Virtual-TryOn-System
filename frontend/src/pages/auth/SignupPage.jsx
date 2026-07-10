import { Navigate } from 'react-router-dom';
import AuthLayout from '../../components/auth/AuthLayout.jsx';
import SignupForm from '../../components/auth/SignupForm.jsx';
import { useAuth } from '../../context/AuthContext.jsx';

export default function SignupPage() {
  const { isAuthenticated, initializing } = useAuth();

  if (!initializing && isAuthenticated) {
    return <Navigate to="/profile" replace />;
  }

  return (
    <AuthLayout title="Create account" subtitle="Start with core account details. Fit preferences can be added later.">
      <SignupForm />
    </AuthLayout>
  );
}
