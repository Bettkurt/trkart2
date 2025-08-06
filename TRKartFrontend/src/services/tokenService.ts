import api from './api';
import sessionService from './sessionService';
import { TokenResponse } from '@/types';

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
          const refreshToken = sessionService.getRefreshToken();

          if (!refreshToken) {
            throw new Error('No refresh token available');
          }

          // Get a new token from the server
          const response = await api.post<TokenResponse>('/Token/refresh', {
            refreshToken
          });

          /* Update the session with the new tokens
          if (response.data) {
            sessionService.setUserSession({
              accessToken: response.data.accessToken,
              refreshToken: response.data.refreshToken,
              accessTokenExpiration: response.data.accessTokenExpiration,
              refreshTokenExpiration: response.data.refreshTokenExpiration,
              email: sessionService.getSessionData('userEmail') || ''
            });
          } */

          resolve(response.data);
        } catch (error) {
          console.error('Failed to refresh token:', error);
          // Clear session on refresh failure
          sessionService.clearSession();
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
    // If the access token is not expired, return it
    if (!sessionService.isAccessTokenExpired()) {
      return sessionService.getAccessToken();
    }

    // If the refresh token is expired, clear the session and return null
    if (sessionService.isRefreshTokenExpired()) {
      sessionService.clearSession();
      return null;
    }

    // Refresh the token
    try {
      const response = await this.refreshToken();
      return response.accessToken;
    } catch (error) {
      console.error('Token refresh failed:', error);
      return null;
    }
  }
}

export default new TokenService();
