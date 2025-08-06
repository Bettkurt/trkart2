import React, { useState, useEffect } from 'react';
import authService from '@/services/authService';
import { useNavigate } from 'react-router-dom';

const Security: React.FC = () => {
  const [sessions, setSessions] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [revokingAll, setRevokingAll] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    // Check if user is authenticated
    if (!authService.isAuthenticated()) {
      navigate('/login');
      return;
    }

    loadSessions();
  }, [navigate]);

  const loadSessions = async () => {
    setLoading(true);
    setError(null);

    try {
      const activeSessions = await authService.getActiveSessions();
      setSessions(activeSessions);
    } catch (err) {
      setError('Failed to load active sessions');
      console.error('Error loading sessions:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleRevokeAllSessions = async () => {
    if (window.confirm('Are you sure you want to revoke all sessions? You will be logged out from all devices.')) {
      setRevokingAll(true);

      try {
        const success = await authService.revokeAllSessions();

        if (success) {
          // Redirect to login page
          navigate('/login');
        } else {
          setError('Failed to revoke all sessions');
        }
      } catch (err) {
        setError('Failed to revoke all sessions');
        console.error('Error revoking sessions:', err);
      } finally {
        setRevokingAll(false);
      }
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString();
  };

  return (
    <div className="container mx-auto p-4">
      <h1 className="text-2xl font-bold mb-6">Security Settings</h1>

      {error && (
        <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
          {error}
        </div>
      )}

      <div className="bg-white shadow-md rounded-lg p-6 mb-6">
        <h2 className="text-xl font-semibold mb-4">Active Sessions</h2>

        {loading ? (
          <p>Loading sessions...</p>
        ) : sessions.length === 0 ? (
          <p>No active sessions found.</p>
        ) : (
          <div>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-gray-200">
                <thead>
                  <tr>
                    <th className="px-6 py-3 bg-gray-50 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Device</th>
                    <th className="px-6 py-3 bg-gray-50 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">IP Address</th>
                    <th className="px-6 py-3 bg-gray-50 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Created At</th>
                    <th className="px-6 py-3 bg-gray-50 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">Expires At</th>
                  </tr>
                </thead>
                <tbody className="bg-white divide-y divide-gray-200">
                  {sessions.map((session, index) => (
                    <tr key={session.sessionID || index}>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {session.deviceInfo || 'Unknown Device'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {session.ipAddress || 'Unknown'}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {formatDate(session.createdAt)}
                      </td>
                      <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                        {formatDate(session.refreshTokenExpiration)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            <div className="mt-6">
              <button 
                onClick={handleRevokeAllSessions}
                disabled={revokingAll}
                className="bg-red-600 hover:bg-red-700 text-white font-bold py-2 px-4 rounded focus:outline-none focus:shadow-outline"
              >
                {revokingAll ? 'Revoking All Sessions...' : 'Revoke All Sessions'}
              </button>
              <p className="text-sm text-gray-600 mt-2">
                This will log you out from all devices, including this one.
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default Security;
