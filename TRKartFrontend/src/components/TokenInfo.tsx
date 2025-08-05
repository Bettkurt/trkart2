import React, { useState, useEffect } from 'react';
import sessionService from '@/services/sessionService';

interface TokenInfoProps {
  showDetails?: boolean;
}

const TokenInfo: React.FC<TokenInfoProps> = ({ showDetails = false }) => {
  const [tokenData, setTokenData] = useState({
    hasAccessToken: false,
    hasRefreshToken: false,
    accessTokenExpired: true,
    refreshTokenExpired: true,
    accessTokenExpiry: '',
    refreshTokenExpiry: '',
  });

  useEffect(() => {
    // Update token info every second
    const interval = setInterval(() => {
      const accessToken = sessionService.getAccessToken();
      const refreshToken = sessionService.getRefreshToken();
      const accessTokenExpiry = sessionService.getAccessTokenExpiration();
      const refreshTokenExpiry = sessionService.getRefreshTokenExpiration();

      setTokenData({
        hasAccessToken: !!accessToken,
        hasRefreshToken: !!refreshToken,
        accessTokenExpired: sessionService.isAccessTokenExpired(),
        refreshTokenExpired: sessionService.isRefreshTokenExpired(),
        accessTokenExpiry: accessTokenExpiry || '',
        refreshTokenExpiry: refreshTokenExpiry || '',
      });
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  if (!showDetails) {
    return (
      <div className="fixed bottom-4 right-4 bg-gray-800 text-white p-2 rounded-md shadow-lg text-xs">
        <div className="flex space-x-2">
          <div>
            Access: 
            <span className={tokenData.hasAccessToken ? (tokenData.accessTokenExpired ? 'text-yellow-400' : 'text-green-400') : 'text-red-400'}>
              {tokenData.hasAccessToken ? (tokenData.accessTokenExpired ? '⚠️' : '✅') : '❌'}
            </span>
          </div>
          <div>
            Refresh: 
            <span className={tokenData.hasRefreshToken ? (tokenData.refreshTokenExpired ? 'text-yellow-400' : 'text-green-400') : 'text-red-400'}>
              {tokenData.hasRefreshToken ? (tokenData.refreshTokenExpired ? '⚠️' : '✅') : '❌'}
            </span>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="fixed bottom-4 right-4 bg-gray-800 text-white p-4 rounded-md shadow-lg text-xs max-w-md">
      <h3 className="font-bold mb-2">Token Status</h3>
      <div className="space-y-1">
        <div>
          Access Token: 
          <span className={tokenData.hasAccessToken ? 'text-green-400' : 'text-red-400'}>
            {tokenData.hasAccessToken ? 'Present' : 'Missing'}
          </span>
          {tokenData.hasAccessToken && (
            <span className={tokenData.accessTokenExpired ? 'text-red-400' : 'text-green-400'}>
              {tokenData.accessTokenExpired ? ' (Expired)' : ' (Valid)'}
            </span>
          )}
        </div>
        {tokenData.hasAccessToken && tokenData.accessTokenExpiry && (
          <div className="text-gray-400">
            Expires: {new Date(tokenData.accessTokenExpiry).toLocaleTimeString()}
          </div>
        )}
        <div>
          Refresh Token: 
          <span className={tokenData.hasRefreshToken ? 'text-green-400' : 'text-red-400'}>
            {tokenData.hasRefreshToken ? 'Present' : 'Missing'}
          </span>
          {tokenData.hasRefreshToken && (
            <span className={tokenData.refreshTokenExpired ? 'text-red-400' : 'text-green-400'}>
              {tokenData.refreshTokenExpired ? ' (Expired)' : ' (Valid)'}
            </span>
          )}
        </div>
        {tokenData.hasRefreshToken && tokenData.refreshTokenExpiry && (
          <div className="text-gray-400">
            Expires: {new Date(tokenData.refreshTokenExpiry).toLocaleTimeString()}
          </div>
        )}
      </div>
    </div>
  );
};

export default TokenInfo;
