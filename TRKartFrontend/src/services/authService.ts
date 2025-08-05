import api from './api';
import { LoginRequest, RegisterRequest, SessionCheckResponse, AuthResponse, TokenResponse, RefreshTokenRequest, User } from '@/types';
import sessionService from './sessionService';

const REMEMBER_ME_KEY = 'rememberMe';
const REMEMBERED_EMAIL_KEY = 'rememberedEmail';

class AuthService {
  // Store user data in localStorage
  setUserData(user: User): void {
    localStorage.setItem('user', JSON.stringify(user));
  }

  // Get user data from localStorage
  getUserData(): User | null {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  }

  // Check if user is authenticated by checking session service
  isAuthenticated(): boolean {
    return sessionService.isAuthenticated();
  }

  // Clear all auth data
  clearAuthData(): void {
    localStorage.removeItem('user');
    sessionService.clearSession();
  }

  // Remember Me functionality
  setRememberMe(value: boolean): void {
    localStorage.setItem(REMEMBER_ME_KEY, value ? '1' : '0');
  }

  getRememberMe(): boolean {
    return localStorage.getItem(REMEMBER_ME_KEY) === '1';
  }

  // Email storage for Remember Me
  setRememberedEmail(email: string): void {
    localStorage.setItem(REMEMBERED_EMAIL_KEY, email);
  }

  getRememberedEmail(): string | null {
    return localStorage.getItem(REMEMBERED_EMAIL_KEY);
  }

  clearRememberedEmail(): void {
    localStorage.removeItem(REMEMBERED_EMAIL_KEY);
  }

  // Auth methods
  async login(credentials: LoginRequest, rememberMe: boolean): Promise<AuthResponse> {
    if (!credentials.email) {
      throw new Error('Email is required');
    }

    this.setRememberMe(rememberMe);

    console.log('Making login request with credentials:', {
      email: credentials.email,
      rememberMe: rememberMe
    });

    const response = await api.post<AuthResponse>('/Auth/login', {
      email: credentials.email,
      password: credentials.password,
      rememberMe: rememberMe
    }, {
      withCredentials: true // Ensure credentials are sent
    });

    console.log('Login response received');

    // Store user session data
    if (response.data) {
      const { accessToken, refreshToken, accessTokenExpiration, refreshTokenExpiration } = response.data;
      
      // Validate required token fields
      if (!accessToken || !refreshToken || !accessTokenExpiration || !refreshTokenExpiration) {
        console.error('Invalid token data in login response:', response.data);
        throw new Error('Invalid token data received from server');
      }
      
      // Update session storage with token data
      sessionService.setUserSession({
        accessToken,
        refreshToken,
        accessTokenExpiration,
        refreshTokenExpiration,
        email: credentials.email
      });

      // Set up user data object with default values
      const userData: User = {
        email: credentials.email,
        customerID: 0, // Will be populated from session check
        fullName: '' // Will be populated from session check
      };

      // Store user data
      this.setUserData(userData);

      // Try to get additional user details
      try {
        const sessionCheck = await this.checkSession();
        if (sessionCheck.hasValidSession && sessionCheck.customerID) {
          userData.customerID = sessionCheck.customerID;
          //userData.fullName = sessionCheck.fullName;
          this.setUserData(userData);
        }
      } catch (error) {
        console.warn('Failed to fetch additional user details:', error);
      }
    }

    // Update stored email based on preference
    if (rememberMe) {
      this.setRememberedEmail(credentials.email);
    } else {
      this.clearRememberedEmail();
    }

    return response.data;
  }

  async register(credentials: RegisterRequest, rememberMe: boolean): Promise<AuthResponse> {
    this.setRememberMe(rememberMe);

    const response = await api.post<AuthResponse>('/Auth/register', {
      email: credentials.email,
      password: credentials.password,
      fullName: credentials.fullName,
      rememberMe: rememberMe
    });

    // Update stored email based on preference
    if (rememberMe) {
      this.setRememberedEmail(credentials.email);
    } else {
      this.clearRememberedEmail();
    }

    return response.data;
  }

