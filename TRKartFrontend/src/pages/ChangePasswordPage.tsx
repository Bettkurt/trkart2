import React, { useState } from 'react';
import { useAuth } from '@/contexts/AuthContext';
import { useNavigate } from 'react-router-dom';
import { Eye, EyeOff } from 'lucide-react';

const ChangePasswordPage: React.FC = () => {
  const { changePassword } = useAuth();
  const navigate = useNavigate();
  const [oldPassword, setOldPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showOld, setShowOld] = useState(false);
  const [showNewPasswords, setShowNewPasswords] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    if (newPassword !== confirmPassword) {
      setError('New passwords do not match.');
      return;
    }
    if (!oldPassword || !newPassword || !confirmPassword) {
      setError('All fields are required.');
      return;
    }
    if (oldPassword === newPassword) {
      setError('New password must be different from the old password.');
      return;
    }
    setLoading(true);
    try {
      await changePassword(oldPassword, newPassword);
      setSuccess('Password changed successfully! Redirecting to dashboard...');
      setTimeout(() => {
        navigate('/dashboard');
      }, 1500);
      setOldPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (err: any) {
      setError(err?.response?.data?.message || 'Failed to change password.');
    } finally {
      setLoading(false);
    }
  };

  const handleBack = () => {
    navigate('/dashboard');
  };

  const inputClass = 'w-full px-4 py-2 border rounded focus:outline-none focus:ring-2 focus:ring-yellow-400';
  const labelClass = 'block text-gray-700 font-medium mb-2';
  const eyeButton = (show: boolean, setShow: (v: boolean) => void) => (
    <button type="button" onClick={() => setShow(!show)} className="absolute right-3 top-3 text-gray-500 focus:outline-none">
      {show ? <EyeOff size={18} /> : <Eye size={18} />}
    </button>
  );

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
          <h2 className="text-2xl font-bold mb-6 text-center">Change Password</h2>
          {error && <div className="mb-4 text-red-600 text-center">{error}</div>}
          {success && <div className="mb-4 text-green-600 text-center">{success}</div>}
          <div className="mb-4 relative">
            <label className={labelClass} htmlFor="oldPassword">Old Password</label>
            <input
              id="oldPassword"
              type={showOld ? 'text' : 'password'}
              className={inputClass}
              value={oldPassword}
              onChange={e => setOldPassword(e.target.value)}
              autoComplete="current-password"
            />
            {eyeButton(showOld, setShowOld)}
          </div>
          <div className="mb-4 relative">
            <label className={labelClass} htmlFor="newPassword">New Password</label>
            <input
              id="newPassword"
              type={showNewPasswords ? 'text' : 'password'}
              className={inputClass}
              value={newPassword}
              onChange={e => setNewPassword(e.target.value)}
              autoComplete="new-password"
            />
            {eyeButton(showNewPasswords, setShowNewPasswords)}
          </div>
          <div className="mb-6 relative">
            <label className={labelClass} htmlFor="confirmPassword">Confirm New Password</label>
            <input
              id="confirmPassword"
              type={showNewPasswords ? 'text' : 'password'}
              className={inputClass}
              value={confirmPassword}
              onChange={e => setConfirmPassword(e.target.value)}
              autoComplete="new-password"
            />
          </div>
          <button
            type="submit"
            className="btn-primary w-full py-3 text-base mb-3"
            disabled={loading}
          >
            {loading ? 'Changing...' : 'Change Password'}
          </button>
        </form>
      </div>
    </div>
  );
};

export default ChangePasswordPage;
