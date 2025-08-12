import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { logger } from '@/utils/logger';

const Header: React.FC = () => {
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      await logout();
      logger.info('Header', 'logout', 'User logged out successfully');
      navigate('/login');
    } catch (error) {
      const err = error as Error;
      logger.error('Header', 'logout', 'Error during logout', err);
    }
  };

  return (
    <header className="bg-white shadow">
      <div className="mx-auto max-w-7xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 justify-between">
          <div className="flex">
            <div className="flex flex-shrink-0 items-center">
              <Link to="/" className="text-xl font-bold text-blue-600">
                TRKart
              </Link>
            </div>
          </div>
          <div className="hidden sm:ml-6 sm:flex sm:items-center">
            <div className="relative ml-3">
              <div>
                <button
                  type="button"
                  className="flex rounded-full bg-white text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
                  id="user-menu-button"
                  aria-expanded={isUserMenuOpen}
                  aria-haspopup="true"
                  onClick={() => setIsUserMenuOpen(!isUserMenuOpen)}
                >
                  <span className="sr-only">Open user menu</span>
                  <div className="h-8 w-8 rounded-full bg-blue-500 flex items-center justify-center text-white">
                    {user?.email?.charAt(0).toUpperCase() || '?'}
                  </div>
                </button>
              </div>
              {isUserMenuOpen && (
                <div
                  className="absolute right-0 z-10 mt-2 w-48 origin-top-right rounded-md bg-white py-1 shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none"
                  role="menu"
                  aria-orientation="vertical"
                  aria-labelledby="user-menu-button"
                >
                  <Link
                    to="/dashboard"
                    className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                    role="menuitem"
                    onClick={() => setIsUserMenuOpen(false)}
                  >
                    Dashboard
                  </Link>
                  <Link
                    to="/security"
                    className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                    role="menuitem"
                    onClick={() => setIsUserMenuOpen(false)}
                  >
                    Security Settings
                  </Link>
                  <button
                    onClick={() => {
                      setIsUserMenuOpen(false);
                      handleLogout();
                    }}
                    className="block w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                    role="menuitem"
                  >
                    Sign out
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </header>
  );
};

export default Header;
