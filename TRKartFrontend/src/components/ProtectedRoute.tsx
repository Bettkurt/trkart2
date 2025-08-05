import { Navigate } from 'react-router-dom';
import { useContext, useEffect, useState } from 'react';
import UserContext from '@/contexts/UserContext';
import sessionService from '@/services/sessionService';
import tokenService from '@/services/tokenService';

interface ProtectedRouteProps {
  children: JSX.Element;
}

const ProtectedRoute = ({ children }: ProtectedRouteProps) => {
  const { user } = useContext(UserContext);
  const [isChecking, setIsChecking] = useState(true);
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  useEffect(() => {
    const checkAuth = async () => {
      // First check if we have valid tokens
      if (sessionService.isAuthenticated()) {
        // If the access token is expired, try to refresh it
        if (sessionService.isAccessTokenExpired()) {
          try {
            // This will attempt to refresh the token if needed
            const token = await tokenService.ensureValidToken();
            setIsAuthenticated(!!token);
          } catch (error) {
            console.error('Token refresh failed in protected route:', error);
            setIsAuthenticated(false);
          }
        } else {
          setIsAuthenticated(true);
        }
      } else {
        setIsAuthenticated(false);
      }

      setIsChecking(false);
    };

    checkAuth();
  }, []);

  // While checking authentication status, show a loading indicator
  if (isChecking) {
    return <div className="flex justify-center items-center h-screen">Verifying authentication...</div>;
  }

  // If not authenticated, redirect to login
  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace />;
  }

  // User is authenticated, render the protected component
  return children;
};
