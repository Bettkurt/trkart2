import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { validationUtils } from '@/utils/validationUtils';
import transactionService from '@/services/transactionService';
import { useAuth } from '@/contexts/AuthContext';

const TransactionFormPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  
  const [formData, setFormData] = useState({
    cardID: '',
    amount: '',
    transactionType: '',
    description: ''
  });

  const [errors, setErrors] = useState<{ [key: string]: string }>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [validationMessage, setValidationMessage] = useState('');

  // Real-time validation handlers
  const handleAmountChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = validationUtils.sanitizeAmount(e.target.value);
    setFormData(prev => ({ ...prev, amount: value }));
    
    const validation = validationUtils.validateAmount(value);
    setErrors(prev => ({
      ...prev,
      amount: validation.isValid ? '' : validation.error || ''
    }));
  };

  const handleTransactionTypeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = validationUtils.sanitizeTransactionType(e.target.value);
    setFormData(prev => ({ ...prev, transactionType: value }));
    
    const validation = validationUtils.validateTransactionType(value);
    setErrors(prev => ({
      ...prev,
      transactionType: validation.isValid ? '' : validation.error || ''
    }));
  };

  const handleDescriptionChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = validationUtils.sanitizeDescription(e.target.value);
    setFormData(prev => ({ ...prev, description: value }));
    
    const validation = validationUtils.validateDescription(value);
    setErrors(prev => ({
      ...prev,
      description: validation.isValid ? '' : validation.error || ''
    }));
  };

  const handleCardIdChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = validationUtils.sanitizeCardId(e.target.value);
    setFormData(prev => ({ ...prev, cardID: value }));
    
    const validation = validationUtils.validateCardId(value);
    setErrors(prev => ({
      ...prev,
      cardID: validation.isValid ? '' : validation.error || ''
    }));
  };

  // Backend validation for amount only
  const validateWithBackend = async () => {
    try {
      const amountValidation = await transactionService.validateAmount(formData.amount);
      if (!amountValidation.isValid) {
        setErrors(prev => ({
          ...prev,
          amount: amountValidation.message
        }));
        return false;
      }
      return true;
    } catch (error: any) {
      setValidationMessage(`Backend validation error: ${error.response?.data?.message || error.message}`);
      return false;
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setValidationMessage('');

    try {
      const backendValid = await validateWithBackend();
      
      if (!backendValid) {
        setIsSubmitting(false);
        return;
      }

      const transactionData = {
        cardID: parseInt(formData.cardID),
        amount: parseFloat(formData.amount),
        transactionType: formData.transactionType,
        description: formData.description
      };

      console.log('Submitting transaction data:', transactionData);
      const result = await transactionService.createUserTransaction(transactionData);
      console.log('Transaction result:', result);
      
      if (result.success) {
        setValidationMessage('✅ Transaction created successfully!');
        // Reset form
        setFormData({
          cardID: '',
          amount: '',
          transactionType: '',
          description: ''
        });
        setErrors({});
        
        // Redirect to transactions page after a short delay
        setTimeout(() => {
          navigate('/transactions');
        }, 1500);
      } else {
        setValidationMessage(`❌ Transaction failed: ${result.message}`);
      }
    } catch (error: any) {
      console.error('Transaction creation error:', error);
      
      if (error.response?.status === 401) {
        setValidationMessage('❌ Session expired. Please log in again.');
        // Redirect to login after a delay
        setTimeout(() => {
          window.location.href = '/login';
        }, 2000);
      } else {
        setValidationMessage(`❌ Error: ${error.response?.data?.message || error.message || 'Unknown error occurred'}`);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancel = () => {
    navigate('/transactions');
  };

  const testAuth = async () => {
    try {
      console.log('Current cookies:', document.cookie);
      
      const response = await fetch('http://localhost:7037/api/SecureTransaction/test-auth', {
        credentials: 'include' // Include cookies
      });
      const data = await response.json();
      console.log('Auth test result:', data);
      alert(`Auth test: ${data.message} (CustomerID: ${data.customerId}, SessionToken: ${data.sessionTokenPresent})`);
    } catch (error) {
      console.error('Auth test error:', error);
      alert('Auth test failed');
    }
  };

  const hasErrors = Object.values(errors).some(error => error !== '');

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <Link to="/transactions" className="text-gray-600 hover:text-gray-900 mr-4">
                ← Back to Transactions
              </Link>
              <h1 className="text-xl font-semibold text-gray-900">Create New Transaction</h1>
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

      <main className="max-w-2xl mx-auto py-6 sm:px-6 lg:px-8">
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

          {/* Transaction Form */}
          <div className="bg-white rounded-lg shadow-sm p-6">
            <form onSubmit={handleSubmit} className="space-y-6">
              {/* Card ID */}
              <div>
                <label htmlFor="cardID" className="block text-sm font-medium text-gray-700 mb-1">
                  Card ID
                </label>
                <input
                  type="text"
                  id="cardID"
                  value={formData.cardID}
                  onChange={handleCardIdChange}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
                    errors.cardID ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
                  }`}
                  placeholder="Enter card ID"
                  required
                />
                {errors.cardID && (
                  <p className="text-red-500 text-sm mt-1">{errors.cardID}</p>
                )}
              </div>

              {/* Amount */}
              <div>
                <label htmlFor="amount" className="block text-sm font-medium text-gray-700 mb-1">
                  Amount
                </label>
                <input
                  type="text"
                  id="amount"
                  value={formData.amount}
                  onChange={handleAmountChange}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
                    errors.amount ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
                  }`}
                  placeholder="Enter amount"
                  required
                />
                {errors.amount && (
                  <p className="text-red-500 text-sm mt-1">{errors.amount}</p>
                )}
              </div>

              {/* Transaction Type */}
              <div>
                <label htmlFor="transactionType" className="block text-sm font-medium text-gray-700 mb-1">
                  Transaction Type
                </label>
                <select
                  id="transactionType"
                  value={formData.transactionType}
                  onChange={handleTransactionTypeChange}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
                    errors.transactionType ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
                  }`}
                  required
                >
                  <option value="">Select transaction type</option>
                  <option value="Pay">Pay</option>
                  <option value="Load">Load</option>
                  <option value="Refund">Refund</option>
                  <option value="TransferIn">Transfer In</option>
                  <option value="TransferOut">Transfer Out</option>
                </select>
                {errors.transactionType && (
                  <p className="text-red-500 text-sm mt-1">{errors.transactionType}</p>
                )}
              </div>

              {/* Description */}
              <div>
                <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
                  Description
                </label>
                <input
                  type="text"
                  id="description"
                  value={formData.description}
                  onChange={handleDescriptionChange}
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
                    errors.description ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
                  }`}
                  placeholder="Enter description (letters and numbers only)"
                  required
                />
                {errors.description && (
                  <p className="text-red-500 text-sm mt-1">{errors.description}</p>
                )}
              </div>

              {/* Validation Message */}
              {validationMessage && (
                <div className={`p-3 rounded-md ${
                  validationMessage.includes('✅') ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
                }`}>
                  {validationMessage}
                </div>
              )}

              {/* Form Actions */}
              <div className="flex space-x-3 pt-4">
                <button
                  type="button"
                  onClick={handleCancel}
                  className="flex-1 py-2 px-4 border border-gray-300 rounded-md font-medium text-gray-700 hover:bg-gray-50 transition-colors"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isSubmitting || hasErrors}
                  className={`flex-1 py-2 px-4 rounded-md font-medium ${
                    isSubmitting || hasErrors
                      ? 'bg-gray-400 cursor-not-allowed'
                      : 'bg-blue-600 hover:bg-blue-700'
                  } text-white transition-colors`}
                >
                  {isSubmitting ? 'Processing...' : 'Create Transaction'}
                </button>
              </div>
            </form>
          </div>

          {/* Help Section */}
          <div className="mt-6 bg-blue-50 rounded-lg p-4">
            <h3 className="text-sm font-medium text-blue-900 mb-2">💡 Transaction Types</h3>
            <div className="text-sm text-blue-800 space-y-1">
              <p><strong>Pay:</strong> Payment transaction (negative amount)</p>
              <p><strong>Load:</strong> Loading money to card (positive amount)</p>
              <p><strong>Refund:</strong> Refund transaction (positive amount)</p>
              <p><strong>Transfer In:</strong> Money received (positive amount)</p>
              <p><strong>Transfer Out:</strong> Money sent (negative amount)</p>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default TransactionFormPage; 