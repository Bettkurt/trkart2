import api from './api';
import { LoginRequest, RegisterRequest, AuthResponse, User } from '@/types';

class AuthService {
  async login(credentials: LoginRequest): Promise<AuthResponse> {
    const response = await api.post<AuthResponse>('/Auth/login', credentials);
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
      localStorage.removeItem('token');
      localStorage.removeItem('user');
    }
  }

  // Store user data in localStorage
  setUserData(token: string, user: User): void {
    localStorage.setItem('token', token);
    localStorage.setItem('user', JSON.stringify(user));
  }

  // Get user data from localStorage
  getUserData(): User | null {
    const userStr = localStorage.getItem('user');
    return userStr ? JSON.parse(userStr) : null;
  }

  // Get token from localStorage
  getToken(): string | null {
    return localStorage.getItem('token');
  }

  // Check if user is authenticated
  isAuthenticated(): boolean {
    return !!this.getToken();
  }

  // Clear all auth data
  clearAuthData(): void {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }
}

export default new AuthService(); 