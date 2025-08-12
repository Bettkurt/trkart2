import React, { useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { logger } from '@/utils/logger';

const AboutPage: React.FC = () => {
  const location = useLocation();
  
  useEffect(() => {
    logger.info('AboutPage', 'mount', 'About page loaded', { path: location.pathname });
    
    return () => {
      logger.debug('AboutPage', 'unmount', 'About page unmounting');
    };
  }, [location.pathname]);
  
  const handleNavigation = (target: string) => {
    logger.info('AboutPage', 'navigation', `Navigating to ${target}`, { from: 'AboutPage' });
  };
  
  const handleExternalLink = (url: string, label: string) => {
    logger.info('AboutPage', 'externalLink', `Opening external link: ${label}`, { url });
    // The actual navigation will be handled by the browser
  };
  return (
    <div className="min-h-screen flex flex-col">
      {/* Navigation */}
      <nav className="bg-white">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-24">
            {/* Logo */}
            <div className="flex-shrink-0 flex items-center">
              <img
                className="h-20 w-auto"
                src="/assets/logo.png"
                alt="TRKart Logo"
              />
            </div>
            {/* Navigation Links */}
            <div className="flex-1 flex justify-center">
              <div className="flex space-x-12">
                <Link 
                  to="/" 
                  onClick={() => handleNavigation('Home')} 
                  className="text-xl font-bold text-gray-600 hover:text-yellow-600 transition-colors"
                >
                  Home
                </Link>
                <Link 
                  to="/about" 
                  onClick={() => handleNavigation('About')}
                  className="text-xl font-bold text-gray-600 hover:text-yellow-600 transition-colors"
                >
                  About
                </Link>
                <a 
                  href="#" 
                  onClick={(e) => {
                    e.preventDefault();
                    handleExternalLink('#', 'Application & Loading Centers');
                  }}
                  className="text-xl font-bold text-gray-600 hover:text-yellow-600 transition-colors"
                >
                  Application & Loading Centers
                </a>
                <a 
                  href="#" 
                  onClick={(e) => {
                    e.preventDefault();
                    handleExternalLink('#', 'Contact');
                  }}
                  className="text-xl font-bold text-gray-600 hover:text-yellow-600 transition-colors"
                >
                  Contact
                </a>
              </div>
            </div>
            {/* Login/Register Buttons */}
            <div className="flex items-center space-x-4">
              <Link 
                to="/login" 
                onClick={() => handleNavigation('Login')}
                className="px-4 py-2 text-base font-medium text-gray-700 hover:text-black-600 transition-colors"
              >
                Login
              </Link>
              <Link 
                to="/register" 
                onClick={() => handleNavigation('Register')}
                className="px-4 py-2 text-base font-medium text-white bg-yellow-600 rounded-md hover:bg-black transition-colors"
              >
                Register
              </Link>
            </div>
          </div>
        </div>
      </nav>
      {/* Main Content */}
      <main className="flex-grow flex flex-col items-center justify-center bg-[#f3f8fa] px-4 py-16">
        <div className="max-w-2xl mx-auto bg-white rounded-xl shadow p-8">
          <h1 className="text-4xl font-extrabold text-yellow-600 mb-6 text-center">About us</h1>
          <p className="text-gray-700 text-lg text-center">
            The aim of the 'Turkey Card' project, prepared within the scope of the 'National Intelligent Transportation System', is to create a national platform and provide benefits to the National Economy and citizens. In this context, efforts are underway to make all kinds of financial transactions with a single card and to create a single payment system that can be used in public transportation vehicles of all cities, in a way that will not victimize the users who have the transportation cards of the municipalities. In this context, we work in coordination and integration with municipalities and all stakeholders operating in the sector.
          </p>
        </div>
      </main>
      {/* Footer */}
      <footer className="bg-[#f3f8fa] py-10 shadow-[0_-2px_10px_rgba(0,0,0,0.1)] relative min-h-[200px] mt-auto">
        <div className="max-w-6xl mx-auto grid grid-cols-1 md:grid-cols-3 gap-8 md:gap-16 items-start h-full px-4">
          {/* Logo Section */}
          <div className="flex flex-col justify-end items-start md:items-start logo-section">
            <div className="flex items-center gap-3 logo">
              <img src="/assets/logo.png" alt="Türkiye Kart Logo" className="w-32 h-32 object-contain" />
            </div>
          </div>
          {/* Corporate Section */}
          <div className="flex flex-col gap-2 nav-section items-start md:items-center">
            <h3 className="font-semibold text-3xl text-gray-800 section-title mb-1">Corporate</h3>
            <a 
              href="#" 
              onClick={(e) => {
                e.preventDefault();
                handleExternalLink('#', 'About (footer)');
              }}
              className="text-gray-500 text-xl hover:text-yellow-500 nav-link transition-colors"
            >
              About
            </a>
            <a 
              href="#" 
              onClick={(e) => {
                e.preventDefault();
                handleExternalLink('#', 'Press');
              }}
              className="text-gray-500 text-xl hover:text-yellow-500 nav-link transition-colors"
            >
              Press
            </a>
          </div>
          {/* Online Section */}
          <div className="flex flex-col gap-2 nav-section items-start md:items-center">
            <h3 className="font-semibold text-3xl text-gray-800 section-title mb-1">Online</h3>
            <Link 
              to="/login" 
              onClick={() => handleNavigation('Login (footer)')}
              className="text-#ca8a04 text-xl font-medium hover:underline inline-flex items-center mt-2"
            >
              Login <span className="ml-1">&rarr;</span>
            </Link>
            <Link 
              to="/register" 
              onClick={() => handleNavigation('Register (footer)')}
              className="text-#ca8a04 text-xl font-medium hover:underline inline-flex items-center mt-2"
            >
              Register <span className="ml-1">&rarr;</span>
            </Link>
          </div>
        </div>
      </footer>
    </div>
  );
};

export default AboutPage; 