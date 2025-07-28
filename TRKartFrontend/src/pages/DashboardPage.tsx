import React from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';

const DashboardPage: React.FC = () => {
  const { user, logout } = useAuth();

  const handleLogout = async () => {
    await logout();
  };

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-xl font-semibold text-gray-900">TRKart Dashboard</h1>
            </div>
            <div className="flex items-center space-x-4">
              <span className="text-gray-700">Welcome, {user?.email}</span>
              <button
                onClick={handleLogout}
                className="btn-secondary"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            <Link to="/cards" className="card hover:shadow-lg transition-shadow">
              <h3 className="text-lg font-medium text-gray-900">My Cards</h3>
              <p className="text-gray-600 mt-2">Manage your payment cards</p>
            </Link>
            
            <Link to="/transactions" className="card hover:shadow-lg transition-shadow">
              <h3 className="text-lg font-medium text-gray-900">Transactions</h3>
              <p className="text-gray-600 mt-2">View your transaction history</p>
            </Link>
            
            <div className="card">
              <h3 className="text-lg font-medium text-gray-900">Quick Actions</h3>
              <div className="mt-4 space-y-2">
                <button className="btn-primary w-full">New Transaction</button>
                <button className="btn-secondary w-full">Add New Card</button>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default DashboardPage; 