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
        // Create a user object from the session data
        const userData: User = {
          customerID: sessionData.customerID || 0,
          email: sessionData.email,
          fullName: sessionData.fullName || '',
        };
        setUser(userData);
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
      // console.log('Attempting login with:', { email, password, rememberMe });
      setIsLoading(true);
      const response = await authService.login(credentials, credentials.rememberMe);
      console.log('Login response:', response);

      // Get the email from the auth service which handles the remember me logic
      const rememberedEmail = authService.getRememberedEmail();
      const userEmail = rememberedEmail || credentials.email;
      
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
        email: '',
        fullName: '', // Will be updated after session check
      };
      
      authService.setUserData(initialUserData);
      setUser(initialUserData);
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
      console.error('Registration error:', error);
      throw error;
    } finally {
      setIsLoading(false);
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
      setIsLoading(true);
      await authService.logout();
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      setIsLoading(false);
      clearUserData(sessionEmail);
      setUser(null);
      setHasValidSession(false);
      setSessionEmail(null);
      localStorage.removeItem('user');
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