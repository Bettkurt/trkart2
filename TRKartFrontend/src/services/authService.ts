import api from './api';
import { LoginRequest, RegisterRequest, SessionCheckResponse, AuthResponse, TokenResponse, RefreshTokenRequest, User } from '@/types';

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

  // Check if user is authenticated by checking if we have user data in localStorage
  isAuthenticated(): boolean {
    return !!this.getUserData();
  }

  // Clear all localStorage data
  clearLocalStorage(): void {
    localStorage.clear();
    console.log('Local storage cleared');
  }

  // Clear auth data while preserving Remember Me and email if needed
  clearAuthData(): void {
    // Save Remember Me and email before clearing
    const rememberMe = this.getRememberMe();
    const rememberedEmail = this.getRememberedEmail();
    const userEmail = this.getUserData()?.email;
    
    // Clear all auth-related items
    localStorage.removeItem('user');
    
    // Clear transaction and card data for the user
    if (userEmail) {
      localStorage.removeItem(`trkart_transactions_${userEmail}`);
      localStorage.removeItem(`trkart_cards_${userEmail}`);
    }
    
    // Clear anonymous data as well
    localStorage.removeItem('trkart_transactions_anonymous');
    localStorage.removeItem('trkart_cards_anonymous');
    
    // Restore Remember Me and email if needed
    if (rememberMe && rememberedEmail) {
      this.setRememberMe(true);
      this.setRememberedEmail(rememberedEmail);
    } else {
      // Clear Remember Me settings if not needed
      localStorage.removeItem(REMEMBER_ME_KEY);
      if (!rememberMe) {
        this.clearRememberedEmail();
      }
    }
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

    // Clear localStorage before login
    this.clearLocalStorage();

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

    console.log('Login response received', response.data);

    // Store user session data
    if (response.data) {
      const { accessToken, refreshToken, accessTokenExpiration, refreshTokenExpiration } = response.data;
      
      // Validate required token fields
      if (!accessToken || !refreshToken || !accessTokenExpiration || !refreshTokenExpiration) {
        console.error('Invalid token data in login response:', response.data);
        throw new Error('Invalid token data received from server');
      }
      
      /* Update session storage with token data
      sessionService.setUserSession({
        accessToken,
        refreshToken,
        accessTokenExpiration,
        refreshTokenExpiration,
        email: credentials.email
      }); */

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

    /* Update session with new tokens
    if (response.data) {
      const userEmail = sessionService.getSessionData('userEmail') || '';

      sessionService.setUserSession({
        accessToken: response.data.accessToken,
        refreshToken: response.data.refreshToken,
        accessTokenExpiration: response.data.accessTokenExpiration,
        refreshTokenExpiration: response.data.refreshTokenExpiration,
        email: userEmail
      });
    } */

    return response.data;
  }

  async logout(): Promise<boolean> {
    try {
      // Clear client-side auth data first
      this.clearAuthData();
      
      // Clear the axios authorization header
      delete api.defaults.headers.common['Authorization'];
      
      // Call the server to clear the HTTP-only cookies
      try {
        await api.post('/Auth/logout', {}, { 
          withCredentials: true
        });
      } catch (error) {
        console.error('[AuthService] Error during logout API call:', error);
        // Continue with client-side cleanup even if API call fails
      }
      
      return true;
    } catch (error) {
      console.error('[AuthService] Logout error:', error);
      // Ensure we still clear data even if something goes wrong
      this.clearAuthData();
      return false;
    }
  }

  async checkSession(): Promise<SessionCheckResponse> {
    console.log('[AuthService] Checking session with server...');
    
    try {
      const response = await api.get<SessionCheckResponse>('/Auth/check-session', {
        withCredentials: true
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
        data: error.response?.data
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

  async changePassword(email: string, currentPassword: string, newPassword: string): Promise<void> {
    await api.post('/Auth/change-password', {
      email,
      currentPassword,
      newPassword
    });
  }

  async changeEmail(password: string, newEmail: string): Promise<void> {
    await api.post('/Auth/change-email', {
      password,
      newEmail
    }, {
      withCredentials: true
    });
  }
}

export default new AuthService();