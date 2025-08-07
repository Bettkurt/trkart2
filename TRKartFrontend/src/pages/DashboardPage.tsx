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
          <div className="flex justify-between h-20">
            <div className="flex items-center space-x-4">
              <img 
                src="/assets/logo.png" 
                alt="TRKart Logo" 
                className="h-12 w-auto"
              />
              <h1 className="text-2xl font-semibold text-gray-900">TRKart Dashboard</h1>
            </div>
            <div className="flex items-center space-x-6">
              <span className="text-lg text-gray-700">Welcome, {user?.email}</span>
              <button
                onClick={handleLogout}
                className="btn-secondary px-6 py-2 text-base"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </nav>

      <div className="flex">
        {/* Left Sidebar Navigation */}
        <div className="w-80 bg-white shadow-sm min-h-screen p-8">
          <div className="flex flex-col space-y-6">
            {/* Quick Actions - Bottom */}
            <div className="card p-6 bg-yellow-400 border-yellow-600">
              <h3 className="text-xl font-medium text-gray-900 mb-6">Quick Actions</h3>
              <div className="space-y-4">
                <Link to="/new-transaction" className="btn-primary w-full block text-center py-3 text-base">New Transaction</Link>
                <Link to="/new-transfer" className="btn-primary w-full block text-center py-3 text-base">New Transfer</Link>
                <Link to="/cards" className="btn-secondary w-full block text-center py-3 text-base">Add New Card</Link>
              </div>
            </div>
            
            {/* Transactions - Middle */}
            <Link to="/transactions" className="card hover:shadow-lg transition-shadow p-6 bg-yellow-400 border-yellow-600">
              <h3 className="text-xl font-medium text-gray-900">Transactions</h3>
              <p className="text-gray-600 mt-3 text-base">View your transaction history</p>
            </Link>
            
            {/* My Cards - Top */}
            <Link to="/cards" className="card hover:shadow-lg transition-shadow p-6 bg-yellow-400 border-yellow-600">
              <h3 className="text-xl font-medium text-gray-900">My Cards</h3>
              <p className="text-gray-600 mt-3 text-base">Manage your payment cards</p>
            </Link>
          </div>
        </div>

        {/* Main Content Area */}
        <div className="flex-1 p-8">
          <div className="max-w-5xl mx-auto">
            <div className="bg-white rounded-lg shadow-sm p-8">
              <h2 className="text-3xl font-bold text-gray-900 mb-6">Welcome to TRKart</h2>
              <p className="text-gray-600 mb-8 text-lg">
                Manage your cards, view transactions, and perform quick actions from the navigation panel on the left.
              </p>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
                <div className="bg-blue-50 rounded-lg p-6">
                  <h3 className="text-xl font-semibold text-blue-900 mb-3">Recent Activity</h3>
                  <p className="text-blue-700 text-base">Check your latest transactions and card activities here.</p>
                </div>
                
                <div className="bg-green-50 rounded-lg p-6">
                  <h3 className="text-xl font-semibold text-green-900 mb-3">Quick Stats</h3>
                  <p className="text-green-700 text-base">View your account summary and statistics.</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default DashboardPage; 