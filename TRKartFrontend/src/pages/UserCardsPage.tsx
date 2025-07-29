import React, { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/contexts/AuthContext';
import userCardService from '@/services/userCardService';
import { UserCard } from '@/types';
import LoadingSpinner from '@/components/LoadingSpinner';

const UserCardsPage: React.FC = () => {
  const { user } = useAuth();
  const [cards, setCards] = useState<UserCard[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchCards = async () => {
      if (!user?.customerID) return;
      
      try {
        const data = await userCardService.getCardsByCustomerId(user.customerID);
        setCards(data);
      } catch (err) {
        setError('Failed to load cards');
      } finally {
        setIsLoading(false);
      }
    };

    fetchCards();
  }, [user]);

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
              <button className="btn-primary">
                Add New Card
              </button>
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
              <button className="btn-primary mt-4">Add Your First Card</button>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              {cards.map((card) => (
                <div key={card.cardID} className="card">
                  <div className="flex items-center justify-between mb-4">
                    <h3 className="text-lg font-medium text-gray-900">Card #{card.cardID}</h3>
                    <div className={`px-2 py-1 text-xs font-medium rounded-full ${
                      card.status === 'Active'
                        ? 'bg-green-100 text-green-800'
                        : 'bg-red-100 text-red-800'
                    }`}>
                      {card.status}
                    </div>
                  </div>
                  
                  <div className="space-y-3">
                    <div>
                      <label className="text-sm font-medium text-gray-500">Card Number</label>
                      <p className="text-sm text-gray-900 font-mono">
                        {card.cardNumber}
                      </p>
                    </div>
                    
                    <div>
                      <label className="text-sm font-medium text-gray-500">Balance</label>
                      <p className="text-lg font-semibold text-gray-900">
                        {card.balance.toFixed(2)} TL
                      </p>
                    </div>
                    
                    <div>
                      <label className="text-sm font-medium text-gray-500">Created</label>
                      <p className="text-sm text-gray-900">
                        {new Date(card.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                  </div>
                  
                  <div className="mt-6 flex space-x-2">
                    <button className="btn-primary flex-1">View Details</button>
                    <button className="btn-secondary">Delete</button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </main>
    </div>
  );
};

export default UserCardsPage; 