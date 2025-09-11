import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import { walletService } from '@/services/walletService';
import { WalletDto, WalletTransactionDto, TransactionStatus } from '@/types/wallet';
import { logger } from '@/utils/logger';

const WalletTransactionsPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [wallet, setWallet] = useState<WalletDto | null>(null);
  const [transactions, setTransactions] = useState<WalletTransactionDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Fetch wallet data and transactions
  useEffect(() => {
    const fetchWalletData = async () => {
      if (!user?.customerID) return;
      
      setIsLoading(true);
      setError(null);
      
      try {
        // Fetch wallet info
        const walletData = await walletService.getWalletByCustomerId(user.customerID);
        setWallet(walletData);
        
        // Fetch all transactions
        const transactionsData = await walletService.getWalletTransactions(walletData.walletId);
        setTransactions(transactionsData);
      } catch (err) {
        const error = err as Error;
        logger.error('WalletTransactionsPage', 'fetchWalletData', 'Failed to fetch wallet transactions', error);
        setError('Failed to load wallet transactions. Please try again later.');
      } finally {
        setIsLoading(false);
      }
    };

    fetchWalletData();
  }, [user?.customerID]);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="text-center">
          <div className="w-16 h-16 border-4 border-blue-500 border-t-transparent rounded-full animate-spin mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading wallet transactions...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 flex items-center justify-center">
        <div className="bg-white p-8 rounded-lg shadow-md max-w-md w-full text-center">
          <div className="text-red-500 text-5xl mb-4">⚠️</div>
          <h2 className="text-xl font-semibold text-gray-800 mb-2">Error Loading Transactions</h2>
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
          <button
            onClick={() => navigate('/dashboard')}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 transition-colors"
          >
            Back to Dashboard
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        {/* Header */}
        <div className="flex justify-between items-center mb-8">
          <div>
            <h1 className="text-2xl font-bold text-gray-900">Wallet Transactions</h1>
            <p className="text-gray-600">View all your wallet transactions</p>
          </div>
          <div className="flex items-center
          ">
            <span className="text-sm font-medium text-gray-700 mr-4">
              Balance: <span className="font-semibold">{walletService.formatCurrency(wallet.balance)}</span>
            </span>
            <button
              onClick={() => navigate('/wallet')}
              className="px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-md hover:bg-blue-700 transition-colors"
            >
              Back to Wallet
            </button>
          </div>
        </div>

        {/* Transactions List */}
        <div className="bg-white rounded-xl shadow-md overflow-hidden">
          {transactions.length === 0 ? (
            <div className="text-center py-12">
              <div className="text-gray-400 text-5xl mb-4">📭</div>
              <h3 className="text-lg font-medium text-gray-900 mb-1">No transactions yet</h3>
              <p className="text-gray-500">Your wallet transactions will appear here</p>
              <button
                onClick={() => navigate('/wallet')}
                className="mt-4 px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-md hover:bg-blue-700 transition-colors"
              >
                Make Your First Transaction
              </button>
            </div>
          ) : (
            <div className="divide-y divide-gray-200">
              {transactions.map((transaction) => (
                <div key={transaction.transactionId} className="p-6 hover:bg-gray-50 transition-colors">
                  <div className="flex justify-between items-start">
                    <div>
                      <h3 className="text-base font-medium text-gray-900">
                        {transaction.description}
                      </h3>
                      <p className="text-sm text-gray-500 mt-1">
                        {walletService.formatDate(transaction.transactionDate)}
                      </p>
                      {transaction.referenceId && (
                        <p className="text-xs text-gray-400 mt-1">
                          Ref: {transaction.referenceId}
                        </p>
                      )}
                    </div>
                    <div className="text-right">
                      <div className={`text-base font-medium ${
                        walletService.getTransactionSignClass(transaction.transactionType)
                      }`}>
                        {walletService.getTransactionSign(transaction.transactionType)}
                        {walletService.formatCurrency(transaction.amount).replace('$', '')}
                      </div>
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        transaction.status.toString().toUpperCase() === 'APPROVED' || 
                        transaction.status === TransactionStatus.Approved
                          ? 'bg-green-100 text-green-800'
                          : transaction.status.toString().toUpperCase() === 'PENDING' || 
                            transaction.status === TransactionStatus.Pending
                          ? 'bg-yellow-100 text-yellow-800'
                          : 'bg-red-100 text-red-800'
                      }`}>
                        {transaction.status.toString().charAt(0) + 
                         transaction.status.toString().slice(1).toLowerCase()}
                      </span>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default WalletTransactionsPage;
