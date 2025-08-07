import React, { useState, useEffect } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import userCardService from '@/services/userCardService';
import LoadingSpinner from '@/components/LoadingSpinner';

const AddCardPage: React.FC = () => {
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
    const checkCardCount = async () => {
      if (!user?.customerID) {
        setError('User information not available. Please log in again.');
        setIsLoading(false);
        return;
      }

      try {
        const cards = await userCardService.getCardsByCustomerId(user.customerID);
        setCardCount(cards.length);
      } catch (err) {
        console.error('Failed to fetch cards:', err);
        setError('Failed to load your card information.');
      } finally {
        setIsLoading(false);
      }
    };

    checkCardCount();
  }, [user]);

  const handleCreateCard = async () => {
    if (!user?.customerID) {
      setError('User information not available. Please log in again.');
      return;
    }

    if (cardCount >= 3) {
      setError('You have reached the maximum number of cards (3).');
      return;
    }

    setIsCreating(true);
    setError('');
    setSuccess('');

    try {
      await userCardService.createUserCard({
        customerID: user.customerID
      });
      
      setSuccess('Card created successfully! It will be activated after first load transaction.');
      // Redirect back to cards page after 2 seconds
      setTimeout(() => {
        navigate('/cards');
      }, 2000);
    } catch (err) {
      console.error('Failed to create card:', err);
      setError('Failed to create a new card. Please try again.');
    } finally {
      setIsCreating(false);
    }
  };

  const handleDecline = () => {
    // Go back to the previous page or to cards page if no history
    if (location.key !== 'default') {
      navigate(-1);
    } else {
      navigate('/cards');
    }
  };

  if (isLoading) {
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
                  onClick={() => navigate('/cards')}
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

export default AddCardPage;
