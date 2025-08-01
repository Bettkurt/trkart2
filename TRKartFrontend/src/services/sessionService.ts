// Session storage keys
export const SESSION_KEYS = {
  USER_ID: 'userId',
  ACCESS_TOKEN: 'accessToken',
  REFRESH_TOKEN: 'refreshToken',
  USER_EMAIL: 'userEmail',
  USER_NAME: 'userName',
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

  // Set user session after successful login
  setUserSession(data: {
    accessToken: string;
    email: string;
  }): void {
    this.setSessionData(SESSION_KEYS.ACCESS_TOKEN, data.accessToken);
    this.setSessionData(SESSION_KEYS.USER_EMAIL, data.email);
  }

  // Get current user ID
  getUserId(): string | null {
    return this.getSessionData(SESSION_KEYS.USER_ID);
  }

  // Get access token
  getAccessToken(): string | null {
    return this.getSessionData(SESSION_KEYS.ACCESS_TOKEN);
  }

  // Check if user is authenticated
  isAuthenticated(): boolean {
    return !!this.getAccessToken();
  }
}

export default new SessionService();
