import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import userCardService from '@/services/userCardService';
import { logger } from '@/utils/logger';
import LoadingSpinner from '@/components/LoadingSpinner';

const CardCreationPage: React.FC = () => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [isLoading, setIsLoading] = useState(true);
  const [cardCount, setCardCount] = useState(0);
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

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
        const cards = await userCardService.getCardsByCustomerId(user.customerID);
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

    if (cardCount >= 3) {
      const errorMsg = 'You have reached the maximum number of cards (3).';
      logger.warn('CardCreationPage', 'handleCreateCard', errorMsg, { customerId: user.customerID, cardCount });
      setError(errorMsg);
      return;
    }

    logger.info('CardCreationPage', 'handleCreateCard', 'Creating new card', { customerId: user.customerID });
    
    setIsCreating(true);
    setError('');
    setSuccess('');

    try {
      const newCard = await userCardService.createUserCard({
        customerID: user.customerID
      });
      
      const successMsg = 'Card created successfully! It will be activated after first load transaction.';
      logger.info('CardCreationPage', 'handleCreateCard', 'Card created successfully', { 
        customerId: user.customerID, 
        cardId: newCard.cardID,
        cardNumber: newCard.cardNumber
      });
      
      setSuccess(successMsg);
      
      // Redirect back to cards page after 2 seconds
      setTimeout(() => {
        logger.debug('CardCreationPage', 'handleCreateCard', 'Redirecting to cards page');
        navigate('/cards');
      }, 2000);
    } catch (err) {
      const errorMsg = 'Failed to create a new card. Please try again.';
      logger.error('CardCreationPage', 'handleCreateCard', errorMsg, err as Error, { customerId: user.customerID });
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

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <button 
                onClick={handleDecline}
                className="text-gray-600 hover:text-gray-900 mr-4 flex items-center"
              >
                ← Back
              </button>
              <h1 className="text-xl font-semibold text-gray-900">Add New Card</h1>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-3xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          {error && (
            <div className="bg-red-100 border-l-4 border-red-500 text-red-700 p-4 mb-6 rounded">
              <p>{error}</p>
            </div>
          )}

          {success ? (
            <div className="bg-green-100 border-l-4 border-green-500 text-green-700 p-4 mb-6 rounded">
              <p>{success}</p>
              <p className="mt-2">Redirecting to cards page...</p>
            </div>
          ) : hasMaxCards ? (
            <div className="bg-yellow-100 border-l-4 border-yellow-500 text-yellow-700 p-4 mb-6 rounded">
              <h3 className="font-bold">Maximum Cards Reached</h3>
              <p className="mt-2">
                You already have {cardCount} cards. The maximum number of cards per user is 3. 
                Please delete an existing card before adding a new one.
              </p>
              <div className="mt-4">
                <button 
                  onClick={() => {
                    logger.info('CardCreationPage', 'maxCardsRedirect', 'User clicked Go to My Cards button');
                    navigate('/cards');
                  }}
                  className="btn-primary"
                >
                  Go to My Cards
                </button>
              </div>
            </div>
          ) : (
            <div className="bg-white shadow overflow-hidden sm:rounded-lg">
              <div className="px-4 py-5 sm:px-6">
                <h3 className="text-lg leading-6 font-medium text-gray-900">Add a New Card</h3>
                <p className="mt-1 max-w-2xl text-sm text-gray-500">
                  Create a new virtual card for your account
                </p>
              </div>
              <div className="border-t border-gray-200 px-4 py-5 sm:p-0">
                <dl className="sm:divide-y sm:divide-gray-200">
                  <div className="py-4 sm:py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
                    <dt className="text-sm font-medium text-gray-500">Card Status</dt>
                    <dd className="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                      <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-yellow-100 text-yellow-800">
                        Inactive
                      </span>
                      <p className="mt-1 text-sm text-gray-500">
                        Your card will be activated after the first load transaction.
                      </p>
                    </dd>
                  </div>
                  <div className="py-4 sm:py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
                    <dt className="text-sm font-medium text-gray-500">Initial Balance</dt>
                    <dd className="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                      0.00 TL
                      <p className="mt-1 text-sm text-gray-500">
                        You can load money to your card after creation.
                      </p>
                    </dd>
                  </div>
                  <div className="py-4 sm:py-5 sm:grid sm:grid-cols-3 sm:gap-4 sm:px-6">
                    <dt className="text-sm font-medium text-gray-500">Terms & Conditions</dt>
                    <dd className="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                      <ul className="list-disc pl-5 space-y-1">
                        <li>New cards are inactive until the first load transaction</li>
                        <li>You can have a maximum of 3 cards</li>
                        <li>There are no fees for card creation</li>
                        <li>Cards can be used for online and in-store purchases</li>
                      </ul>
                    </dd>
                  </div>
                </dl>
              </div>
              <div className="px-4 py-4 bg-gray-50 sm:px-6 flex justify-end space-x-3">
                <button
                  type="button"
                  onClick={handleDecline}
                  className="btn-secondary"
                  disabled={isCreating}
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleCreateCard}
                  className="btn-primary"
                  disabled={isCreating || hasMaxCards}
                >
                  {isCreating ? 'Creating...' : 'Create Card'}
                </button>
              </div>
            </div>
          )}
        </div>
      </main>
    </div>
  );
};

export default CardCreationPage;
