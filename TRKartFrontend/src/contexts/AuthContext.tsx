import React, { createContext, useContext, useState, useEffect, ReactNode, useCallback } from 'react';
import { User } from '@/types';
import authService from '@/services/authService';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  hasValidSession: boolean;
  sessionEmail: string | null;
  login: (credentials: { email: string; password: string; rememberMe: boolean }) => Promise<void>;
  register: (email: string, password: string, fullName: string, rememberMe: boolean) => Promise<void>;
  logout: () => Promise<void>;
  checkSession: () => Promise<void>;
  getRememberedEmail: () => string | null;
  setUser: (user: User | null) => void;
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
      
      if (sessionData.hasValidSession && sessionData.email) {
        setSessionEmail(sessionData.email);
        setUser({
          customerID: 0, // Should come from backend
          email: sessionData.email,
          fullName: '' // Should come from backend
        });
      } else {
        setSessionEmail(null);
        setUser(null);
      }
    } catch (error) {
      console.error('Session check error:', error);
      setHasValidSession(false);
      setSessionEmail(null);
      setUser(null);
    }
  };

  const getRememberedEmail = useCallback((): string | null => {
    return authService.getRememberedEmail();
  }, []);

  useEffect(() => {
    // Check for existing session on app load
    const initializeAuth = async () => {
      await checkSession(); 
           
      /* Also check for stored user data as fallback
      const storedUser = authService.getUserData();
      if (storedUser && !user) {
        setUser(storedUser);
      } */
      
      setIsLoading(false);
    };

    initializeAuth();
  }, []);

  const login = async (credentials: { email: string; password: string; rememberMe: boolean }) => {
    try {
      setIsLoading(true);
      await authService.login(credentials, credentials.rememberMe);
      
      // Get the email from the auth service which handles the remember me logic
      const rememberedEmail = authService.getRememberedEmail();
      const userEmail = rememberedEmail || credentials.email;
      
      const userData: User = {
        customerID: 0, // Should come from backend
        email: userEmail,
        fullName: '' // Should come from backend
      };
      
      setUser(userData);
      setHasValidSession(true);
      setSessionEmail(userEmail);
      
      // No need to store user data in localStorage, authService handles the email storage
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (email: string, password: string, fullName: string, rememberMe: boolean) => {
    try {
      setIsLoading(true);
      // Pass rememberMe to authService.register
      await authService.register({ email, password, fullName }, rememberMe);
      
      // After successful registration, log the user in with rememberMe preference
      await login({ 
        email, 
        password, 
        rememberMe 
      });
    } catch (error) {
      console.error('Registration error:', error);
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  const logout = async () => {
    try {
      setIsLoading(true);
      await authService.logout();
      setUser(null);
      setHasValidSession(false);
      setSessionEmail(null);
      localStorage.removeItem('user');
    } catch (error) {
      console.error('Logout error:', error);
      // Even if logout fails, clear local state
      setUser(null);
      setHasValidSession(false);
      setSessionEmail(null);
      localStorage.removeItem('user');
    } finally {
      setIsLoading(false);
    }
  };

  const value: AuthContextType = {
    user,
    isAuthenticated: !!user,
    isLoading,
    hasValidSession,
    sessionEmail,
    login,
    register,
    logout,
    setUser,
    checkSession,
    getRememberedEmail,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};