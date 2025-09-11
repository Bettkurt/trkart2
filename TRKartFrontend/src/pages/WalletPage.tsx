import React, { useEffect, useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { walletService } from '@/services/walletService';
import { WalletDto, WalletTransactionDto } from '@/types/wallet';
import { logger } from '@/utils/logger';
import LoadWalletForm from '@/components/wallet/LoadWalletForm';
import PayFromWalletForm from '@/components/wallet/PayFromWalletForm';

const WalletPage: React.FC = () => {
  const { user } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<'load' | 'pay'>('load');
  const [wallet, setWallet] = useState<WalletDto | null>(null);
  const [transactions, setTransactions] = useState<WalletTransactionDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Check URL hash to set active tab
  useEffect(() => {
    const hash = window.location.hash;
    if (hash === '#pay') {
      setActiveTab('pay');
    } else {
      setActiveTab('load');
    }
  }, [location]);

  // Fetch wallet data when user changes
  useEffect(() => {
    const fetchData = async () => {
      if (!user?.customerID) {
        console.error('No customer ID found in user context');
        setError('User not authenticated');
        return;
      }
      
      try {
        setIsLoading(true);
        console.log('Fetching wallet data for customer ID:', user.customerID);
        
        // Fetch wallet data
        const walletData = await walletService.getWalletByCustomerId(user.customerID);
        console.log('Wallet data received:', walletData);
        setWallet(walletData);
        
        if (walletData?.walletId) {
          console.log('Fetching transactions for wallet ID:', walletData.walletId);
          const transactions = await walletService.getWalletTransactions(walletData.walletId);
          console.log('Transactions received:', transactions);
          setTransactions(transactions);
        }
      } catch (err: any) {
        console.error('Error in fetchData:', {
          error: err,
          response: err.response?.data,
          status: err.response?.status,
          headers: err.response?.headers
        });
        setError(err.response?.data?.message || 'Failed to load wallet data. Please try again.');
      } finally {
        setIsLoading(false);
      }
    };
    
    fetchData();
  }, [user?.customerID]);

  const handleTabChange = (tab: 'load' | 'pay') => {
    setActiveTab(tab);
    navigate(`/wallet#${tab}`);
  };

  const handleTransactionComplete = async () => {
    if (!wallet) return;
    
    try {
      // Refresh wallet data after transaction
      const updatedWallet = await walletService.getWalletByCustomerId(user!.customerID);
      setWallet(updatedWallet);
      
      // Refresh transactions
      const transactionsData = await walletService.getWalletTransactions(wallet.walletId);
      setTransactions(transactionsData);
    } catch (err) {
      const error = err as Error;
      logger.error('WalletPage', 'handleTransactionComplete', 'Failed to refresh wallet data', error);
      setError('Transaction completed but failed to refresh data. Please refresh the page.');
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-16 h-16 border-4 border-blue-500 border-t-transparent rounded-full animate-spin mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading wallet information...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md max-w-md w-full text-center">
          <div className="text-red-500 text-5xl mb-4">⚠️</div>
          <h2 className="text-xl font-semibold text-gray-800 mb-2">Error Loading Wallet</h2>
          <p className="text-gray-600 mb-6">{error}</p>
          <button
            onClick={() => window.location.reload()}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  if (!wallet) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md max-w-md w-full text-center">
          <div className="text-yellow-500 text-5xl mb-4">👛</div>
          <h2 className="text-xl font-semibold text-gray-800 mb-2">No Wallet Found</h2>
          <p className="text-gray-600 mb-6">You don't have a wallet yet. Please contact support to create one.</p>
          <Link
            to="/dashboard"
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors inline-block"
          >
            Back to Dashboard
          </Link>
        </div>
      </div>
    );
  }


  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        <div className="mb-4">
          <button
            onClick={() => navigate('/dashboard')}
            className="flex items-center text-blue-600 hover:text-blue-800 mb-4"
          >
            <svg className="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            Back to Dashboard
          </button>
        </div>
        
        {/* Wallet Card */}
        <div className="bg-white rounded-xl shadow-md overflow-hidden mb-8">
          <div className="p-6">
            <div className="flex justify-between items-start mb-6">
              <div>
                <h2 className="text-2xl font-bold text-gray-800">My Wallet</h2>
                <p className="text-gray-600 mt-1">
                  Wallet: {wallet.walletNumber}
                </p>
                <span className={`inline-block px-3 py-1 rounded-full text-sm font-medium ${
                  wallet.status === 4 // 4 = Active in CardStatus enum
                    ? 'bg-green-100 text-green-800' 
                    : 'bg-yellow-100 text-yellow-800'
                }`}>
                  {wallet.status === 4 ? 'Active' : 'Inactive'}
                </span>
              </div>
              <Link
                to="/wallet/transactions"
                className="text-blue-600 hover:text-blue-800 text-sm font-medium"
              >
                View All Transactions
              </Link>
            </div>

            <div className="bg-gray-50 p-4 rounded-lg mb-6">
              <div className="text-sm text-gray-500 mb-1">Available Balance</div>
              <div className="text-3xl font-bold text-gray-900">
                {walletService.formatCurrency(wallet.balance)}
              </div>
              <div className="text-sm text-gray-500 mt-2">
                Last updated: {new Date(wallet.updatedAt).toLocaleString()}
              </div>
            </div>

            {/* Tabs */}
            <div className="flex border-b border-gray-200 mb-6">
              <button
                className={`py-2 px-4 font-medium text-sm ${
                  activeTab === 'load'
                    ? 'border-b-2 border-blue-500 text-blue-600'
                    : 'text-gray-500 hover:text-gray-700'
                }`}
                onClick={() => handleTabChange('load')}
              >
                Load Money
              </button>
              <button
                className={`py-2 px-4 font-medium text-sm ${
                  activeTab === 'pay'
                    ? 'border-b-2 border-blue-500 text-blue-600'
                    : 'text-gray-500 hover:text-gray-700'
                }`}
                onClick={() => handleTabChange('pay')}
              >
                Pay
              </button>
            </div>

            {/* Tab Content */}
            <div className="mb-6">
              {activeTab === 'load' ? (
                <LoadWalletForm 
                  walletId={wallet.walletId} 
                  onSuccess={handleTransactionComplete} 
                />
              ) : (
                <PayFromWalletForm 
                  walletId={wallet.walletId}
                  balance={wallet.balance}
                  onSuccess={handleTransactionComplete}
                />
              )}
            </div>
          </div>
        </div>

        {/* Recent Transactions */}
        <div className="bg-white rounded-xl shadow-md overflow-hidden">
          <div className="p-6">
            <div className="flex justify-between items-center mb-4">
              <h2 className="text-lg font-semibold text-gray-800">Recent Transactions</h2>
              <Link
                to="/wallet/transactions"
                className="text-sm text-blue-600 hover:text-blue-800 font-medium"
              >
                See All
              </Link>
            </div>

            {transactions.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                No transactions yet
              </div>
            ) : (
              <div className="space-y-4">
                {transactions.map((transaction) => {
                  console.log('Transaction:', {
                    id: transaction.transactionId,
                    type: transaction.transactionType,
                    isWalletLoad: transaction.transactionType === 'WalletLoad',
                    amount: transaction.amount,
                    description: transaction.description
                  });
                  return (
                  <div key={transaction.transactionId} className="flex justify-between items-center py-3 border-b border-gray-100">
                    <div>
                      <div className="font-medium text-gray-900">{transaction.description}</div>
                      <div className="text-sm text-gray-500">
                        {walletService.formatDate(transaction.transactionDate)}
                      </div>
                    </div>
                    <div className={`font-medium ${
                      walletService.getTransactionSignClass(transaction.transactionType)
                    }`}>
                      {walletService.getTransactionSign(transaction.transactionType)}
                      {walletService.formatCurrency(transaction.amount).replace('$', '')}
                    </div>
                  </div>
                );
                })}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default WalletPage;
