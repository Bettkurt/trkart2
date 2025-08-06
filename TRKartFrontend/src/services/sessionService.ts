import axios from 'axios';

// Session storage keys
export const SESSION_KEYS = {
  USER_ID: 'userId',
  ACCESS_TOKEN: 'accessToken',
  REFRESH_TOKEN: 'refreshToken',
  USER_EMAIL: 'userEmail',
  USER_NAME: 'userName',
  TOKEN_EXPIRY: 'tokenExpiry',
  REFRESH_TOKEN_EXPIRY: 'refreshTokenExpiry'
} as const;

class SessionService {
  // Set session data
  setSessionData(key: string, value: string): void {
    if (typeof window !== 'undefined') {
      sessionStorage.setItem(key, value);
    }
  }

  // Get session data
  getSessionData(key: string): string | null {
    if (typeof window !== 'undefined') {
      return sessionStorage.getItem(key);
    }
    return null;
  }

  // Remove session data
  removeSessionData(key: string): void {
    if (typeof window !== 'undefined') {
      sessionStorage.removeItem(key);
    }
  }

  // Clear all session data
  clearSession(): void {
    if (typeof window !== 'undefined') {
      sessionStorage.clear();
    }
  }

  /* Set user session after successful login
  setUserSession(data: {
    accessToken: string;
    refreshToken: string;
    accessTokenExpiration: string;
    refreshTokenExpiration: string;
    email: string;
  }): void {
    this.setSessionData(SESSION_KEYS.ACCESS_TOKEN, data.accessToken);
    this.setSessionData(SESSION_KEYS.REFRESH_TOKEN, data.refreshToken);
    this.setSessionData(SESSION_KEYS.TOKEN_EXPIRY, data.accessTokenExpiration);
    this.setSessionData(SESSION_KEYS.REFRESH_TOKEN_EXPIRY, data.refreshTokenExpiration);
    this.setSessionData(SESSION_KEYS.USER_EMAIL, data.email);
  }
  */

  // Get current user ID
  getUserId(): string | null {
    return this.getSessionData(SESSION_KEYS.USER_ID);
  }

  // Get access token
  getAccessToken(): string | null {
    return this.getSessionData(SESSION_KEYS.ACCESS_TOKEN);
  }

  // Get refresh token
  getRefreshToken(): string | null {
    return this.getSessionData(SESSION_KEYS.REFRESH_TOKEN);
  }

  // Get access token expiration
  getAccessTokenExpiration(): string | null {
    return this.getSessionData(SESSION_KEYS.TOKEN_EXPIRY);
  }

  // Get refresh token expiration
  getRefreshTokenExpiration(): string | null {
    return this.getSessionData(SESSION_KEYS.REFRESH_TOKEN_EXPIRY);
  }

  // Check if access token is expired
  isAccessTokenExpired(): boolean {
    const expiryStr = this.getAccessTokenExpiration();
    if (!expiryStr) return true;

    try {
      const expiry = new Date(expiryStr);
      const now = new Date();

      // Return true if token is expired or will expire in the next 30 seconds
      return expiry <= new Date(now.getTime() + 30 * 1000);
    } catch (error) {
      console.error('Error parsing token expiration:', error);
      return true; // If there's an error parsing the date, consider it expired
    }
  }

  // Check if token refresh is needed (token is expired or about to expire)
  isTokenRefreshNeeded(bufferSeconds: number = 300): boolean {
    const expiryStr = this.getAccessTokenExpiration();
    if (!expiryStr) return true;

    try {
      const expiry = new Date(expiryStr);
      const now = new Date();
      
      // Return true if token is expired or will expire within the buffer period
      return expiry <= new Date(now.getTime() + bufferSeconds * 1000);
    } catch (error) {
      console.error('Error checking token refresh need:', error);
      return true; // If there's an error, assume refresh is needed
    }
  }

  // Check if refresh token is expired
  isRefreshTokenExpired(): boolean {
    const expiryStr = this.getRefreshTokenExpiration();
    if (!expiryStr) return true;

    try {
      const expiry = new Date(expiryStr);
      const now = new Date();
      
      // Add a small buffer (5 seconds) to account for clock skew
      return expiry <= new Date(now.getTime() + 5000);
    } catch (error) {
      console.error('Error parsing refresh token expiration:', error);
      return true; // If there's an error parsing the date, consider it expired
    }
  }

  // Check if user is authenticated
  isAuthenticated(): boolean {
    return !!this.getAccessToken() && !this.isRefreshTokenExpired();
  }

  // Check and refresh token on app load if needed
  async checkAndRefreshToken(): Promise<boolean> {
    console.log('[Token] Starting token refresh check...');
    
    // If no refresh token, user is not authenticated
    const refreshToken = this.getRefreshToken();
    console.log('[Token] Refresh token exists:', !!refreshToken);
    
    if (!refreshToken || this.isRefreshTokenExpired()) {
      console.log('[Token] No refresh token or expired:', { 
        hasToken: !!refreshToken, 
        isExpired: this.isRefreshTokenExpired() 
      });
      this.clearSession();
      return false;
    }

    // If access token is still valid, no need to refresh
    const isAccessTokenExpired = this.isAccessTokenExpired();
    console.log('[Token] Access token expired:', isAccessTokenExpired);
    
    if (!isAccessTokenExpired) {
      console.log('[Token] Access token still valid, no refresh needed');
      return true;
    }

    try {
      console.log('[Token] Attempting to refresh token...');
      const refreshUrl = `${process.env.VITE_API_BASE_URL || 'http://localhost:7037/api'}/auth/refresh-token`;
      console.log('[Token] Refresh URL:', refreshUrl);
      
      // Log the refresh token being sent (first few chars for security)
      console.log('[Token] Sending refresh token (truncated):', 
        refreshToken ? `${refreshToken.substring(0, 10)}...` : 'none');
      
      const response = await axios.post<{
        accessToken: string;
        refreshToken: string;
        accessTokenExpiration: string;
        refreshTokenExpiration: string;
      }>(
        refreshUrl,
        { refreshToken },
        { 
          withCredentials: true,
          headers: {
            'Content-Type': 'application/json'
          }
        }
      );

      console.log('[Token] Refresh response status:', response.status);
      
      if (response.data) {
        console.log('[Token] Token refresh successful');
        console.log('[Token] New access token (truncated):', 
          response.data.accessToken ? `${response.data.accessToken.substring(0, 10)}...` : 'none');
        /*
        this.setUserSession({
          accessToken: response.data.accessToken,
          refreshToken: response.data.refreshToken,
          accessTokenExpiration: response.data.accessTokenExpiration,
          refreshTokenExpiration: response.data.refreshTokenExpiration,
          email: this.getSessionData('userEmail') || ''
        }); */
        
        // Verify the token was actually set
        const newAccessToken = this.getAccessToken();
        console.log('[Token] New access token stored:', !!newAccessToken);
        
        return true;
      }
    } catch (error: any) {
      console.error('[Token] Failed to refresh token:', {
        message: error.message,
        response: error.response ? {
          status: error.response.status,
          data: error.response.data,
          headers: error.response.headers
        } : 'No response',
        config: {
          url: error.config?.url,
          method: error.config?.method,
          headers: error.config?.headers
        }
      });
      this.clearSession();
    }
    
    return false;
  }
}

export default new SessionService();
