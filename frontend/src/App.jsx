import { Navigate, Route, Routes } from "react-router-dom";

import HomePage from "./pages/HomePage.jsx";
import LoggedInHomePage from "./pages/LoggedInHomePage.jsx";
import LoginPage from "./pages/auth/LoginPage.jsx";
import SignupPage from "./pages/auth/SignupPage.jsx";
import ForgotPasswordPage from "./pages/auth/ForgotPasswordPage.jsx";
import ResetPasswordPage from "./pages/auth/ResetPasswordPage.jsx";
import ProfilePage from "./pages/ProfilePage.jsx";
import ProductDetailsPage from "./pages/ProductDetailsPage.jsx";
import ProtectedRoute from "./routes/ProtectedRoute.jsx";
import CartPage from "./pages/CartPage.jsx";

export default function App() {
  return (
    <Routes>
      {/* Public homepage */}
      <Route path="/" element={<HomePage />} />

      {/* Authentication pages */}
      <Route path="/auth/login" element={<LoginPage />} />
      <Route path="/auth/signup" element={<SignupPage />} />

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

      {/* Protected homepage after login */}
      <Route
        path="/shop"
        element={
          <ProtectedRoute>
            <LoggedInHomePage />
          </ProtectedRoute>
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
    <ProtectedRoute>
      <CartPage />
    </ProtectedRoute>
  }
/>
      {/* Protected profile */}
      <Route
        path="/profile"
        element={
          <ProtectedRoute>
            <ProfilePage />
          </ProtectedRoute>
        }
      />

      {/* Unknown links return to public homepage */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}