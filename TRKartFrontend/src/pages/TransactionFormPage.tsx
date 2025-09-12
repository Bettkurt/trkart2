import React, { useState, useEffect } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { validationUtils } from '@/utils/validationUtils';
import transactionService from '@/services/transactionService';
import userCardService from '@/services/userCardService';
import { useAuth } from '@/contexts/AuthContext';
import { UserCard, CreateTransactionRequest } from '@/types';
import { CardStatus } from '@/types/cardStatus';
import { TransactionType, getTransactionTypeName, getTransactionFormPageTypes } from '@/types/TransactionType';
import LoadingSpinner from '@/components/LoadingSpinner';
import { logger } from '@/utils/logger';

const TransactionFormPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [formData, setFormData] = useState<{
    cardID: string;
    amount: string;
    transactionType: TransactionType | '';
    description: string;
  }>({
    cardID: '',
    amount: '',
    transactionType: '',
    description: ''
  });

  const [errors, setErrors] = useState<{ [key: string]: string }>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [validationMessage, setValidationMessage] = useState('');
  const [userCards, setUserCards] = useState<UserCard[]>([]);
  const [loadingCards, setLoadingCards] = useState(true);

  // Log component mount and load user cards
  useEffect(() => {
    logger.info('TransactionForm', 'mount', 'Transaction form page loaded', {
      path: location.pathname,
      search: location.search,
      hasUser: !!user,
      customerId: user?.customerID
    });

    // Get cardId from URL if present and valid
    let cardIdFromUrl: string | null = null;
    let cardIdNum: number | null = null;

    const searchParams = new URLSearchParams(location.search);
    const cardIdParam = searchParams.get('cardId');

    if (cardIdParam) {
      const parsedId = parseInt(cardIdParam, 10);
      if (!isNaN(parsedId)) {
        cardIdFromUrl = cardIdParam; // Keep original string for form state
        cardIdNum = parsedId; // Number for comparison with card IDs

        logger.debug('TransactionForm', 'mount', 'Found valid cardId in URL', {
          cardId: cardIdFromUrl,
          cardIdNum
        });
      } else {
        logger.warn('TransactionForm', 'mount', 'Invalid cardId in URL', {
          cardId: cardIdParam
        });
      }
    }

    const loadUserCards = async () => {
      if (!user?.customerID) {
        logger.warn('TransactionForm', 'loadCards', 'No user or customer ID found');
        setLoadingCards(false);
        return;
      }

      try {
        setLoadingCards(true);
        logger.debug('TransactionForm', 'loadCards', 'Loading user cards');

        const cards = await userCardService.getUserCards();

        // Filter to only show Active and Inactive cards (can deposit to both)
        const availableCards = cards.filter(card =>
          card.cardStatus === CardStatus.Active || card.cardStatus === CardStatus.Inactive
        );

        // Log which cards are being filtered out
        const filteredCards = cards.filter(card =>
          card.cardStatus === CardStatus.Deactivated || 
          card.cardStatus === CardStatus.Expired || 
          card.cardStatus === CardStatus.Lost
        );

        if (filteredCards.length > 0) {
          logger.info('TransactionForm', 'loadCards', 'Filtered out unavailable cards', {
            filteredCards: filteredCards.map(card => ({
              cardId: card.cardID,
              cardNumber: card.cardNumber,
              status: card.cardStatus
            }))
          });
        }

        logger.info('TransactionForm', 'loadCards', 'Successfully loaded user cards', {
          totalCardCount: cards.length,
          availableCardCount: availableCards.length,
          filteredCardCount: filteredCards.length
        });

        setUserCards(availableCards);

        // If we have a valid cardId in the URL and it exists in the available cards, select it
        if (cardIdNum !== null && cardIdFromUrl !== null) {
          const cardExists = availableCards.some(card => card.cardID === cardIdNum);
          if (cardExists) {
            logger.debug('TransactionForm', 'loadCards', 'Auto-selecting card from URL', {
              cardId: cardIdNum,
              cardIdStr: cardIdFromUrl
            });
            setFormData(prev => ({
              ...prev,
              cardID: cardIdFromUrl! // This is guaranteed to be a string here (we checked it's not null above)
            }));
          } else {
            logger.warn('TransactionForm', 'loadCards', 'Card from URL not found in available cards', {
              cardId: cardIdNum,
              cardIdStr: cardIdFromUrl,
              availableCardIds: availableCards.map(c => c.cardID)
            });
          }
        }
      } catch (error) {
        const err = error instanceof Error ? error : new Error(String(error));
        logger.error('TransactionForm', 'loadCards', 'Failed to load user cards', err);
        setValidationMessage('❌ Failed to load your cards. Please try again.');
      } finally {
        setLoadingCards(false);
      }
    };

    loadUserCards();

    return () => {
      logger.debug('TransactionForm', 'unmount', 'Transaction form page unmounting');
    };
  }, [user, location.pathname]);

  // Real-time validation handlers
  const handleAmountChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = validationUtils.sanitizeAmount(e.target.value);
    setFormData(prev => ({ ...prev, amount: value }));

    const validation = validationUtils.validateAmount(value);
    const error = validation.isValid ? '' : validation.error || '';

    if (error) {
      logger.debug('TransactionForm', 'validation', 'Amount validation failed', {
        value,
        error
      });
    }

    setErrors(prev => ({
      ...prev,
      amount: error
    }));
  };

  const handleTransactionTypeChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value === '' ? '' : Number(e.target.value) as TransactionType;
    setFormData(prev => ({ ...prev, transactionType: value }));

    const validation = value === '' ? { isValid: false, error: 'Transaction type is required' } : validationUtils.validateTransactionType(value);
    const error = validation.isValid ? '' : validation.error || '';

    logger.debug('TransactionForm', 'transactionTypeChange', 'Transaction type changed', {
      transactionType: value,
      transactionTypeName: value !== '' ? getTransactionTypeName(value) : '',
      isValid: validation.isValid,
      error
    });

    setErrors(prev => ({
      ...prev,
      transactionType: error
    }));
  };

  const handleDescriptionChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = validationUtils.sanitizeDescription(e.target.value);
    setFormData(prev => ({ ...prev, description: value }));

    const validation = validationUtils.validateDescription(value);
    const error = validation.isValid ? '' : validation.error || '';

    if (error) {
      logger.debug('TransactionForm', 'validation', 'Description validation failed', {
        description: value,
        error
      });
    }

    setErrors(prev => ({
      ...prev,
      description: error
    }));
  };

  const handleCardIdChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value;
    setFormData(prev => ({ ...prev, cardID: value }));

    // Log card selection
    logger.debug('TransactionForm', 'cardSelection', 'Card selected', {
      cardId: value,
      hasCards: userCards.length > 0
    });

    // Clear card ID error when a card is selected
    if (value) {
      setErrors(prev => ({
        ...prev,
        cardID: ''
      }));
    }
  };

  // Backend validation for amount only
  const validateWithBackend = async () => {
    logger.debug('TransactionForm', 'validation', 'Validating amount with backend', {
      amount: formData.amount
    });

    try {
      const amountValidation = await transactionService.validateAmount(formData.amount);

      if (!amountValidation.isValid) {
        logger.warn('TransactionForm', 'validation', 'Backend validation failed', {
          amount: formData.amount,
          error: amountValidation.message
        });

        setErrors(prev => ({
          ...prev,
          amount: amountValidation.message
        }));
        return false;
      }

      logger.debug('TransactionForm', 'validation', 'Backend validation passed');
      return true;
    } catch (error: any) {
      const err = error instanceof Error ? error : new Error(String(error));
      const errorMessage = `Backend validation error: ${error.response?.data?.message || error.message}`;

      logger.error('TransactionForm', 'validation', 'Error during backend validation', err, {
        amount: formData.amount,
        status: error.response?.status
      });

      setValidationMessage(errorMessage);
      return false;
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setValidationMessage('');

    logger.info('TransactionForm', 'submit', 'Form submission started', {
      hasCardSelected: !!formData.cardID,
      amount: formData.amount,
      transactionType: formData.transactionType
    });

    try {
      // Validate card selection
      if (!formData.cardID) {
        const errorMsg = 'Please select a card';
        logger.warn('TransactionForm', 'validation', 'No card selected');
        setErrors(prev => ({ ...prev, cardID: errorMsg }));
        setValidationMessage(`❌ ${errorMsg}`);
        return;
      }

      // Clear previous errors
      setErrors({});

      // Validate with backend
      const isValid = await validateWithBackend();
      if (!isValid) return;

      // Prepare transaction data with proper types
      const transactionData: CreateTransactionRequest = {
        cardID: parseInt(formData.cardID, 10),
        amount: parseFloat(formData.amount),
        transactionType: formData.transactionType as TransactionType,
        description: formData.description || 'No description provided'
      };

      logger.debug('TransactionForm', 'submit', 'Submitting transaction', {
        ...transactionData,
        amount: transactionData.amount // Keep amount as number for logging
      });

      // Submit transaction
      const result = await transactionService.createTransaction(transactionData);

      if (result.success) {
        logger.info('TransactionForm', 'submit', 'Transaction created successfully', {
          transactionId: result.transaction?.transactionID, // Using transactionID instead of id
          cardId: formData.cardID,
          amount: formData.amount,
          transactionType: formData.transactionType
        });

        setValidationMessage('✅ Transaction created successfully!');

        // Reset form
        setFormData({
          cardID: '',
          amount: '',
          transactionType: '',
          description: ''
        });

        // Redirect to transactions page after a short delay
        setTimeout(() => {
          logger.debug('TransactionForm', 'navigation', 'Redirecting to transactions page');
          navigate('/transactions');
        }, 1500);
      } else {
        const errorMsg = `❌ Transaction failed: ${result.message}`;
        logger.error('TransactionForm', 'submit', 'Transaction creation failed', new Error(result.message), {
          context: {
            cardId: formData.cardID,
            amount: formData.amount,
            transactionType: formData.transactionType
          }
        });
        setValidationMessage(errorMsg);
      }
    } catch (error: any) {
      const err = error instanceof Error ? error : new Error(String(error));
      let errorMsg = `❌ Error: ${error.response?.data?.message || error.message || 'Unknown error occurred'}`;

      if (error.response?.status === 401) {
        errorMsg = '❌ Session expired. Please log in again.';
        logger.warn('TransactionForm', 'auth', 'Session expired during transaction submission', err);

        // Redirect to login after a delay
        setTimeout(() => {
          logger.info('TransactionForm', 'auth', 'Redirecting to login page');
          window.location.href = '/login';
        }, 2000);
      } else {
        logger.error('TransactionForm', 'submit', 'Error during transaction submission', err, {
          context: {
            status: error.response?.status,
            responseData: error.response?.data
          }
        });
      }

      setValidationMessage(errorMsg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancel = () => {
    logger.info('TransactionForm', 'navigation', 'User cancelled transaction, returning to transactions list');
    navigate('/transactions');
  };

  const testAuth = async () => {
    logger.debug('TransactionForm', 'auth', 'Testing authentication status');

    try {
      logger.debug('TransactionForm', 'auth', 'Current authentication cookies', {
        cookies: document.cookie.split('; ').filter(c => c.startsWith('trkart_'))
      });

      const response = await fetch('http://localhost:7037/api/SecureTransaction/test-auth', {
        credentials: 'include' // Include cookies
      });

      const data = await response.json();
      logger.info('TransactionForm', 'auth', 'Authentication test successful', {
        customerId: data.customerId,
        accessTokenPresent: data.accessTokenPresent,
        refreshTokenPresent: data.refreshTokenPresent
      });

      alert(`Auth test: ${data.message} (CustomerID: ${data.customerId}, AccessToken: ${data.accessTokenPresent}, RefreshToken: ${data.refreshTokenPresent})`);
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      logger.error('TransactionForm', 'auth', 'Authentication test failed', err);
      alert('Auth test failed. Check console for details.');
    }
  };

  // Track form errors
  const hasFormErrors = Object.values(errors).some(error => error !== '');

  // Log form state changes
  useEffect(() => {
    logger.debug('TransactionForm', 'render', 'Form state updated', {
      hasCardSelected: !!formData.cardID,
      amount: formData.amount,
      transactionType: formData.transactionType,
      hasValidationErrors: hasFormErrors
    });
  }, [formData, errors, hasFormErrors]);

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
          {!!user?.email && (
            <div className="bg-blue-50 border border-blue-200 text-blue-700 px-4 py-2 rounded mb-6 text-sm">
              <div className="flex items-center justify-between">
                <span>Logged in as: {user.email}</span>
                {!!user.customerID && user.customerID > 0 && (
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
                  Card
                </label>
                {loadingCards && <LoadingSpinner />}
                {!loadingCards && userCards.length === 0 && (
                  <p className="text-red-500 text-sm">No cards found for this customer. Please add a card first.</p>
                )}
                {!loadingCards && userCards.length > 0 && (
                  <select
                    id="cardID"
                    value={formData.cardID}
                    onChange={handleCardIdChange}
                    className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 
                      ${errors.cardID ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
                      }`}
                    required
                  >
                    <option value="">Select a card</option>
                    {userCards.map(card => (
                      <option key={card.cardID} value={card.cardID}>
                        {card.cardNumber} - ₺{card.balance.toFixed(2)}
                      </option>
                    ))}
                  </select>
                )}
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
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 
                    ${errors.amount ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
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
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 
                    ${errors.transactionType ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
                    }`}
                  required
                >
                  <option value="">Select transaction type</option>
                  {getTransactionFormPageTypes().map(type => (
                    <option key={type} value={type}>
                      {getTransactionTypeName(type)}
                    </option>
                  ))}
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
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 
                    ${errors.description ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
                    }`}
                  placeholder="Enter description (letters and numbers only)"
                />
                {errors.description && (
                  <p className="text-red-500 text-sm mt-1">{errors.description}</p>
                )}
              </div>

              {/* Validation Message */}
              {validationMessage && (
                <div className={`p-3 rounded-md 
                  ${validationMessage.includes('✅') ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
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
                  disabled={isSubmitting || hasFormErrors}
                  className={`flex-1 py-2 px-4 rounded-md font-medium ${isSubmitting || hasFormErrors
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
            <h3 className="text-sm font-medium text-blue-900 mb-2">💡 Available Transaction Types</h3>
            <div className="text-sm text-blue-800 space-y-1">
              <p><strong>Load:</strong> Loading money to card (amount will be added)</p>
              <p><strong>Refund:</strong> Refund transaction (amount will be added) - Testing only</p>
              <p><strong>Pay:</strong> Payment transaction (amount will be deducted) - Testing only</p>
            </div>
            <div className="text-xs text-blue-600 mt-2">
              <p>💡 <strong>Note:</strong> Top-Up and Transfer operations are available in their dedicated pages.</p>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default TransactionFormPage; 