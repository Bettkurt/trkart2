import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import userCardService from '@/services/userCardService';
import { UserCard } from '@/types';
import { CardStatus } from '@/types/cardStatus';

const CustomerCardsPage: React.FC = () => {
  const { customerId } = useParams<{ customerId: string }>();
  const navigate = useNavigate();
  const [cards, setCards] = useState<UserCard[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchCustomerCards = async () => {
      try {
        setLoading(true);
        // In a real app, you might want to add permission checks here
        const customerCards = await userCardService.getCustomerCards(Number(customerId));
        setCards(customerCards);
      } catch (err) {
        console.error('Error fetching customer cards:', err);
        setError('Failed to load customer cards');
      } finally {
        setLoading(false);
      }
    };

    if (customerId) {
      fetchCustomerCards();
    }
  }, [customerId]);

  const getStatusBadge = (status: CardStatus) => {
    switch (status) {
      case CardStatus.Active:
        return 'bg-green-100 text-green-800';
      case CardStatus.Inactive:
        return 'bg-yellow-100 text-yellow-800';
      case CardStatus.Expired:
        return 'bg-red-100 text-red-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  const formatDate = (dateString?: string) => {
    if (!dateString) return 'N/A';
    return new Date(dateString).toLocaleDateString();
  };

  if (loading) {
    return (
      <div className="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
        <div className="max-w-4xl mx-auto">
          <div className="text-center py-12">
            <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-500 mx-auto"></div>
            <p className="mt-4 text-gray-600">Loading cards...</p>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
        <div className="max-w-4xl mx-auto">
          <div className="bg-red-50 border-l-4 border-red-400 p-4 mb-6">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-red-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <p className="text-sm text-red-700">{error}</p>
              </div>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 py-8 px-4 sm:px-6 lg:px-8">
      <div className="max-w-4xl mx-auto">
        <div className="mb-6">
          <button
            onClick={() => navigate(-1)}
            className="flex items-center text-blue-600 hover:text-blue-800 mb-4"
          >
            <svg className="w-5 h-5 mr-1" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            Back to Previous Page
          </button>
          <h1 className="text-2xl font-bold text-gray-900">Customer Cards</h1>
          <p className="mt-1 text-sm text-gray-500">Customer ID: {customerId}</p>
        </div>

        <div className="bg-white shadow overflow-hidden sm:rounded-md">
          {cards.length === 0 ? (
            <div className="text-center py-12">
              <svg
                className="mx-auto h-12 w-12 text-gray-400"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                aria-hidden="true"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={1}
                  d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2"
                />
              </svg>
              <h3 className="mt-2 text-sm font-medium text-gray-900">No cards found</h3>
              <p className="mt-1 text-sm text-gray-500">This customer doesn't have any cards yet.</p>
            </div>
          ) : (
            <ul className="divide-y divide-gray-200">
              {cards.map((card) => (
                <li key={card.cardID}>
                  <div className="px-4 py-4 sm:px-6">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center">
                        <div className="text-sm font-medium text-blue-600 truncate">
                          {card.cardNumber ? `•••• •••• •••• ${card.cardNumber.slice(-4)}` : 'Card ••••'}
                        </div>
                        <div className="ml-2">
                          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusBadge(card.cardStatus)}`}>
                            {CardStatus[card.cardStatus]}
                          </span>
                        </div>
                      </div>
                      <div className="ml-2 flex-shrink-0 flex">
                        <span className="px-2 inline-flex text-xs leading-5 font-semibold rounded-full bg-blue-100 text-blue-800">
                          {card.cardType}
                        </span>
                      </div>
                    </div>
                    <div className="mt-2 sm:flex sm:justify-between">
                      <div className="sm:flex">
                        <p className="text-sm text-gray-500">Expires: {formatDate(card.cardExpirationDate)}</p>
                      </div>
                      <div className="mt-2 flex items-center text-sm text-gray-500 sm:mt-0">
                        {/* Creation date not available in UserCard type */}
                      </div>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>
    </div>
  );
};

export default CustomerCardsPage;
