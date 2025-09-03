import React, { useEffect } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import TransferForm from '@/components/TransferForm';
import { useAuth } from '@/contexts/AuthContext';
import { logger } from '@/utils/logger';
import transferService from '@/services/transferService';

const NewTransferPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [fromCardId, setFromCardId] = React.useState<string | null>(null);

  useEffect(() => {
    // Get fromCardId from URL if present
    const searchParams = new URLSearchParams(location.search);
    const cardIdParam = searchParams.get('fromCardId');
    
    if (cardIdParam) {
      logger.debug('NewTransferPage', 'mount', 'Found fromCardId in URL', { 
        fromCardId: cardIdParam 
      });
      setFromCardId(cardIdParam);
    }
  }, [location.search]);

  const handleTransferSubmit = async (transfer: any) => {
    try {
      logger.info('NewTransferPage', 'handleTransferSubmit', 'Submitting transfer', { transfer });
      
      // Convert amount to number and card ID to number
      const transferData = {
        ...transfer,
        amount: parseFloat(transfer.amount),
        senderCardID: parseInt(transfer.senderCardID, 10)
      };

      // Call the transfer service
      const result = await transferService.createTransfer(transferData);
      
      if (result.success) {
        logger.info('NewTransferPage', 'handleTransferSubmit', 'Transfer successful', { result });
        // Show success message and redirect to transfers page
        alert('Transfer completed successfully!');
        navigate('/transfers');
      } else {
        const error = new Error(result.message);
        logger.error('NewTransferPage', 'handleTransferSubmit', 'Transfer failed', error);
        alert(`Transfer failed: ${result.message}`);
      }
    } catch (error: any) {
      const err = new Error(error.message);
      logger.error('NewTransferPage', 'handleTransferSubmit', 'Error during transfer', err);
      alert(`An error occurred: ${error.message || 'Please try again later.'}`);
    }
  };

  // Redirect if not authenticated
  if (!user) {
    navigate('/login');
    return null;
  }

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow-sm border-b">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <div className="flex items-center">
              <Link to="/" className="text-2xl font-bold text-gray-900">
                TRKart
              </Link>
            </div>
            <nav className="flex space-x-8">
              <Link to="/dashboard" className="text-gray-500 hover:text-gray-900">
                Dashboard
              </Link>
              <Link to="/transactions" className="text-gray-500 hover:text-gray-900">
                Transactions
              </Link>
              <Link to="/transfers" className="text-gray-500 hover:text-gray-900">
                Transfers
              </Link>
              <Link to="/cards" className="text-gray-500 hover:text-gray-900">
                My Cards
              </Link>
            </nav>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          {/* Page Header */}
          <div className="mb-8">
            <div className="flex items-center justify-between">
              <div>
                <h1 className="text-3xl font-bold text-gray-900">New Transfer</h1>
                <p className="mt-2 text-gray-600">
                  Transfer money between cards securely with linked transaction tracking.
                </p>
              </div>
              <Link
                to="/transfers"
                className="inline-flex items-center px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
              >
                ← Back to Transfers
              </Link>
            </div>
          </div>

          {/* Transfer Form */}
          <div className="mt-8 sm:mx-auto sm:w-full sm:max-w-md">
            <div className="bg-white py-8 px-4 shadow sm:rounded-lg sm:px-10">
              <TransferForm 
                onSubmit={handleTransferSubmit} 
                initialFromCardId={fromCardId}
              />
            </div>
          </div>

          {/* Information Section */}
          <div className="mt-12 max-w-3xl mx-auto">
            <div className="bg-white shadow rounded-lg p-6">
              <h2 className="text-xl font-semibold text-gray-900 mb-4">How Transfers Work</h2>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <h3 className="font-medium text-gray-900 mb-2">Transfer Process</h3>
                  <ul className="text-sm text-gray-600 space-y-1">
                    <li>• Select your card as the sender</li>
                    <li>• Enter the recipient's card number</li>
                    <li>• Specify the transfer amount</li>
                    <li>• System creates linked transactions</li>
                  </ul>
                </div>
                <div>
                  <h3 className="font-medium text-gray-900 mb-2">Transaction Linking</h3>
                  <ul className="text-sm text-gray-600 space-y-1">
                    <li>• TransferOut: Deducts from sender card</li>
                    <li>• TransferIn: Credits recipient card</li>
                    <li>• Both transactions are cross-linked</li>
                    <li>• Full audit trail maintained</li>
                  </ul>
                </div>
              </div>
            </div>
          </div>

          {/* Security Notice */}
          <div className="mt-8 max-w-3xl mx-auto">
            <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
              <div className="flex">
                <div className="flex-shrink-0">
                  <svg className="h-5 w-5 text-yellow-400" viewBox="0 0 20 20" fill="currentColor">
                    <path fillRule="evenodd" d="M8.257 3.099c.765-1.36 2.722-1.36 3.486 0l5.58 9.92c.75 1.334-.213 2.98-1.742 2.98H4.42c-1.53 0-2.493-1.646-1.743-2.98l5.58-9.92zM11 13a1 1 0 11-2 0 1 1 0 012 0zm-1-8a1 1 0 00-1 1v3a1 1 0 002 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
                  </svg>
                </div>
                <div className="ml-3">
                  <h3 className="text-sm font-medium text-yellow-800">Security Notice</h3>
                  <div className="mt-2 text-sm text-yellow-700">
                    <p>
                      All transfers are processed securely with linked transaction records. 
                      Please verify the recipient card number carefully before confirming the transfer.
                    </p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default NewTransferPage; 