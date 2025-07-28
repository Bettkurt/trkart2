import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from '@/contexts/AuthContext';
import LoginPage from '@/pages/LoginPage';
import RegisterPage from '@/pages/RegisterPage';
import DashboardPage from '@/pages/DashboardPage';
import TransactionHistoryPage from '@/pages/TransactionHistoryPage';
import UserCardsPage from '@/pages/UserCardsPage';
import LoadingSpinner from '@/components/LoadingSpinner';

// Protected Route Component
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <LoadingSpinner />;
  }

  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
};

// Public Route Component (redirects to dashboard if already authenticated)
const PublicRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <LoadingSpinner />;
  }

  return isAuthenticated ? <Navigate to="/dashboard" replace /> : <>{children}</>;
};

const AppRoutes: React.FC = () => {
  return (
    <Router>
      <Routes>
        <Route path="/login" element={
          <PublicRoute>
            <LoginPage />
          </PublicRoute>
        } />
        <Route path="/register" element={
          <PublicRoute>
            <RegisterPage />
          </PublicRoute>
        } />
        <Route path="/dashboard" element={
          <ProtectedRoute>
            <DashboardPage />
          </ProtectedRoute>
        } />
        <Route path="/transactions" element={
          <ProtectedRoute>
            <TransactionHistoryPage />
          </ProtectedRoute>
        } />
        <Route path="/cards" element={
          <ProtectedRoute>
            <UserCardsPage />
          </ProtectedRoute>
        } />
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </Router>
  );
};

const App: React.FC = () => {
  return (
    <AuthProvider>
      <AppRoutes />
    </AuthProvider>
  );
};

export default App; 