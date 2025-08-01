import api from './api';
import { LoginRequest, RegisterRequest, SessionCheckResponse, AuthResponse } from '@/types';
import sessionService from './sessionService';

const REMEMBER_ME_KEY = 'rememberMe';
const REMEMBERED_EMAIL_KEY = 'rememberedEmail';

class AuthService {
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
    this.setRememberMe(rememberMe);

    console.log('Making login request with credentials:', credentials);

    const response = await api.post<AuthResponse>('/Auth/login', {
      email: credentials.email,
      password: credentials.password,
      rememberMe: rememberMe
    }, {
      withCredentials: true // Ensure credentials are sent
    });

    console.log('Login response:', response);
    console.log('Response headers:', response.headers);
    console.log('Response cookies:', document.cookie);

    // Store user session data
    if (response.data && response.data.token) {
      sessionService.setUserSession({
        accessToken: response.data.token,
        email: credentials.email
      });
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

  async logout(): Promise<boolean> {
    try {
      
      try {
        // Call the server to invalidate the session
        await api.post('/Auth/logout', {}, {
          withCredentials: true,
          headers: {
            'Authorization': `Bearer ${api.defaults.headers.common['Authorization']}`
          }
        });
        
      } catch (error) {
        console.error('[AuthService] Error during logout API call:', error);
        // Continue with client-side cleanup even if API call fails
      }
      
      // Clear the axios authorization header
      delete api.defaults.headers.common['Authorization'];
      
      // Redirect to home page
      window.location.href = '/';
      
      return true;
    } catch (error) {
      console.error('[AuthService] Logout error:', error);
      // Even if there's an error, we'll still redirect to home
      window.location.href = '/';
      return false;
    }
  }

  async checkSession(): Promise<SessionCheckResponse> {
    const response = await api.get<SessionCheckResponse>('/Auth/check-session');
    return response.data;
  }
}

export default new AuthService();