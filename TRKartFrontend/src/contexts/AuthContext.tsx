import React, { createContext, useContext, useState, useEffect, ReactNode, useCallback } from 'react';
import { User } from '@/types';
import authService from '@/services/authService';
import tokenService from '@/services/tokenService';

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
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
  changeEmail: (password: string, newEmail: string) => Promise<void>;
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
      console.log('[Auth] Checking session with backend...');
      const sessionData = await authService.checkSession();
      console.log('[Auth] Session check result:', sessionData.hasValidSession, sessionData.email
        ,sessionData.customerID, sessionData.fullName);
      
      setHasValidSession(sessionData.hasValidSession);
      
      if (sessionData.hasValidSession && sessionData.email) {
        // Create a user object from the session data
        const userData: User = {
          customerID: sessionData.customerID || 0,
          email: sessionData.email,
          fullName: sessionData.fullName || '',
        };
        console.log('[Auth] Setting user data:', userData);
        setUser(userData);
        setSessionEmail(sessionData.email);
        
        // Store user data in localStorage for quick access
        authService.setUserData(userData);
      } else {
        console.log('[Auth] No valid session or email found, checking for refresh token???');
        // If no valid session but we have a refresh token, try to refresh
      
        const refreshToken = document.cookie.split('; ').find(row => row.startsWith('refreshToken='))?.split('=')[1];
       
        if (refreshToken) {
          console.log('[Auth] Found refresh token, attempting to refresh...');
          try {
          
           const test = await tokenService.refreshToken();
            // If refresh was successful, check session again
        
            const sessionData1 = await authService.checkSession();
           
            setHasValidSession(sessionData1.hasValidSession);
          
          
            if (sessionData1.hasValidSession && sessionData1.email) {
              // Create a user object from the session data
              const userData: User = {
                customerID: sessionData1.customerID || 0,
                email: sessionData1.email,
                fullName: sessionData1.fullName || '',
              };
              

              console.log('[Auth] Setting user data:', userData);
              setUser(userData);
              setSessionEmail(sessionData1.email);
              authService.setUserData(userData);
            } 
          } catch (error) {
            console.error('[Auth] Token refresh failed:', error);
            // Clear invalid tokens

            authService.clearAuthData();
          }
        }
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
      try {
        setIsLoading(true);
        // First check if we have user data in localStorage
        const storedUser = authService.getUserData();
        if (storedUser) {
          setUser(storedUser);
          setSessionEmail(storedUser.email);
        }
        
        // Then verify with the server
        await checkSession();
      } catch (error) {
        console.error('[Auth] Initialization error:', error);
      } finally {
        setIsLoading(false);
      }
    };

    initializeAuth();

    // Set up a timer to check session periodically (e.g., every 5 minutes)
    const sessionCheckInterval = setInterval(checkSession, 5 * 60 * 1000);

    // Clean up interval on unmount
    return () => clearInterval(sessionCheckInterval);
  }, []);

  const login = async (credentials: { email: string; password: string; rememberMe: boolean }) => {
    try {
      setIsLoading(true);
      await authService.login(credentials, credentials.rememberMe);
      
      // After successful login, verify the session to get user data
      const sessionCheck = await authService.checkSession();
      
      if (sessionCheck.hasValidSession && sessionCheck.email) {
        const userData: User = {
          customerID: sessionCheck.customerID || 0,
          email: sessionCheck.email,
          fullName: sessionCheck.fullName || '',
        };
        
        // Update user data in context and localStorage
        setUser(userData);
        setHasValidSession(true);
        setSessionEmail(sessionCheck.email);
        authService.setUserData(userData);
      } else {
        throw new Error('Login successful but could not verify session');
      }
    } catch (error) {
      console.error('Login error:', error);
      setHasValidSession(false);
      setUser(null);
      setSessionEmail(null);
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



  const logout = async () => {
    try {
      setIsLoading(true);
      // Call authService.logout which handles both server and client cleanup
      const success = await authService.logout();
      
      if (!success) {
        throw new Error('Logout failed');
      }
      
      // Clear React state
      setUser(null);
      setHasValidSession(false);
      setSessionEmail(null);
      
      // Redirect to login page
      window.location.href = '/login';
    } catch (error) {
      console.error('Logout error:', error);
      // Even if there's an error, ensure we clear the local state
      setUser(null);
      setHasValidSession(false);
      setSessionEmail(null);
      authService.clearAuthData();
      window.location.href = '/login';
    } finally {
      setIsLoading(false);
    }
  };

  const changePassword = async (currentPassword: string, newPassword: string) => {
    if (!user?.email) throw new Error('No user email');
    await authService.changePassword(user.email, currentPassword, newPassword);
  };

  const changeEmail = async (password: string, newEmail: string) => {
    await authService.changeEmail(password, newEmail);
    // After backend rotates tokens and sets cookies, refresh session/user data
    const sessionData = await authService.checkSession();
    if (sessionData.hasValidSession && sessionData.email) {
      const updatedUser: User = {
        customerID: sessionData.customerID || 0,
        email: sessionData.email,
        fullName: sessionData.fullName || ''
      };
      setUser(updatedUser);
      authService.setUserData(updatedUser);
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
    changePassword,
    changeEmail,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};