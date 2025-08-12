import React, { useState, useEffect } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { Eye, EyeOff } from 'lucide-react';
import { logger } from '@/utils/logger';

const LoginPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const { login, hasValidSession, sessionEmail, getRememberedEmail } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  // Check for remembered email on component mount
  useEffect(() => {
    logger.info('LoginPage', 'mount', 'Login page loaded', { 
      path: location.pathname,
      hasValidSession,
      sessionEmail: sessionEmail ? '***' + sessionEmail.slice(-4) : null
    });

    const rememberedEmail = getRememberedEmail();
    if (rememberedEmail) {
      logger.debug('LoginPage', 'rememberedEmail', 'Found remembered email', { 
        email: rememberedEmail ? '***' + rememberedEmail.split('@')[0].slice(-4) + '@' + rememberedEmail.split('@')[1] : null 
      });
      setEmail(rememberedEmail);
      setRememberMe(true);
    }

    return () => {
      logger.debug('LoginPage', 'unmount', 'Login page unmounting');
    };
  }, [getRememberedEmail, hasValidSession, location.pathname, sessionEmail]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!email || !password) {
      const errorMsg = 'Please enter both email and password';
      logger.warn('LoginPage', 'validation', errorMsg, { 
        hasEmail: !!email, 
        hasPassword: !!password 
      });
      setError(errorMsg);
      return;
    }
    
    setIsLoading(true);
    setError('');
    
    logger.info('LoginPage', 'login', 'Login attempt started', { 
      email: email ? '***' + email.split('@')[0].slice(-4) + '@' + email.split('@')[1] : null,
      rememberMe,
      hasSession: hasValidSession
    });
    
    try {
      await login({ email, password, rememberMe });
      logger.info('LoginPage', 'login', 'Login successful', { 
        email: email ? '***' + email.split('@')[0].slice(-4) + '@' + email.split('@')[1] : null,
        rememberMe
      });
      navigate('/dashboard');
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      logger.error('LoginPage', 'login', 'Login failed', error, { 
        email: email ? '***' + email.split('@')[0].slice(-4) + '@' + email.split('@')[1] : null,
        rememberMe
      });
      setError('Login failed. Please check your credentials.');
    } finally {
      setIsLoading(false);
    }
  };

  const togglePasswordVisibility = () => {
    const newState = !showPassword;
    logger.debug('LoginPage', 'passwordVisibility', `Password visibility ${newState ? 'enabled' : 'disabled'}`);
    setShowPassword(newState);
  };

  const handleNavigation = (target: string) => {
    logger.info('LoginPage', 'navigation', `Navigating to ${target}`, { from: 'LoginPage' });
  };

  const handleForgotPassword = (e: React.MouseEvent) => {
    e.preventDefault();
    logger.info('LoginPage', 'forgotPassword', 'Forgot password link clicked');
    // TODO: Implement forgot password flow
    // Ask for email, verify if it is actually in the database
    // If it is, send a password reset email
    // If it is not, do NOT give any feedback
    // This is to prevent information disclosure
  };

  return (
    <div className="flex min-h-screen bg-white">
      {/* Left: Logo */}
      <div className="hidden md:flex flex-col justify-center items-center w-1/2 bg-white">
        <Link 
          to="/" 
          onClick={() => handleNavigation('Home')}
          className="hover:opacity-90 transition-opacity"
        >
          <img src="/assets/logo.png" alt="TR Türkiye Kart Logo" className="max-w-xs w-64" />
        </Link>
      </div>
      {/* Right: Login Form */}
      <div className="flex flex-1 items-center justify-center px-4">
        <div className="max-w-md w-full">
          <h1 className="text-2xl font-bold text-gray-800 mb-2">Login</h1>
          <p className="text-gray-400 mb-8 text-base">
            {hasValidSession && sessionEmail 
              ? `Welcome back! Please enter your password to continue.`
              : "You can log in with your registered email address and password."
            }
          </p>
          
          {hasValidSession && sessionEmail && (
            <div className="mb-6 p-4 bg-gray-50 rounded-lg">
              <p className="text-sm text-gray-600 mb-1">Logged in as:</p>
              <p className="text-base font-semibold text-gray-800">{sessionEmail}</p>
            </div>
          )}

          <form className="space-y-6" onSubmit={handleSubmit}>
            {error && (
              <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded text-base mb-2">
                {error}
              </div>
            )}
            
            {!hasValidSession && (
              <div>
                <label htmlFor="email" className="block text-sm font-semibold text-gray-700 mb-1">Email</label>
                <input
                  id="email"
                  name="email"
                  type="email"
                  required
                  className="w-full px-3 py-3 border-2 border-gray-300 rounded-xl text-lg placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-400 focus:border-transparent transition-all"
                  placeholder="Email address"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                />
              </div>
            )}
            
            <div>
              <label htmlFor="password" className="block text-sm font-semibold text-gray-700 mb-1">Password</label>
              <div className="relative">
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  required
                  className="w-full px-3 py-3 border-2 border-gray-300 rounded-xl text-lg placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-400 focus:border-transparent transition-all pr-10"
                  placeholder="Password"
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                />
                <button
                  type="button"
                  onClick={togglePasswordVisibility}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-600 hover:text-gray-800"
                  tabIndex={-1}
                  aria-label={showPassword ? 'Hide password' : 'Show password'}
                >
                  {showPassword ? <EyeOff size={20} /> : <Eye size={20} />}
                </button>
              </div>
              <div className="flex items-center justify-between mt-2">
                <div className="flex items-center">
                  <input
                    id="rememberMe"
                    name="rememberMe"
                    type="checkbox"
                    checked={rememberMe}
                    onChange={(e) => setRememberMe(e.target.checked)}
                    className="h-4 w-4 text-cyan-600 focus:ring-cyan-500 border-gray-300 rounded"
                  />
                  <label htmlFor="rememberMe" className="ml-2 block text-sm text-gray-700">
                    Remember me
                  </label>
                </div>
                <a 
                  href="#" 
                  onClick={handleForgotPassword}
                  className="text-cyan-500 font-semibold text-base hover:underline"
                >
                  Forgot my password ?
                </a>
              </div>
            </div>
            
            <button
              type="submit"
              disabled={isLoading}
              className="w-full bg-gray-800 text-white rounded-2xl py-4 font-bold text-lg shadow-md hover:bg-gray-900 transition-colors focus:outline-none focus:ring-2 focus:ring-gray-700 focus:ring-opacity-50 disabled:opacity-50 disabled:cursor-not-allowed mt-2"
            >
              {isLoading ? 'Logging in...' : 'Login'}
            </button>
          </form>
          
          <div className="text-center mt-16">
            <span className="text-gray-600 text-lg">Don't have an account? </span>
            <Link 
              to="/register" 
              onClick={() => handleNavigation('Register')}
              className="text-cyan-500 font-bold hover:underline transition-all text-lg"
            >
              Register
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LoginPage;