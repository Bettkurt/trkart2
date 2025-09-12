import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import userCardService from '@/services/userCardService';
import { UserCard } from '@/types';
import LoadingSpinner from '@/components/LoadingSpinner';
import { logger } from '@/utils/logger';
import CardItem from '@/components/cards/CardItem';

const NewUserCardsPage: React.FC = () => {
  const { user } = useAuth();
  const [cards, setCards] = useState<UserCard[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  // Helper function to save cards to localStorage
  const saveCardsToStorage = (cards: UserCard[]) => {
    const userKey = user?.email ? `trkart_cards_${user.email}` : 'trkart_cards_anonymous';
    try {
      localStorage.setItem(userKey, JSON.stringify(cards));
      logger.info('NewUserCardsPage', 'saveCardsToStorage', `Saved ${cards.length} cards to localStorage`, { userKey });
    } catch (error) {
      logger.error('NewUserCardsPage', 'saveCardsToStorage', 'Failed to save cards to localStorage', error as Error, { userKey });
    }
  };

  // Helper function to load cards from localStorage
  const loadCardsFromStorage = (): UserCard[] => {
    const userKey = user?.email ? `trkart_cards_${user.email}` : 'trkart_cards_anonymous';
    try {
      const stored = localStorage.getItem(userKey);
      const cards = stored ? JSON.parse(stored) : [];
      logger.debug('NewUserCardsPage', 'loadCardsFromStorage', `Loaded ${cards.length} cards from localStorage`, { userKey });
      return cards;
    } catch (error) {
      logger.error('NewUserCardsPage', 'loadCardsFromStorage', 'Failed to load cards from localStorage', error as Error, { userKey });
      return [];
    }
  };

  useEffect(() => {
    logger.info('NewUserCardsPage', 'mount', 'Component mounted', { userId: user?.customerID });
    
    const fetchCards = async () => {
      if (!user?.email) {
        logger.debug('NewUserCardsPage', 'fetchCards', 'No user email found, showing empty state');
        setCards([]);
        setIsLoading(false);
        return;
      }

      try {
        let cardsData: UserCard[] = [];
        
        // Try to fetch from API first if we have a customerID
        if (user?.customerID) {
          try {
            logger.debug('NewUserCardsPage', 'fetchCards', 'Fetching cards from secure API');
            cardsData = await userCardService.getUserCards();
            logger.info('NewUserCardsPage', 'fetchCards', `Fetched ${cardsData.length} cards from secure API`);
            saveCardsToStorage(cardsData);
          } catch (err) {
            logger.error('NewUserCardsPage', 'fetchCards', 'Failed to load cards from API', err as Error);
            // Fall back to localStorage if API fails
            const storedCards = loadCardsFromStorage();
            if (storedCards.length > 0) {
              logger.info('NewUserCardsPage', 'fetchCards', 'Using cached cards from localStorage');
              cardsData = storedCards;
            } else {
              setError('Failed to load cards. Using cached data if available.');
            }
          }
        } else {
          // If no customerID, try to load from localStorage
          const storedCards = loadCardsFromStorage();
          if (storedCards.length > 0) {
            logger.info('NewUserCardsPage', 'fetchCards', 'Using cards from localStorage');
            cardsData = storedCards;
          }
        }
        
        // Sort cards: Active (4) → Inactive (3) → Lost (2), then by cardID
        const sortedCards = [...cardsData].sort((a, b) => {
          if (a.cardStatus !== b.cardStatus) {
            return b.cardStatus - a.cardStatus;
          }
          return a.cardID - b.cardID;
        });
        
        setCards(sortedCards);
      } catch (err) {
        const errorMsg = 'An unexpected error occurred while loading cards.';
        logger.error('NewUserCardsPage', 'fetchCards', errorMsg, err as Error);
        setError(errorMsg);
      } finally {
        setIsLoading(false);
      }
    };

    fetchCards();
    
    return () => {
      logger.debug('NewUserCardsPage', 'unmount', 'Component unmounting');
    };
  }, [user]);

  const handleNameEdit = async (cardId: number, newName: string) => {
    try {
      // Validate input length
      if (newName.length > 16) {
        throw new Error('Card name cannot exceed 16 characters');
      }

      // Call the API to update the card name
      const response = await userCardService.updateCardName(cardId, newName);
      
      if (response.success) {
        // Update the local state with the new name
        setCards(prevCards => 
          prevCards.map(card => 
            card.cardID === cardId 
              ? { ...card, cardName: newName }
              : card
          )
        );
        
        // Save updated cards to localStorage as backup
        const updatedCards = cards.map(card => 
          card.cardID === cardId 
            ? { ...card, cardName: newName }
            : card
        );
        saveCardsToStorage(updatedCards);
        
        logger.info('NewUserCardsPage', 'handleNameEdit', 'Card name updated successfully', { cardId, newName });
        
        // Return success to trigger success message in CardItem
        return Promise.resolve();
      } else {
        // Provide user-friendly error message
        let errorMessage = 'Failed to update card name';
        if (response.message) {
          // Map technical error messages to user-friendly ones
          if (response.message.includes('validation')) {
            errorMessage = 'Card name cannot exceed 16 characters';
          } else if (response.message.includes('not found')) {
            errorMessage = 'Card not found or access denied';
          } else if (response.message.includes('unauthorized')) {
            errorMessage = 'Please log in again to continue';
          } else {
            errorMessage = 'Unable to save card name. Please try again.';
          }
        }
        throw new Error(errorMessage);
      }
    } catch (error) {
      logger.error('NewUserCardsPage', 'handleNameEdit', 'Error updating card name', error as Error, { cardId, newName });
      
      // Provide user-friendly error messages for common scenarios
      let userMessage = 'Failed to update card name';
      if (error instanceof Error) {
        if (error.message.includes('network') || error.message.includes('fetch')) {
          userMessage = 'Network error. Please check your connection and try again.';
        } else if (error.message.includes('timeout')) {
          userMessage = 'Request timed out. Please try again.';
        } else if (error.message.includes('16 characters')) {
          userMessage = 'Card name cannot exceed 16 characters';
        } else {
          userMessage = error.message;
        }
      }
      
      // Re-throw with user-friendly message
      throw new Error(userMessage);
    }
  };

  if (isLoading) {
    return <LoadingSpinner />;
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
              <h1 className="text-xl font-semibold text-gray-900">My Cards</h1>
            </div>
            <div className="flex items-center">
              <Link to="/create-card" className="btn-primary">
                Add New Card
              </Link>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          {error && (
            <div className="bg-red-100 border border-red-400 text-red-700 px-4 py-3 rounded mb-4">
              {error}
            </div>
          )}

          {cards.length === 0 ? (
            <div className="text-center py-12">
              <h3 className="text-lg font-medium text-gray-900 mb-2">No cards found</h3>
              <p className="text-gray-600">You haven't added any cards yet.</p>
              <Link to="/create-card" className="btn-primary mt-4 inline-block">Add Your First Card</Link>
            </div>
          ) : (
            <div className="flex flex-wrap gap-x-24 gap-y-32 p-8 max-w-7xl mx-auto">
              {cards.map((card) => (
                <div key={card.cardID} className="w-80">
                  <CardItem 
                    card={card} 
                    onNameEdit={handleNameEdit} 
                  />
                </div>
              ))}
            </div>
          )}
        </div>
      </main>
    </div>
  );
};

export default NewUserCardsPage;
