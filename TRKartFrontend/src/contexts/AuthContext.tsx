import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { User } from '@/types';
import authService from '@/services/authService';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  hasValidSession: boolean;
  sessionEmail: string | null;
  login: (email: string, password: string, rememberMe: boolean) => Promise<void>;
  loginPasswordOnly: (password: string) => Promise<void>;
  register: (email: string, password: string, fullName: string) => Promise<void>;
  logout: () => Promise<void>;
  setUser: (user: User | null) => void;
  checkSession: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [hasValidSession, setHasValidSession] = useState(false);
  const [sessionEmail, setSessionEmail] = useState<string | null>(null);

  const checkSession = async () => {
    try {
      const sessionData = await authService.checkSession();
      setHasValidSession(sessionData.hasValidSession);
      setSessionEmail(sessionData.email);
      
      if (sessionData.hasValidSession && sessionData.email) {
        // Create a user object from the session email
        const userData: User = {
          customerID: 0, // This should come from the backend
          email: sessionData.email,
          fullName: '', // This should come from the backend
        };
        setUser(userData);
      } else {
        // Clear user data if no valid session
        setUser(null);
        authService.clearAuthData();
      }
    } catch (error) {
      console.error('Session check error:', error);
      setHasValidSession(false);
      setSessionEmail(null);
      setUser(null);
      authService.clearAuthData();
    }
  };

  useEffect(() => {
    // Check for existing session on app load
    const initializeAuth = async () => {
      await checkSession();
      
      // Also check for stored user data as fallback
      const storedUser = authService.getUserData();
      if (storedUser && !user) {
        setUser(storedUser);
      }
      
      setIsLoading(false);
    };

    initializeAuth();
  }, []);

  const login = async (email: string, password: string, rememberMe: boolean) => {
    try {
      await authService.login({ email, password, rememberMe });
      
      // Create a user object from the email
      const userData: User = {
        customerID: 0, // This should come from the backend
        email,
        fullName: '', // This should come from the backend
      };
      
      authService.setUserData(userData);
      setUser(userData);
      setHasValidSession(true);
      setSessionEmail(email);
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  };

  const loginPasswordOnly = async (password: string) => {
    try {
      // The backend will get the session token from cookies
      await authService.loginPasswordOnly({ password });
      
      // Update session state
      setHasValidSession(true);
      if (sessionEmail) {
        const userData: User = {
          customerID: 0, // This should come from the backend
          email: sessionEmail,
          fullName: '', // This should come from the backend
        };
        setUser(userData);
        authService.setUserData(userData);
      }
    } catch (error) {
      console.error('Password-only login error:', error);
      throw error;
    }
  };

  const register = async (email: string, password: string, fullName: string) => {
    try {
      await authService.register({ email, password, fullName });
      
      // After successful registration, log the user in
      await login(email, password, false);
    } catch (error) {
      console.error('Registration error:', error);
      throw error;
    }
  };

  const logout = async () => {
    try {
      await authService.logout();
      setUser(null);
      setHasValidSession(false);
      setSessionEmail(null);
    } catch (error) {
      console.error('Logout error:', error);
      // Even if logout fails, clear local state
      setUser(null);
      setHasValidSession(false);
      setSessionEmail(null);
    }
  };

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    isLoading,
    hasValidSession,
    sessionEmail,
    login,
    loginPasswordOnly,
    register,
    logout,
    setUser,
    checkSession,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}; 