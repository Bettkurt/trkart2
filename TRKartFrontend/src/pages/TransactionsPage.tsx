import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import transactionService from '@/services/transactionService';
import { useAuth } from '@/contexts/AuthContext';
import LoadingSpinner from '@/components/LoadingSpinner';

import { Transaction } from '@/types';

interface TransactionWithStatus extends Transaction {
  transactionStatus?: string;
}

const TransactionsPage: React.FC = () => {
  const { user } = useAuth();
  const [transactions, setTransactions] = useState<TransactionWithStatus[]>([]);
  const [allTransactions, setAllTransactions] = useState<TransactionWithStatus[]>([]);
  const [filteredTransactions, setFilteredTransactions] = useState<TransactionWithStatus[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  
  // Filtering states
  const [filterType, setFilterType] = useState<'customerID' | 'cardID'>('customerID');
  const [selectedCardID, setSelectedCardID] = useState<number | ''>('');
  const [availableCardIDs, setAvailableCardIDs] = useState<number[]>([]);
  const [transactionTypeFilter, setTransactionTypeFilter] = useState<string>('');
  const [dateRangeFilter, setDateRangeFilter] = useState<{ start: string; end: string }>({ start: '', end: '' });

  // Load transactions from API
  useEffect(() => {
    loadTransactions();
  }, [user]);

  // Apply filters when allTransactions or filter settings change
  useEffect(() => {
    applyFilters();
  }, [allTransactions, filterType, selectedCardID, transactionTypeFilter, dateRangeFilter]);

  // Helper function to save transactions to localStorage (user-specific)
  const saveTransactionsToStorage = (transactions: TransactionWithStatus[], userEmail?: string) => {
    try {
      const email = userEmail || user?.email;
      const userKey = email ? `trkart_transactions_${email}` : 'trkart_transactions_anonymous';
      localStorage.setItem(userKey, JSON.stringify(transactions));
      console.log(`Saved ${transactions.length} transactions for user: ${email}`);
    } catch (error) {
      console.error('Failed to save transactions to localStorage:', error);
    }
  };

  // Helper function to load transactions from localStorage (user-specific)
  const loadTransactionsFromStorage = (userEmail?: string): TransactionWithStatus[] => {
    try {
      const email = userEmail || user?.email;
      const userKey = email ? `trkart_transactions_${email}` : 'trkart_transactions_anonymous';
      const stored = localStorage.getItem(userKey);
      const transactions = stored ? JSON.parse(stored) : [];
      console.log(`Loaded ${transactions.length} transactions for user: ${email}`);
      return transactions;
    } catch (error) {
      console.error('Failed to load transactions from localStorage:', error);
      return [];
    }
  };

  // Enhanced filtering function
  const applyFilters = () => {
    let filtered = allTransactions;

    // Card ID filter
    if (filterType === 'cardID' && selectedCardID !== '') {
      filtered = filtered.filter(transaction => transaction.cardID === selectedCardID);
    }

    // Transaction type filter
    if (transactionTypeFilter) {
      filtered = filtered.filter(transaction => 
        transaction.transactionType.toLowerCase().includes(transactionTypeFilter.toLowerCase())
      );
    }

    // Date range filter
    if (dateRangeFilter.start || dateRangeFilter.end) {
      filtered = filtered.filter(transaction => {
        const transactionDate = new Date(transaction.transactionDate);
        const startDate = dateRangeFilter.start ? new Date(dateRangeFilter.start) : null;
        const endDate = dateRangeFilter.end ? new Date(dateRangeFilter.end) : null;

        if (startDate && endDate) {
          return transactionDate >= startDate && transactionDate <= endDate;
        } else if (startDate) {
          return transactionDate >= startDate;
        } else if (endDate) {
          return transactionDate <= endDate;
        }
        return true;
      });
    }

    setFilteredTransactions(filtered);
  };

  // Extract available CardIDs
  const extractAvailableCardIDs = (transactions: TransactionWithStatus[]) => {
    const cardIDs = [...new Set(transactions.map(t => t.cardID))];
    setAvailableCardIDs(cardIDs.sort((a, b) => a - b));
  };

  const loadTransactions = async () => {
    try {
      setLoading(true);
      setError('');
      
      console.log('Loading transactions for user:', user);
      
      // If no user email, show empty state
      if (!user?.email) {
        console.log('No user email, showing empty state');
        setAllTransactions([]);
        setAvailableCardIDs([]);
        setLoading(false);
        return;
      }

      // First, try to load from localStorage
      const storedTransactions = loadTransactionsFromStorage(user.email);
      console.log('Stored transactions:', storedTransactions);
      
      // If user has email but no customerID, use stored transactions
      if (!user?.customerID || user.customerID === 0) {
        console.log('No customerID, using stored transactions');
        setAllTransactions(storedTransactions);
        extractAvailableCardIDs(storedTransactions);
        setLoading(false);
        return;
      }

      // Try to fetch from API using secure endpoint
      try {
        console.log('Fetching transactions from API...');
        const apiTransactions = await transactionService.getUserTransactions();
        console.log('API transactions received:', apiTransactions);
        
        const transactionsWithStatus = apiTransactions.map((t: any) => ({
          ...t,
          transactionStatus: t.transactionStatus || 'API'
        }));
        
        console.log('Transactions with status:', transactionsWithStatus);
        setAllTransactions(transactionsWithStatus);
        extractAvailableCardIDs(transactionsWithStatus);
        saveTransactionsToStorage(transactionsWithStatus, user.email);
      } catch (apiError) {
        console.error('API call failed, using stored transactions:', apiError);
        setAllTransactions(storedTransactions);
        extractAvailableCardIDs(storedTransactions);
      }
    } catch (error) {
      console.error('Error loading transactions:', error);
      setError('Failed to load transactions');
    } finally {
      setLoading(false);
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'API':
        return '🟢';
      case 'Demo':
        return '🟡';
      default:
        return '⚪';
    }
  };

  const formatAmount = (amount: number, transactionType?: string) => {
    const isNegative = ['Pay', 'TransferOut'].includes(transactionType || '');
    const sign = isNegative ? '-' : '+';
    const color = isNegative ? 'text-red-600' : 'text-green-600';
    return <span className={color}>{sign}${Math.abs(amount).toFixed(2)}</span>;
  };

  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return date.toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return dateString;
    }
  };

  const clearFilters = () => {
    setFilterType('customerID');
    setSelectedCardID('');
    setTransactionTypeFilter('');
    setDateRangeFilter({ start: '', end: '' });
  };

  if (loading) {
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
              <Link to="/dashboard" className="text-gray-600 hover:text-gray-900 mr-4">
                ← Back to Dashboard
              </Link>
              <h1 className="text-xl font-semibold text-gray-900">Transaction History</h1>
            </div>
            <div className="flex items-center space-x-4">
              <Link
                to="/new-transaction"
                className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors"
              >
                + New Transaction
              </Link>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
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

          {/* Filters Section */}
          <div className="bg-white rounded-lg shadow-sm p-6 mb-6">
            <div className="flex items-center justify-between mb-4">
              <h2 className="text-lg font-medium text-gray-900">Filters</h2>
              <button
                onClick={clearFilters}
                className="text-sm text-gray-500 hover:text-gray-700"
              >
                Clear All Filters
              </button>
            </div>
            
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
              {/* Filter Type */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Filter By
                </label>
                <select
                  value={filterType}
                  onChange={(e) => setFilterType(e.target.value as 'customerID' | 'cardID')}
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
                  <option value="customerID">All Transactions</option>
                  <option value="cardID">By Card ID</option>
                </select>
              </div>

              {/* Card ID Filter */}
              {filterType === 'cardID' && (
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">
                    Card ID
                  </label>
                  <select
                    value={selectedCardID}
                    onChange={(e) => setSelectedCardID(e.target.value ? Number(e.target.value) : '')}
                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  >
                    <option value="">All Cards</option>
                    {availableCardIDs.map(cardID => (
                      <option key={cardID} value={cardID}>Card {cardID}</option>
                    ))}
                  </select>
                </div>
              )}

              {/* Transaction Type Filter */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Transaction Type
                </label>
                <input
                  type="text"
                  value={transactionTypeFilter}
                  onChange={(e) => setTransactionTypeFilter(e.target.value)}
                  placeholder="Filter by type..."
                  className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
              </div>

              {/* Date Range Filter */}
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Date Range
                </label>
                <div className="flex space-x-2">
                  <input
                    type="date"
                    value={dateRangeFilter.start}
                    onChange={(e) => setDateRangeFilter(prev => ({ ...prev, start: e.target.value }))}
                    className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                  <input
                    type="date"
                    value={dateRangeFilter.end}
                    onChange={(e) => setDateRangeFilter(prev => ({ ...prev, end: e.target.value }))}
                    className="flex-1 px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Error Message */}
          {error && (
            <div className="bg-red-50 border border-red-200 text-red-700 px-4 py-3 rounded mb-6">
              {error}
            </div>
          )}

          {/* Transactions List */}
          <div className="bg-white rounded-lg shadow-sm overflow-hidden">
            <div className="px-6 py-4 border-b border-gray-200">
              <div className="flex items-center justify-between">
                <h2 className="text-lg font-medium text-gray-900">
                  Transactions ({filteredTransactions.length})
                </h2>
                <div className="text-sm text-gray-500">
                  Showing {filteredTransactions.length} of {allTransactions.length} transactions
                </div>
              </div>
            </div>

            {filteredTransactions.length === 0 ? (
              <div className="text-center py-12">
                <div className="text-gray-400 text-6xl mb-4">📊</div>
                <h3 className="text-lg font-medium text-gray-900 mb-2">No transactions found</h3>
                <p className="text-gray-500 mb-6">
                  {allTransactions.length === 0 
                    ? "You don't have any transactions yet." 
                    : "No transactions match your current filters."}
                </p>
                {allTransactions.length === 0 && (
                  <Link
                    to="/new-transaction"
                    className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors"
                  >
                    Create Your First Transaction
                  </Link>
                )}
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-gray-200">
                  <thead className="bg-gray-50">
                    <tr>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Status
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Date
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Card ID
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Type
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Amount
                      </th>
                      <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                        Description
                      </th>
                    </tr>
                  </thead>
                  <tbody className="bg-white divide-y divide-gray-200">
                    {filteredTransactions.map((transaction, index) => (
                      <tr key={transaction.transactionID || index} className="hover:bg-gray-50">
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                          {getStatusIcon(transaction.transactionStatus || '')}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                          {formatDate(transaction.transactionDate)}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                          {transaction.cardID}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                          {transaction.transactionType}
                        </td>
                        <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                          {formatAmount(transaction.amount, transaction.transactionType)}
                        </td>
                        <td className="px-6 py-4 text-sm text-gray-900 max-w-xs truncate">
                          {transaction.description}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </div>
      </main>
    </div>
  );
};

export default TransactionsPage; 