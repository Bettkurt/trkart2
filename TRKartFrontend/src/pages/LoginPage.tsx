import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { Eye, EyeOff } from 'lucide-react';

const LoginPage: React.FC = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');

  const { login, hasValidSession, sessionEmail, getRememberedEmail } = useAuth();
  const navigate = useNavigate();

  // Check for remembered email on component mount
  useEffect(() => {
    const rememberedEmail = getRememberedEmail();
    if (rememberedEmail) {
      setEmail(rememberedEmail);
      setRememberMe(true);
    }
  }, [getRememberedEmail]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!email || !password) {
      setError('Please enter both email and password');
      return;
    }
    
    setIsLoading(true);
    setError('');
    
    try {
      await login({ email, password, rememberMe });
      navigate('/dashboard');
    } catch (err) {
      console.error('Login error:', err);
      setError('Login failed. Please check your credentials.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="flex min-h-screen bg-white">
      {/* Left: Logo */}
      <div className="hidden md:flex flex-col justify-center items-center w-1/2 bg-white">
        <Link to="/" className="hover:opacity-90 transition-opacity">
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
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center text-gray-600 hover:text-gray-800"
                  tabIndex={-1}
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
                <a href="#" className="text-cyan-500 font-semibold text-base hover:underline">Forgot my password ?</a>
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