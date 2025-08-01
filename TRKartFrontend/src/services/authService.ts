import api from './api';
import { LoginRequest, PasswordOnlyLoginRequest, SessionCheckResponse, RegisterRequest, AuthResponse, User } from '@/types';

class AuthService {
  async login(credentials: LoginRequest): Promise<AuthResponse> {
    console.log('Making login request with credentials:', credentials);
    const response = await api.post<AuthResponse>('/Auth/login', credentials);
    console.log('Login response:', response);
    console.log('Response headers:', response.headers);
    console.log('Response cookies:', document.cookie);
    return response.data;
  }

  async loginPasswordOnly(credentials: PasswordOnlyLoginRequest): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/Auth/login-password-only', credentials);
    return response.data;
  }

  async checkSession(): Promise<SessionCheckResponse> {
    const response = await api.get<SessionCheckResponse>('/Auth/check-session');
    return response.data;
  }

  async register(userData: RegisterRequest): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/Auth/register', userData);
    return response.data;
  }

  async logout(): Promise<void> {
    try {
      await api.post('/Auth/logout');
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      // Clear local storage regardless of API call success
      localStorage.removeItem('user');
    }
  }

  // Store user data in localStorage (token is now in cookies)
  // We don't need to manually get it since it's HttpOnly
  setUserData(user: User): void {
    localStorage.setItem('user', JSON.stringify(user));
  }

  // Get user data from localStorage
  getUserData(): User | null {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  }

  // Get token from cookies (this is handled by the browser automatically)
  // We don't need to manually get it since it's HttpOnly
  getToken(): string | null {
    // Token is in HttpOnly cookies, so we can't access it from JavaScript
    // The backend will handle token validation
    return null;
  }

  // Check if user is authenticated by checking if we have user data
  isAuthenticated(): boolean {
    return !!this.getUserData();
  }

  // Clear all auth data
  clearAuthData(): void {
    localStorage.removeItem('user');
  }
}

export default new AuthService(); 