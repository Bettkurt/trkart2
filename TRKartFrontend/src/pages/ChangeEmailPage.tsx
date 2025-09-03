import React, { useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useNavigate } from 'react-router-dom';
import { Eye, EyeOff } from 'lucide-react';

const ChangeEmailPage: React.FC = () => {
  const { changeEmail } = useAuth();
  const navigate = useNavigate();
  const [password, setPassword] = useState('');
  const [newEmail, setNewEmail] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccess('');

    if (!password || !newEmail) {
      setError('Both fields are required.');
      return;
    }
    if (!newEmail.includes('@') || !newEmail.includes('.')) {
      setError('Please enter a valid email.');
      return;
    }

    setLoading(true);
    try {
      await changeEmail(password, newEmail);
      setSuccess('Email changed successfully! Updating your session...');
      setTimeout(() => {
        navigate('/dashboard');
      }, 1200);
      setPassword('');
      setNewEmail('');
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Failed to change email.');
    } finally {
      setLoading(false);
    }
  };

  const handleBack = () => {
    navigate('/dashboard');
  };

  const inputClass = 'w-full px-4 py-2 border rounded focus:outline-none focus:ring-2 focus:ring-yellow-400';
  const labelClass = 'block text-gray-700 font-medium mb-2';

  return (
    <div className="flex items-center justify-center min-h-screen bg-gray-50">
      <div className="bg-white p-8 rounded shadow-md w-full max-w-md relative">
        {/* Back to Dashboard button in upper left */}
        <button
          type="button"
          className="flex items-center text-yellow-600 hover:text-yellow-800 font-medium mb-6 focus:outline-none"
          style={{ position: 'absolute', top: 24, left: 24 }}
          onClick={handleBack}
          disabled={loading}
        >
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" className="w-5 h-5 mr-2">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
          Back to Dashboard
        </button>
        <form onSubmit={handleSubmit} className="pt-12">
          <h2 className="text-2xl font-bold mb-6 text-center">Change Email</h2>
          {error && <div className="mb-4 text-red-600 text-center">{error}</div>}
          {success && <div className="mb-4 text-green-600 text-center">{success}</div>}
          <div className="mb-4 relative">
            <label className={labelClass} htmlFor="password">Password</label>
            <input
              id="password"
              type={showPassword ? 'text' : 'password'}
              className={inputClass}
              value={password}
              onChange={e => setPassword(e.target.value)}
              autoComplete="current-password"
            />
            <button type="button" onClick={() => setShowPassword(s => !s)} className="absolute right-3 top-11 text-gray-500 focus:outline-none">
              {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
            </button>
          </div>
          <div className="mb-6">
            <label className={labelClass} htmlFor="newEmail">New Email</label>
            <input
              id="newEmail"
              type="email"
              className={inputClass}
              value={newEmail}
              onChange={e => setNewEmail(e.target.value)}
              autoComplete="email"
            />
          </div>
          <button
            type="submit"
            className="btn-primary w-full py-3 text-base mb-3"
            disabled={loading}
          >
            {loading ? 'Changing...' : 'Change Email'}
          </button>
        </form>
      </div>
    </div>
  );
};

export default ChangeEmailPage;


