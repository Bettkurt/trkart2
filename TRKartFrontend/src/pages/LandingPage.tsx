import React, { useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { logger } from '@/utils/logger';

const LandingPage: React.FC = () => {
  const location = useLocation();

  // Log component mount and page view
  useEffect(() => {
    logger.info('LandingPage', 'mount', 'Landing page loaded', { 
      path: location.pathname,
      userAgent: navigator.userAgent,
      screenSize: `${window.innerWidth}x${window.innerHeight}`
    });

    return () => {
      logger.debug('LandingPage', 'unmount', 'Landing page unmounting');
    };
  }, [location.pathname]);

  const handleNavigation = (target: string) => {
    logger.info('LandingPage', 'navigation', `Navigating to ${target}`, { from: 'LandingPage' });
  };

  const handleExternalLink = (url: string, label: string) => {
    logger.info('LandingPage', 'externalLink', `Opening external link: ${label}`, { url });
    // The actual navigation will be handled by the default link behavior
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

      {/* Hero Section */}
      <main className="flex-grow">
        <div className="relative w-screen left-1/2 -translate-x-1/2 h-[70vh] flex items-center justify-center overflow-hidden bg-white">
          <img
            className="absolute inset-0 w-full h-full object-cover"
            src="/assets/trkartt66.png"
            alt="TRKart Digital Banking"
          />
          <div className="absolute inset-0 bg-black bg-opacity-50" />
          <div className="relative z-10 flex flex-col items-center justify-center w-full h-full text-center">
            <h1 className="text-4xl font-extrabold tracking-tight sm:text-5xl lg:text-6xl text-white">
            Modern Digital Banking
            </h1>
            <p className="mt-6 text-xl text-gray-100 max-w-2xl mx-auto">
            TR Card delivers financial freedom. A fast, secure, and easy-to-use digital banking solution.
            </p>
          </div>
        </div>
      </main>
      {/* Info Section Before Footer */}
      <section className="bg-[#f3f8fa] w-full py-16 px-4">
        <div className="max-w-6xl mx-auto">
          <h1 className="text-5xl font-extrabold text-#ca8a04 mb-8 text-center md:text-left">Türkiye Card</h1>
          <p className="text-gray-700 text-lg mb-12 text-center md:text-left">
            In today’s world, where domestic mobility is increasing, transportation options are expanding, and technology is advancing rapidly, citizens using urban public transportation in different cities often find themselves needing new payment methods frequently. To streamline this process, the Turkey Card will be introduced in collaboration with various municipalities and institutions, offering a unified card product. This card will allow citizens not only to use it for public transportation but also to perform banking transactions. Moreover, thanks to PTT’s extensive network of offices and alternative transaction channels, accessing the card product and related services will be straightforward. The physical Turkey Card will be integrated with a newly developed mobile application, enabling users to conduct banking, transportation, and online balance top-up operations through their mobile devices. This integration is designed to provide significant convenience in daily life.
          </p>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-12">
            <div>
              <h2 className="text-3xl font-bold text-#ca8a04 mb-4">Application Centers</h2>
              <ul className="list-disc list-inside text-gray-800 mb-4">
                <li>
                  <a 
                    href="#" 
                    onClick={(e) => {
                      e.preventDefault();
                      handleExternalLink('#', 'PTT Branches');
                    }}
                    className="hover:underline"
                  >
                    All PTT Branches across Türkiye
                  </a>
                </li>
                <li>
                  <a 
                    href="#" 
                    onClick={(e) => {
                      e.preventDefault();
                      handleExternalLink('#', 'Türkiye Card Mobile App');
                    }}
                    className="hover:underline"
                  >
                    Türkiye Card Mobile Application or Türkiye Card Website
                  </a>
                </li>
              </ul>
              <a 
                href="#" 
                onClick={(e) => {
                  e.preventDefault();
                  handleExternalLink('#', 'Application Centers');
                }}
                className="text-#ca8a04 font-medium hover:underline inline-flex items-center"
              >
                Application Centers <span className="ml-1">&rarr;</span>
              </a>
            </div>
            <div>
              <h2 className="text-3xl font-bold text-#ca8a04 mb-4">Loading/Payment Centers</h2>
              <a 
                href="#" 
                onClick={(e) => {
                  e.preventDefault();
                  handleExternalLink('#', 'Loading/Payment Centers');
                }}
                className="text-#ca8a04 font-medium hover:underline inline-flex items-center mt-2"
              >
                Loading/Payment Centers <span className="ml-1">&rarr;</span>
              </a>
            </div>
          </div>
        </div>
      </section>
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
                handleExternalLink('#', 'About (Footer)');
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
              onClick={() => handleNavigation('Login (Footer)')}
              className="text-#ca8a04 text-xl font-medium hover:underline inline-flex items-center mt-2"
            >
              Login <span className="ml-1">&rarr;</span>
            </Link>
            <Link 
              to="/register" 
              onClick={() => handleNavigation('Register (Footer)')}
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

export default LandingPage;
