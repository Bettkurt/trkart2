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
        // Create a user object from the session data
        const userData: User = {
          customerID: sessionData.customerID || 0,
          email: sessionData.email,
          fullName: sessionData.fullName || '',
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
      console.log('Attempting login with:', { email, password, rememberMe });
      const response = await authService.login({ email, password, rememberMe });
      console.log('Login response:', response);
      
      // Test session immediately after login to get customerID and fullName
      setTimeout(async () => {
        try {
          const sessionCheck = await authService.checkSession();
          console.log('Session check after login:', sessionCheck);
          
          if (sessionCheck.hasValidSession && sessionCheck.email) {
            // Create a user object from the session data with customerID and fullName
            const userData: User = {
              customerID: sessionCheck.customerID || 0,
              email: sessionCheck.email,
              fullName: sessionCheck.fullName || '',
            };
            
            authService.setUserData(userData);
            setUser(userData);
            setHasValidSession(true);
            setSessionEmail(sessionCheck.email);
            
            console.log('User data updated after login:', userData);
          }
        } catch (error) {
          console.error('Session check error after login:', error);
        }
      }, 1000);
      
      // Set initial user data (will be updated after session check)
      const initialUserData: User = {
        customerID: 0, // Will be updated after session check
        email,
        fullName: '', // Will be updated after session check
      };
      
      authService.setUserData(initialUserData);
      setUser(initialUserData);
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
      
      // Update session state and get user data
      setTimeout(async () => {
        try {
          const sessionCheck = await authService.checkSession();
          console.log('Session check after password-only login:', sessionCheck);
          
          if (sessionCheck.hasValidSession && sessionCheck.email) {
            // Create a user object from the session data with customerID and fullName
            const userData: User = {
              customerID: sessionCheck.customerID || 0,
              email: sessionCheck.email,
              fullName: sessionCheck.fullName || '',
            };
            
            setUser(userData);
            authService.setUserData(userData);
            setHasValidSession(true);
            setSessionEmail(sessionCheck.email);
            
            console.log('User data updated after password-only login:', userData);
          }
        } catch (error) {
          console.error('Session check error after password-only login:', error);
        }
      }, 1000);
      
      // Set initial session state
      setHasValidSession(true);
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

  // Helper function to clear user-specific data from localStorage
  const clearUserData = (userEmail: string | null) => {
    try {
      if (userEmail) {
        // Clear user-specific transactions and cards
        localStorage.removeItem(`trkart_transactions_${userEmail}`);
        localStorage.removeItem(`trkart_cards_${userEmail}`);
      }
      // Also clear anonymous data
      localStorage.removeItem('trkart_transactions_anonymous');
      localStorage.removeItem('trkart_cards_anonymous');
    } catch (error) {
      console.error('Failed to clear user data:', error);
    }
  };

  const logout = async () => {
    try {
      await authService.logout();
      // Clear user-specific data before clearing user state
      clearUserData(sessionEmail);
      setUser(null);
      setHasValidSession(false);
      setSessionEmail(null);
    } catch (error) {
      console.error('Logout error:', error);
      // Even if logout fails, clear local state and user data
      clearUserData(sessionEmail);
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