  async refreshToken(refreshToken: string): Promise<TokenResponse> {
    const request: RefreshTokenRequest = { refreshToken };

    const response = await api.post<TokenResponse>('/Auth/refresh-token', request, {
      withCredentials: true
    });

    // Update session with new tokens
    if (response.data) {
      const userEmail = sessionService.getSessionData('userEmail') || '';

      sessionService.setUserSession({
        accessToken: response.data.accessToken,
        refreshToken: response.data.refreshToken,
        accessTokenExpiration: response.data.accessTokenExpiration,
        refreshTokenExpiration: response.data.refreshTokenExpiration,
        email: userEmail
      });
    }

    return response.data;
  }

  async logout(): Promise<boolean> {
    try {
      // Get refresh token before clearing session
      const refreshToken = sessionService.getRefreshToken();

      try {
        // Call the server to revoke the refresh token
        if (refreshToken) {
          await api.post('/Auth/logout', {}, {
            withCredentials: true
          });
        }
      } catch (error) {
        console.error('[AuthService] Error during logout API call:', error);
        // Continue with client-side cleanup even if API call fails
      }

      // Clear all auth data
      this.clearAuthData();

      // Clear the axios authorization header
      delete api.defaults.headers.common['Authorization'];

      // Redirect to home page
      window.location.href = '/';

      return true;
    } catch (error) {
      console.error('[AuthService] Logout error:', error);
      // Even if there's an error, we'll still clear data and redirect to home
      this.clearAuthData();
      window.location.href = '/';
      return false;
    }
  }

  async checkSession(): Promise<SessionCheckResponse> {
    console.log('[AuthService] Checking session with server...');
    
    // Get the current access token
    const accessToken = sessionService.getAccessToken();
    
    try {
      const response = await api.get<SessionCheckResponse>('/Auth/check-session', {
        withCredentials: true,
        headers: accessToken ? {
          'Authorization': `Bearer ${accessToken}`
        } : {}
      });
      
      console.log('[AuthService] Session check response:', {
        status: response.status,
        hasValidSession: response.data?.hasValidSession,
        email: response.data?.email
      });
      
      return response.data;

    } catch (error: any) {
      console.error('[AuthService] Session check error:', {
        message: error.message,
        status: error.response?.status,
        data: error.response?.data,
        config: {
          url: error.config?.url,
          method: error.config?.method,
          headers: error.config?.headers
        }
      });
      throw error;
    }
  }

  // Update user data if session is valid
  updateUserData(sessionData: SessionCheckResponse): User | null {
    if (!sessionData.hasValidSession || !sessionData.customerID || !sessionData.email) {
      return null;
    }
    
    const userData: User = {
      email: sessionData.email,
      customerID: sessionData.customerID,
      fullName: sessionData.fullName || ''
    };
    
    this.setUserData(userData);
    return userData;
  }

  // Check if we can access protected resources
  async validateToken(): Promise<boolean> {
    try {
      await api.get('/Token/validate', {
        withCredentials: true
      });
      return true;
    } catch (error) {
      return false;
    }
  }

  // Revoke all sessions for the current user
  async revokeAllSessions(): Promise<boolean> {
    try {
      await api.post('/Security/revoke-all-sessions', {}, {
        withCredentials: true
      });

      // Clear all auth data
      this.clearAuthData();

      return true;
    } catch (error) {
      console.error('Failed to revoke all sessions:', error);
      return false;
    }
  }

  // Get all active sessions for the current user
  async getActiveSessions(): Promise<any[]> {
    try {
      const response = await api.get('/Security/active-sessions', {
        withCredentials: true
      });

      return response.data || [];
    } catch (error) {
      console.error('Failed to get active sessions:', error);
      return [];
    }
  }
}

export default new AuthService();