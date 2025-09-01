import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import userCardService from '@/services/userCardService';
import { logger } from '@/utils/logger';
import LoadingSpinner from '@/components/LoadingSpinner';
import { CardType, CARD_TYPES } from '../types/cardTypes';

const CardCreationPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [isLoading, setIsLoading] = useState(true);
  const [cardCount, setCardCount] = useState(0);
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [selectedCardType, setSelectedCardType] = useState<CardType | null>(null);
  const [cardName, setCardName] = useState('');

  // Check if user already has 3 cards
  useEffect(() => {
    logger.info('CardCreationPage', 'mount', 'Component mounted', { hasUser: !!user, customerId: user?.customerID });
    
    const checkCardCount = async () => {
      if (!user?.customerID) {
        const errorMsg = 'User information not available. Please log in again.';
        logger.error('CardCreationPage', 'checkCardCount', errorMsg);
        setError(errorMsg);
        setIsLoading(false);
        return;
      }

      try {
        logger.debug('CardCreationPage', 'checkCardCount', 'Fetching user cards', { customerId: user.customerID });
        const cards = await userCardService.getUserCards();
        logger.info('CardCreationPage', 'checkCardCount', `Found ${cards.length} cards for user`, { 
          customerId: user.customerID, 
          cardCount: cards.length 
        });
        setCardCount(cards.length);
      } catch (err) {
        const errorMsg = 'Failed to load your card information.';
        logger.error('CardCreationPage', 'checkCardCount', errorMsg, err as Error, { customerId: user?.customerID });
        setError(errorMsg);
      } finally {
        setIsLoading(false);
      }
    };

    checkCardCount();
    
    return () => {
      logger.debug('CardCreationPage', 'unmount', 'Component unmounting');
    };
  }, [user]);

  const handleCreateCard = async () => {
    if (!user?.customerID) {
      const errorMsg = 'User information not available. Please log in again.';
      logger.error('CardCreationPage', 'handleCreateCard', errorMsg);
      setError(errorMsg);
      return;
    }

    if (selectedCardType === null) {
      setError('Please select a card type.');
      return;
    }

    if (hasMaxCards) {
      const errorMsg = 'You have reached the maximum number of cards (3).';
      logger.warn('CardCreationPage', 'handleCreateCard', errorMsg, { customerId: user.customerID, cardCount });
      setError(errorMsg);
      return;
    }

    logger.info('CardCreationPage', 'handleCreateCard', 'Creating new card', { 
      customerId: user.customerID,
      cardType: selectedCardType,
      cardName: cardName || 'Unnamed Card'
    });
    
    setIsCreating(true);
    setError('');
    setSuccess('');

    try {
      const newCard = await userCardService.createUserCard({
        customerID: user.customerID,
        cardType: selectedCardType,
        cardName: cardName || undefined
      });
      
      const successMsg = 'Card created successfully! It will be activated after first load transaction.';
      logger.info('CardCreationPage', 'handleCreateCard', 'Card created successfully', { 
        customerId: user.customerID, 
        cardId: newCard.cardID,
        cardNumber: newCard.cardNumber,
        cardType: newCard.cardType
      });
      
      setSuccess(successMsg);
      
      // Redirect back to cards page after 2 seconds
      setTimeout(() => {
        logger.debug('CardCreationPage', 'handleCreateCard', 'Redirecting to cards page');
        navigate('/cards');
      }, 2000);
    } catch (err) {
      const errorMsg = 'Failed to create a new card. Please try again.';
      logger.error('CardCreationPage', 'handleCreateCard', errorMsg, err as Error, { 
        customerId: user.customerID,
        cardType: selectedCardType,
        cardName: cardName || 'Unnamed Card'
      });
      setError(errorMsg);
    } finally {
      setIsCreating(false);
    }
  };

  const handleDecline = () => {
    logger.info('CardCreationPage', 'handleDecline', 'User canceled card creation', { hasHistory: location.key !== 'default' });
    
    // Go back to the previous page or to cards page if no history
    if (location.key !== 'default') {
      logger.debug('CardCreationPage', 'handleDecline', 'Navigating back');
      navigate(-1);
    } else {
      logger.debug('CardCreationPage', 'handleDecline', 'Navigating to cards page');
      navigate('/cards');
    }
  };

  if (isLoading) {
    logger.debug('CardCreationPage', 'render', 'Rendering loading spinner');
    return <LoadingSpinner />;
  }

  const hasMaxCards = cardCount >= 3;
  const cardsLeft = 3 - cardCount;

  return (
    <div className="min-h-screen bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-3xl mx-auto bg-white rounded-xl shadow-md overflow-hidden md:max-w-4xl">
        <div className="p-8">
          <div className="text-center mb-8">
            <h1 className="text-2xl font-bold text-gray-900">Create New Card</h1>
            <p className="mt-2 text-sm text-gray-600">
              {!hasMaxCards 
                ? `You can create up to 3 cards. You have ${cardsLeft} card(s) left.`
                : 'You have reached the maximum number of cards.'}
            </p>
          </div>

          {error && (
            <div className="mb-6 p-4 bg-red-50 border-l-4 border-red-500 text-red-700">
              <p>{error}</p>
            </div>
          )}

          {success && (
            <div className="mb-6 p-4 bg-green-50 border-l-4 border-green-500 text-green-700">
              <p>{success}</p>
              <p className="text-sm mt-1">Redirecting you back to cards page...</p>
            </div>
          )}

          <div className="space-y-6">
            {/* Card Type Selection */}
            <div>
              <h2 className="text-lg font-medium text-gray-900 mb-3">Select Card Type</h2>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mt-6">
                {CARD_TYPES.map((type) => (
                  <div 
                    key={type.id}
                    onClick={() => {
                      setSelectedCardType(type.id);
                    }}
                    className={`border-2 rounded-lg p-6 cursor-pointer transition-all duration-200 ${
                      selectedCardType === type.id 
                        ? 'border-blue-500 bg-blue-50' 
                        : 'border-gray-200 hover:border-blue-300'
                    }`}
                  >
                    <div className="flex flex-col items-center text-center">
                      <div className="w-16 h-16 rounded-full bg-gray-100 flex items-center justify-center mb-4">
                        <span className="text-3xl">
                          {type.id === CardType.Standard ? '💳' : 
                           type.id === CardType.Gold ? '💫' : '✨'}
                        </span>
                      </div>
                      <h3 className="text-lg font-semibold text-gray-900">{type.name}</h3>
                      <p className="mt-2 text-sm text-gray-600">{type.description}</p>
                      <p className="mt-2 text-sm font-medium text-gray-900">Limit: {type.limit}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Card Name Input */}
            <div>
              <label htmlFor="cardName" className="block text-sm font-medium text-gray-700 mb-1">
                Card Name (Optional)
              </label>
              <input
                type="text"
                id="cardName"
                value={cardName}
                onChange={(e) => setCardName(e.target.value)}
                placeholder="e.g., Shopping Card, Travel Card, My Gold Card etc"
                className="mt-1 block w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                maxLength={20}
              />
              <p className="mt-1 text-xs text-gray-500">Maximum 20 characters</p>
            </div>
          </div>

          <div className="mt-8">
            <button
              onClick={handleCreateCard}
              disabled={isCreating || hasMaxCards || selectedCardType === null}
              className={`w-full flex justify-center py-3 px-4 border border-transparent rounded-md shadow-sm text-sm font-medium text-white ${
                isCreating || hasMaxCards || selectedCardType === null
                  ? 'bg-gray-400 cursor-not-allowed'
                  : 'bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500'
              }`}
              aria-busy={isCreating}
              aria-disabled={isCreating || hasMaxCards || selectedCardType === null}
            >
              {isCreating ? (
                <>
                  <svg className="animate-spin -ml-1 mr-3 h-5 w-5 text-white" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Creating...
                </>
              ) : hasMaxCards ? (
                'Maximum Cards Reached'
              ) : selectedCardType === null ? (
                'Select a Card Type'
              ) : (
                'Create New Card'
              )}
            </button>
          </div>

          <div className="mt-6 text-center">
            <button
              onClick={handleDecline}
              className="text-sm font-medium text-indigo-600 hover:text-indigo-500"
            >
              &larr; Back to Cards
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CardCreationPage;
