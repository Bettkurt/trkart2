import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';

import { AuthProvider, useAuth } from '@/contexts/AuthContext';
import LandingPage from '@/pages/LandingPage';
import LoginPage from '@/pages/LoginPage';
import RegisterPage from '@/pages/RegisterPage';
import DashboardPage from '@/pages/DashboardPage';
import UserCardsPage from '@/pages/UserCardsPage';
import NewUserCardsPage from '@/pages/NewUserCardsPage';
import TransactionTestPage from '@/pages/TransactionTestPage'; // Fixed import path
import TransactionsPage from '@/pages/TransactionsPage';
import TransactionFormPage from '@/pages/TransactionFormPage';
import TopUpPage from '@/pages/TopUpPage';
import CardCreationPage from '@/pages/CardCreationPage';
import CardDeletionPage from '@/pages/CardDeletionPage';
import LostCardPage from '@/pages/LostCardPage';
import NewTransferPage from '@/pages/NewTransferPage';
import TransfersPage from '@/pages/TransfersPage';
import AboutPage from '@/pages/AboutPage';
import ChangePasswordPage from '@/pages/ChangePasswordPage';
import ChangeEmailPage from '@/pages/ChangeEmailPage';
import WalletPage from '@/pages/WalletPage';
import WalletTransactionsPage from '@/pages/WalletTransactionsPage';

// Protected Route Component
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <div className="flex items-center justify-center min-h-screen">Loading...</div>;
  }

  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
};

// Public Route Component
const PublicRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return <div className="flex items-center justify-center min-h-screen">Loading...</div>;
  }

  return isAuthenticated ? <Navigate to="/dashboard" replace /> : <>{children}</>;
};

const AppRoutes: React.FC = () => {
  return (
    <Routes>
      <Route path="/" element={
        <PublicRoute>
          <LandingPage />
        </PublicRoute>
      } />
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
          <TransactionsPage />
        </ProtectedRoute>
      } />
      <Route path="/new-transaction" element={
        <ProtectedRoute>
          <TransactionFormPage />
        </ProtectedRoute>
      } />
      <Route path="/top-up" element={
        <ProtectedRoute>
          <TopUpPage />
        </ProtectedRoute>
      } />
      <Route path="/new-transfer" element={
        <ProtectedRoute>
          <NewTransferPage />
        </ProtectedRoute>
      } />
      <Route path="/transfers" element={
        <ProtectedRoute>
          <TransfersPage />
        </ProtectedRoute>
      } />
      <Route path="/cards" element={
        <ProtectedRoute>
          <UserCardsPage />
        </ProtectedRoute>
      } />
      <Route path="/cards-new" element={
        <ProtectedRoute>
          <NewUserCardsPage />
        </ProtectedRoute>
      } />
      <Route path="/wallet" element={
        <ProtectedRoute>
          <WalletPage />
        </ProtectedRoute>
      } />
      <Route path="/wallet/transactions" element={
        <ProtectedRoute>
          <WalletTransactionsPage />
        </ProtectedRoute>
      } />
      <Route path="/create-card" element={
        <ProtectedRoute>
          <CardCreationPage />
        </ProtectedRoute>
      } />
      <Route path="/delete-card/:cardId" element={
        <ProtectedRoute>
          <CardDeletionPage />
        </ProtectedRoute>
      } />
      <Route path="/cards/lost/:cardId" element={
        <ProtectedRoute>
          <LostCardPage />
        </ProtectedRoute>
      } />
      <Route path="/transaction-test" element={
        <TransactionTestPage />
      } />
      <Route path="/about" element={<AboutPage />} />
      <Route path="/change-password" element={
        <ProtectedRoute>
          <ChangePasswordPage />
        </ProtectedRoute>
      } />
      <Route path="/change-email" element={
        <ProtectedRoute>
          <ChangeEmailPage />
        </ProtectedRoute>
      } />
      {/* Catch all route - redirect to home instead of dashboard */}
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
};

const App: React.FC = () => {
  return (
    <Router>
      <AuthProvider>
        <AppRoutes />
      </AuthProvider>
    </Router>
  )
}

export default App;