import api from './api';
import { TokenResponse } from '@/types';
import { getCookie, setCookie, deleteCookie, isTokenExpired } from '@/utils/cookieUtils';
import authService from './authService';

class TokenService {
  private isRefreshing = false;
  private refreshPromise: Promise<TokenResponse> | null = null;

  // Refresh the access token using the refresh token
  async refreshToken(): Promise<TokenResponse> {
    // If we're already refreshing, return the existing promise
    if (this.isRefreshing && this.refreshPromise) {
      return this.refreshPromise;
    }

    this.isRefreshing = true;

    try {
      // Create a new promise for the refresh
      this.refreshPromise = new Promise<TokenResponse>(async (resolve, reject) => {
        try {
          const refreshToken = getCookie('refreshToken');

          if (!refreshToken) {
            throw new Error('No refresh token available');
          }

          // Get a new token from the server
          const response = await api.post<TokenResponse>('/Token/refresh', {
            refreshToken
          });

          // Update cookies with the new tokens
          if (response.data) {
            const { accessToken, refreshToken} = response.data;
            
            // Update cookies
            setCookie('accessToken', accessToken);
            setCookie('refreshToken', refreshToken);
            
            // Update auth service with the new tokens
            const userData = authService.getUserData();
            if (userData) {
              // Update the auth state with the new tokens
              authService.setUserData(userData);
            }
          }

          resolve(response.data);
        } catch (error) {
          console.error('Failed to refresh token:', error);
          // Clear session on refresh failure
          // Clear all auth cookies
          ['accessToken', 'refreshToken', 'accessTokenExpiration', 'refreshTokenExpiration'].forEach(cookie => {
            deleteCookie(cookie);
          });
          reject(error);
        } finally {
          this.isRefreshing = false;
          this.refreshPromise = null;
        }
      });

      return await this.refreshPromise;
    } catch (error) {
      this.isRefreshing = false;
      this.refreshPromise = null;
      throw error;
    }
  }

  // Check if we need to refresh the token and do so if needed
  async ensureValidToken(): Promise<string | null> {
    // First check if refresh token exists and is not expired
    const refreshToken = getCookie('refreshToken');
    const isRefreshTokenValid = refreshToken && !isTokenExpired('refreshTokenExpiration');

    if (!isRefreshTokenValid) {
      // Clear all auth cookies if refresh token is invalid
      ['accessToken', 'refreshToken', 'accessTokenExpiration', 'refreshTokenExpiration'].forEach(cookie => {
        deleteCookie(cookie);
      });
      return null;
    }

    // If we have a valid refresh token but no access token or it's expired, refresh it
    const accessToken = getCookie('accessToken');
    if (!accessToken || isTokenExpired('accessTokenExpiration')) {
      try {
        const response = await this.refreshToken();
        return response.accessToken;
      } catch (error) {
        console.error('Token refresh failed:', error);
        return null;
      }
    }

    return accessToken;
  }
}

export default new TokenService();
