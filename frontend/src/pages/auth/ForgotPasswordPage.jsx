import { Link } from 'react-router-dom';
import AuthLayout from '../../components/auth/AuthLayout.jsx';

export default function ForgotPasswordPage() {
  return (
    <AuthLayout title="Forgot password" subtitle="Password reset will be connected in a later auth phase.">
      <div className="auth-form">
        <div className="alert alert-info" role="status">
          This page is reserved for the password reset API.
        </div>
        <Link className="secondary-button" to="/auth/login">
          Back to sign in
        </Link>
      </div>
    </AuthLayout>
  );
}
