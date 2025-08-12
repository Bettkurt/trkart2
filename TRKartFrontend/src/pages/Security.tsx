import React, { useState, useEffect } from 'react';
import authService from '@/services/authService';
import { useNavigate, useLocation } from 'react-router-dom';
import { logger } from '@/utils/logger';

const Security: React.FC = () => {
  const [sessions, setSessions] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [revokingAll, setRevokingAll] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    logger.info('Security', 'mount', 'Security page loaded', {
      path: location.pathname,
      isAuthenticated: authService.isAuthenticated()
    });

    // Check if user is authenticated
    if (!authService.isAuthenticated()) {
      logger.warn('Security', 'auth', 'Unauthorized access attempt - redirecting to login');
      navigate('/login');
      return;
    }

    loadSessions();

    return () => {
      logger.debug('Security', 'unmount', 'Security page unmounting');
    };
  }, [navigate, location.pathname]);

  const loadSessions = async () => {
    setLoading(true);
    setError(null);
    
    logger.info('Security', 'loadSessions', 'Loading active sessions');

    try {
      const activeSessions = await authService.getActiveSessions();
      logger.info('Security', 'loadSessions', 'Successfully loaded active sessions', {
        sessionCount: activeSessions.length
      });
      setSessions(activeSessions);
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      logger.error('Security', 'loadSessions', 'Failed to load active sessions', error);
      setError('Failed to load active sessions');
    } finally {
      setLoading(false);
    }
  };

  const handleRevokeAllSessions = async () => {
    const confirmMessage = 'Are you sure you want to revoke all sessions? You will be logged out from all devices.';
    logger.info('Security', 'revokeAllSessions', 'User initiated session revocation', {
      sessionCount: sessions.length
    });
    
    if (window.confirm(confirmMessage)) {
      setRevokingAll(true);
      logger.debug('Security', 'revokeAllSessions', 'Starting session revocation');

      try {
        const success = await authService.revokeAllSessions();

        if (success) {
          logger.info('Security', 'revokeAllSessions', 'Successfully revoked all sessions');
          navigate('/login');
        } else {
          const errorMsg = 'Failed to revoke all sessions';
          logger.error('Security', 'revokeAllSessions', errorMsg);
          setError(errorMsg);
        }
      } catch (err) {
        const error = err instanceof Error ? err : new Error(String(err));
        logger.error('Security', 'revokeAllSessions', 'Error revoking sessions', error);
        setError('Failed to revoke all sessions');
      } finally {
        setRevokingAll(false);
      }
    } else {
      logger.debug('Security', 'revokeAllSessions', 'User cancelled session revocation');
    }
  };

  const formatDate = (dateString: string) => {
    try {
      return new Date(dateString).toLocaleString();
    } catch (err) {
      const error = err instanceof Error ? err : new Error(String(err));
      logger.error('Security', 'formatDate', 'Error formatting date', error, {
        context: { dateString }
      });
      return 'Invalid date';
    }
  };

  return (
    <div className="container mx-auto p-4">
      <h1 className="text-2xl font-bold mb-6">Security Settings</h1>

      {error && (
        <div 
          className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4"
          role="alert"
        >
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
                aria-busy={revokingAll}
                aria-label={revokingAll ? 'Revoking all sessions...' : 'Revoke all sessions'}
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
