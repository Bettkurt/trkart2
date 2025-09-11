import React, { useEffect, useState, useRef } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { logger } from '@/utils/logger';
import { useMatchCardHeights } from '@/hooks/useMatchCardHeights';

const DashboardPage: React.FC = () => {
  const { user, logout } = useAuth();
  const location = useLocation();
  const [expanded, setExpanded] = useState(false);
  const quickActionsRef = useRef<HTMLDivElement>(null);
  const transactionsRef = useRef<HTMLAnchorElement>(null);
  const myCardsRef = useRef<HTMLAnchorElement>(null);
  
  // Use the height matching hook
  useMatchCardHeights(expanded);

  // Log component mount and user info
  useEffect(() => {
    logger.info('DashboardPage', 'mount', 'Dashboard page loaded', { 
      path: location.pathname,
      hasUser: !!user,
      userEmail: user?.email
    });

    return () => {
      logger.debug('DashboardPage', 'unmount', 'Dashboard page unmounting');
    };
  }, [location.pathname, user]);

  const handleLogout = async () => {
    logger.info('DashboardPage', 'logout', 'User initiated logout', { userEmail: user?.email });
    try {
      await logout();
      logger.info('DashboardPage', 'logout', 'Logout successful');
    } catch (error) {
      logger.error('DashboardPage', 'logout', 'Logout failed', error instanceof Error ? error : new Error(String(error)));
    }
  };

  const handleNavigation = (target: string) => {
    logger.info('DashboardPage', 'navigation', `Navigating to ${target}`, { from: 'DashboardPage' });
  };

  const [showSettings, setShowSettings] = useState(false);

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="w-full px-4 sm:px-6 lg:px-8">
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
              
              {/* Wallet Button */}
              <Link 
                to="/wallet" 
                className="btn-primary px-6 py-2 text-base"
                onClick={() => handleNavigation('wallet')}
              >
                Wallet
              </Link>
              
              {/* Settings button with dropdown */}
              <div className="relative">
                <button
                  onClick={() => setShowSettings(s => !s)}
                  className="btn-secondary px-6 py-2 text-base"
                  data-testid="settings-button"
                >
                  Settings
                </button>
                {showSettings && (
                  <div className="absolute right-0 mt-2 w-48 bg-white border border-gray-200 rounded shadow-md z-50">
                    <Link
                      to="/change-password"
                      className="block px-4 py-2 text-gray-700 hover:bg-gray-100"
                      onClick={() => setShowSettings(false)}
                    >
                      Change Password
                    </Link>
                    <Link
                      to="/change-email"
                      className="block px-4 py-2 text-gray-700 hover:bg-gray-100"
                      onClick={() => setShowSettings(false)}
                    >
                      Change Email
                    </Link>
                  </div>
                )}
              </div>
              <button
                onClick={handleLogout}
                className="bg-red-700 hover:bg-red-800 text-white px-6 py-2 text-base rounded"
                data-testid="logout-button"
              >
                Logout
              </button>
            </div>
          </div>
        </div>
      </nav>

      <div className="flex">
        {/* Left Sidebar Navigation */}
        <div className="w-80 bg-white shadow-sm min-h-screen p-6">
          <div className="flex flex-col space-y-4">
            {/* Quick Actions - Expandable */}
            <div className="w-full" ref={quickActionsRef}>
              <div className={`card quick-actions-card bg-yellow-400 border-yellow-600 overflow-hidden transition-all duration-300 ease-in-out ${expanded ? 'open' : ''}`}>
                <button 
                  className="w-full text-left p-6 focus:outline-none"
                  onClick={() => setExpanded(!expanded)}
                  onKeyDown={(e) => e.key === 'Enter' && setExpanded(!expanded)}
                  aria-expanded={expanded}
                  aria-controls="quick-actions-content"
                >
                  <div className="flex justify-between items-center">
                    <div>
                      <h3 className="text-xl font-medium text-gray-900">Quick Actions</h3>
                      <p className="text-gray-600 mt-1 text-base">Perform common actions quickly</p>
                    </div>
                    <span className={`chevron transition-transform duration-300 ease-in-out ${expanded ? 'rotate-90' : ''}`}>
                      &#9654;
                    </span>
                  </div>
                </button>
                <div className="card-body p-0">
                  <div className="collapsed-content overflow-hidden transition-[height] duration-300 ease-in-out">
                    {/* Minimal height for collapsed state */}
                    <div className="h-4"></div>
                  </div>
                  <div 
                    className={`quick-actions-submenu ${expanded ? 'open' : ''}`}
                    style={{
                      maxHeight: expanded ? '1000px' : '0',
                      paddingTop: expanded ? '0.5rem' : '0',
                      paddingBottom: expanded ? '1.5rem' : '0',
                      marginTop: '0.5rem',
                      transition: 'max-height 0.3s ease, padding 0.3s ease',
                      overflow: 'hidden'
                    }}
                  >
                    <div className="space-y-4 px-6" onClick={(e) => e.stopPropagation()}>
                      <Link 
                        to="/new-transaction" 
                        className="btn-primary w-full block text-center py-3 text-base transition-all duration-200 hover:translate-y-[-2px] hover:shadow-md"
                        onClick={(e) => e.stopPropagation()}
                      >
                        New Transaction
                      </Link>
                      <Link 
                        to="/top-up" 
                        className="bg-green-600 hover:bg-green-700 text-white w-full block text-center py-3 text-base rounded-md font-medium transition-all duration-200 hover:translate-y-[-2px] hover:shadow-md"
                        onClick={(e) => e.stopPropagation()}
                      >
                        💳 Top-Up Card
                      </Link>
                      <Link 
                        to="/create-card" 
                        className="btn-secondary w-full block text-center py-3 text-base transition-all duration-200 hover:translate-y-[-2px] hover:shadow-md"
                        onClick={(e) => e.stopPropagation()}
                      >
                        Add New Card
                      </Link>
                      <Link 
                        to="/new-transfer" 
                        className="btn-primary w-full block text-center py-3 text-base transition-all duration-200 hover:translate-y-[-2px] hover:shadow-md"
                        onClick={(e) => e.stopPropagation()}
                      >
                        New Transfer
                      </Link>
                      <Link 
                        to="/cards-new" 
                        className="btn-secondary w-full block text-center py-3 text-base transition-all duration-200 hover:translate-y-[-2px] hover:shadow-md"
                        onClick={(e) => e.stopPropagation()}
                      >
                        View New Card Design
                      </Link>
                    </div>
                  </div>
                </div>
              </div>
            </div>
            
            {/* Transactions */}
            <Link 
              to="/transactions" 
              ref={transactionsRef}
              className="card hover:shadow-lg transition-shadow p-6 bg-yellow-400 border-yellow-600"
            >
              <div className="ml-5 mt-2.5">
                <h3 className="text-xl font-medium text-gray-900">Transactions</h3>
                <p className="text-gray-600 mt-3 text-base">View your transaction history</p>
              </div>
            </Link>
            
            <Link 
              to="/cards" 
              ref={myCardsRef}
              className="card hover:shadow-lg transition-shadow p-6 bg-yellow-400 border-yellow-600"
            >
              <div className="ml-5 mt-2.5">
                <h3 className="text-xl font-medium text-gray-900">My Cards</h3>
                <p className="text-gray-600 mt-3 text-base">Manage your payment cards</p>
              </div>
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