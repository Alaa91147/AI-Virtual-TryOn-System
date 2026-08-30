import EmailVerificationBanner from './components/auth/EmailVerificationBanner.jsx';
import VerifyEmailPage from './pages/auth/VerifyEmailPage.jsx';
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

import AdminDashboardPage from './pages/AdminDashboardPage.jsx';
import AdminProductsPage from './pages/AdminProductsPage.jsx';
import AdminOrdersPage from './pages/AdminOrdersPage.jsx';
import AdminCustomersPage from './pages/AdminCustomersPage.jsx';
import AdminPromotionsPage from './pages/AdminPromotionsPage.jsx';
import AdminBroadcastPage from './pages/AdminBroadcastPage.jsx';
import AdminCategoriesPage from './pages/AdminCategoriesPage.jsx';
import AdminAuditLogsPage from './pages/AdminAuditLogsPage.jsx';
import AdminNotificationsPage from './pages/AdminNotificationsPage.jsx';
import AdminInventoryHistoryPage from './pages/AdminInventoryHistoryPage.jsx';
import AdminSecurityPage from './pages/AdminSecurityPage.jsx';
import AdminCustomerControlsPage from './pages/AdminCustomerControlsPage.jsx';

import ProtectedRoute from './routes/ProtectedRoute.jsx';
import AdminRoute from './routes/AdminRoute.jsx';
import CustomerRoute from './routes/CustomerRoute.jsx';

import NotificationToast from './components/shop/NotificationToast.jsx';

export default function App() {
  return (
    <>
      <NotificationToast />
      <EmailVerificationBanner />
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
          path="/admin"
          element={
            <AdminRoute>
              <AdminDashboardPage />
            </AdminRoute>
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
          path="/admin/orders"
          element={
            <AdminRoute>
              <AdminOrdersPage />
            </AdminRoute>
          }
        />

        <Route
          path="/admin/customers"
          element={
            <AdminRoute>
              <AdminCustomersPage />
            </AdminRoute>
          }
        />

        <Route
          path="/admin/promotions"
          element={
            <AdminRoute>
              <AdminPromotionsPage />
            </AdminRoute>
          }
        />

        <Route
          path="/admin/broadcast"
          element={
            <AdminRoute>
              <AdminBroadcastPage />
            </AdminRoute>
          }
        />

        <Route
          path="/admin/categories"
          element={
            <AdminRoute>
              <AdminCategoriesPage />
            </AdminRoute>
          }
        />

        <Route
          path="/admin/audit-logs"
          element={
            <AdminRoute>
              <AdminAuditLogsPage />
            </AdminRoute>
          }
        />

        <Route
          path="/admin/notifications"
          element={
            <AdminRoute>
              <AdminNotificationsPage />
            </AdminRoute>
          }
        />

        <Route
          path="/admin/inventory-history"
          element={
            <AdminRoute>
              <AdminInventoryHistoryPage />
            </AdminRoute>
          }
        />

        <Route
          path="/admin/security"
          element={
            <AdminRoute>
              <AdminSecurityPage />
            </AdminRoute>
          }
        />

        <Route
          path="/admin/customer-controls"
          element={
            <AdminRoute>
              <AdminCustomerControlsPage />
            </AdminRoute>
          }
        />

        <Route
          path="*"
          element={
            <Navigate
              to="/"
              replace
            />
          }
        />
              <Route
          path="/auth/verify-email"
          element={<VerifyEmailPage />}
        />
</Routes>
    </>
  );
}


