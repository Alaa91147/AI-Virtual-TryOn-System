import {
  Navigate,
  Route,
  Routes,
} from 'react-router-dom';

import HomePage from './pages/HomePage.jsx';
import LoggedInHomePage from './pages/LoggedInHomePage.jsx';
import LoginPage from './pages/auth/LoginPage.jsx';
import SignupPage from './pages/auth/SignupPage.jsx';
import ForgotPasswordPage from './pages/auth/ForgotPasswordPage.jsx';
import ResetPasswordPage from './pages/auth/ResetPasswordPage.jsx';
import ProfilePage from './pages/ProfilePage.jsx';
import ProductDetailsPage from './pages/ProductDetailsPage.jsx';
import CartPage from './pages/CartPage.jsx';
import AdminProductsPage from './pages/AdminProductsPage.jsx';
import ProtectedRoute from './routes/ProtectedRoute.jsx';
import AdminRoute from './routes/AdminRoute.jsx';
import CustomerRoute from './routes/CustomerRoute.jsx';
import NotificationToast from './components/shop/NotificationToast.jsx';

export default function App() {
  return (
    <>
      <NotificationToast />

      <Routes>
        <Route
          path="/"
          element={<HomePage />}
        />

        <Route
          path="/auth/login"
          element={<LoginPage />}
        />

        <Route
          path="/auth/signup"
          element={<SignupPage />}
        />

        <Route
          path="/auth/forgot-password"
          element={<ForgotPasswordPage />}
        />

        <Route
          path="/auth/reset-password"
          element={<ResetPasswordPage />}
        />

        <Route
          path="/reset-password"
          element={<ResetPasswordPage />}
        />

        <Route
          path="/shop"
          element={
            <CustomerRoute>
              <LoggedInHomePage />
            </CustomerRoute>
          }
        />

        <Route
          path="/shop/products/:productId"
          element={
            <ProtectedRoute>
              <ProductDetailsPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/shop/cart"
          element={
            <CustomerRoute>
              <CartPage />
            </CustomerRoute>
          }
        />

        <Route
          path="/profile"
          element={
            <CustomerRoute>
              <ProfilePage />
            </CustomerRoute>
          }
        />

        <Route
          path="/admin/products"
          element={
            <AdminRoute>
              <AdminProductsPage />
            </AdminRoute>
          }
        />

        <Route
          path="*"
          element={
            <Navigate to="/" replace />
          }
        />
      </Routes>
    </>
  );
}
