import React, { useState, useEffect } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { TopUpResponse } from '@/types';
import TopUpForm from '@/components/TopUpForm';
import LoadingSpinner from '@/components/LoadingSpinner';
import { logger } from '@/utils/logger';

const TopUpPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  
  const [isLoading, setIsLoading] = useState(true);
  const [successMessage, setSuccessMessage] = useState<string>('');

  // Log component mount
  useEffect(() => {
    logger.info('TopUpPage', 'mount', 'Top-up page loaded', {
      path: location.pathname,
      hasUser: !!user,
      customerId: user?.customerID
    });

    // Check authentication
    if (!user?.customerID) {
      logger.warn('TopUpPage', 'auth', 'No user or customer ID found');
      navigate('/login');
      return;
    }

    setIsLoading(false);

    return () => {
      logger.debug('TopUpPage', 'unmount', 'Top-up page unmounting');
    };
  }, [user, location.pathname, navigate]);

  const handleTopUpSuccess = (response: TopUpResponse) => {
    logger.info('TopUpPage', 'success', 'Top-up completed successfully', {
      transactionId: response.transaction?.transactionID,
      cardNumber: response.transaction?.cardNumber,
      amount: response.transaction?.amount,
      netAmount: response.transaction?.netAmount,
      correlationId: response.correlationId
    });

    setSuccessMessage(`✅ Top-up completed successfully! Transaction ID: ${response.transaction?.transactionID}`);

    // Redirect to transactions page after a short delay
    setTimeout(() => {
      logger.debug('TopUpPage', 'navigation', 'Redirecting to transactions page');
      navigate('/transactions');
    }, 3000);
  };

  const handleCancel = () => {
    logger.info('TopUpPage', 'navigation', 'User cancelled top-up, returning to transactions list');
    navigate('/transactions');
  };

  const testAuth = async () => {
    logger.debug('TopUpPage', 'auth', 'Testing authentication status');
    
    try {
      logger.debug('TopUpPage', 'auth', 'Current authentication cookies', {
        cookies: document.cookie.split('; ').filter(c => c.startsWith('trkart_'))
      });
      
      const response = await fetch('http://localhost:7037/api/SecureTransaction/test-auth', {
        credentials: 'include' // Include cookies
      });
      
      const data = await response.json();
      logger.info('TopUpPage', 'auth', 'Authentication test successful', {
        customerId: data.customerId,
        accessTokenPresent: data.accessTokenPresent,
        refreshTokenPresent: data.refreshTokenPresent
      });
      
      alert(`Auth test: ${data.message} (CustomerID: ${data.customerId}, AccessToken: ${data.accessTokenPresent}, RefreshToken: ${data.refreshTokenPresent})`);
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.error('TopUpPage', 'auth', 'Authentication test failed', err);
      alert('Auth test failed. Check console for details.');
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <LoadingSpinner />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <Link to="/transactions" className="text-gray-600 hover:text-gray-900 mr-4">
                ← Back to Transactions
              </Link>
              <h1 className="text-xl font-semibold text-gray-900">Top-Up Card</h1>
            </div>
            <div className="flex items-center space-x-2">
              <button
                onClick={testAuth}
                className="text-sm bg-gray-500 hover:bg-gray-600 text-white px-3 py-1 rounded"
              >
                Test Auth
              </button>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-4xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          {/* User Status */}
          {user?.email && (
            <div className="bg-blue-50 border border-blue-200 text-blue-700 px-4 py-2 rounded mb-6 text-sm">
              <div className="flex items-center justify-between">
                <span>Logged in as: {user.email}</span>
                {user.customerID && user.customerID > 0 && (
                  <span className="text-xs">Customer ID: {user.customerID}</span>
                )}
              </div>
            </div>
          )}

          {/* Success Message */}
          {successMessage && (
            <div className="bg-green-100 border border-green-200 text-green-800 px-4 py-3 rounded mb-6">
              <div className="flex items-center justify-between">
                <span>{successMessage}</span>
                <span className="text-xs">Redirecting to transactions...</span>
              </div>
            </div>
          )}

          {/* Top-Up Form */}
          <div className="bg-white rounded-lg shadow-sm">
            <TopUpForm 
              onSubmit={handleTopUpSuccess}
              onCancel={handleCancel}
            />
          </div>

          {/* Information Section */}
          <div className="mt-8 grid grid-cols-1 md:grid-cols-2 gap-6">
            {/* How Top-Up Works */}
            <div className="bg-blue-50 rounded-lg p-6">
              <h3 className="text-lg font-medium text-blue-900 mb-4">💡 How Top-Up Works</h3>
              <div className="text-sm text-blue-800 space-y-2">
                <p><strong>1. Card Validation:</strong> We verify the card is active and belongs to you</p>
                <p><strong>2. Payment Processing:</strong> Your payment is processed through the selected method</p>
                <p><strong>3. Balance Update:</strong> The net amount (minus fees) is added to your card</p>
                <p><strong>4. Confirmation:</strong> You receive a transaction confirmation with tracking ID</p>
              </div>
            </div>

            {/* Security Features */}
            <div className="bg-green-50 rounded-lg p-6">
              <h3 className="text-lg font-medium text-green-900 mb-4">🔒 Security Features</h3>
              <div className="text-sm text-green-800 space-y-2">
                <p><strong>Secure Processing:</strong> All payments use encrypted connections</p>
                <p><strong>Authorization:</strong> Only you can top up your own cards</p>
                <p><strong>Duplicate Prevention:</strong> External references prevent duplicate charges</p>
                <p><strong>Amount Limits:</strong> $10 - $10,000 per transaction for safety</p>
              </div>
            </div>

            {/* Payment Methods */}
            <div className="bg-yellow-50 rounded-lg p-6">
              <h3 className="text-lg font-medium text-yellow-900 mb-4">💳 Supported Payment Methods</h3>
              <div className="text-sm text-yellow-800 space-y-2">
                <p><strong>Credit Card:</strong> Visa, MasterCard, American Express</p>
                <p><strong>Bank Transfer:</strong> Direct bank account transfers</p>
                <p><strong>Wire Transfer:</strong> International wire transfers</p>
                <p><strong>Digital Wallets:</strong> PayPal, Stripe</p>
                <p><strong>Cash:</strong> At participating locations</p>
              </div>
            </div>

            {/* Transaction Limits */}
            <div className="bg-purple-50 rounded-lg p-6">
              <h3 className="text-lg font-medium text-purple-900 mb-4">📊 Transaction Limits</h3>
              <div className="text-sm text-purple-800 space-y-2">
                <p><strong>Minimum Amount:</strong> $10.00 per transaction</p>
                <p><strong>Maximum Amount:</strong> $10,000.00 per transaction</p>
                <p><strong>Daily Limit:</strong> Contact support for higher limits</p>
                <p><strong>Processing Time:</strong> Instant for most payment methods</p>
                <p><strong>Fees:</strong> Varies by payment method (shown before confirmation)</p>
              </div>
            </div>
          </div>

          {/* Development Notice */}
          {process.env.NODE_ENV === 'development' && (
            <div className="mt-6 bg-orange-50 border border-orange-200 rounded-lg p-4">
              <h3 className="text-sm font-medium text-orange-900 mb-2">🚧 Development Mode</h3>
              <div className="text-sm text-orange-800 space-y-1">
                <p>• Top-ups are automatically approved for testing</p>
                <p>• No real payment processing occurs</p>
                <p>• Use the "Test Auth" button to verify authentication</p>
                <p>• All transaction data is stored for development purposes</p>
              </div>
            </div>
          )}
        </div>
      </main>
    </div>
  );
};

export default TopUpPage;
