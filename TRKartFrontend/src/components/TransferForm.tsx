import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { validationUtils } from '@/utils/validationUtils';
import { useAuth } from '@/contexts/AuthContext';
import { UserCard } from '@/types';
import userCardService from '@/services/userCardService';
import transferService from '@/services/transferService';
import LoadingSpinner from '@/components/LoadingSpinner';

interface TransferFormProps {
  onSubmit?: (transfer: any) => void;
  initialFromCardId?: string | null;
}

const TransferForm: React.FC<TransferFormProps> = ({ onSubmit, initialFromCardId }) => {
  const { user } = useAuth();
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    senderCardID: initialFromCardId || '',
    recipientCardNumber: '',
    amount: ''
  });

  const [errors, setErrors] = useState<{ [key: string]: string }>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [validationMessage, setValidationMessage] = useState('');
  const [userCards, setUserCards] = useState<UserCard[]>([]);
  const [loadingCards, setLoadingCards] = useState(true);

  // Load user cards and handle initial card selection
  useEffect(() => {
    const loadUserCards = async () => {
      if (!user?.customerID) {
        setLoadingCards(false);
        return;
      }

      try {
        setLoadingCards(true);
        const cards = await userCardService.getUserCards();
        setUserCards(cards);

        // If we have an initial card ID, verify it exists in the user's cards
        if (initialFromCardId) {
          const cardId = parseInt(initialFromCardId, 10);
          if (!isNaN(cardId)) {
            const cardExists = cards.some(card => card.cardID === cardId);
            if (cardExists) {
              setFormData(prev => ({
                ...prev,
                senderCardID: initialFromCardId
              }));
            }
          }
        }
      } catch (error) {
        console.error('Failed to load user cards:', error);
        setValidationMessage('❌ Failed to load your cards. Please try again.');
      } finally {
        setLoadingCards(false);
      }
    };

    loadUserCards();
  }, [user, initialFromCardId]);

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

  const handleSenderCardChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value;
    setFormData(prev => ({ ...prev, senderCardID: value }));
    
    if (value) {
      setErrors(prev => ({
        ...prev,
        senderCardID: ''
      }));
    }
  };

  const handleRecipientCardNumberChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value
      .replace(/\s/g, '') // Remove spaces first (like TransferForm)
      .toUpperCase()
      .replace(/[^A-Z0-9]/g, '') // Remove other non-alphanumeric characters
      .slice(0, 16); // Limit to 16 characters
    setFormData(prev => ({ ...prev, recipientCardNumber: value }));
    
    if (value) {
      setErrors(prev => ({
        ...prev,
        recipientCardNumber: ''
      }));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setValidationMessage('');

    // Validate form
    const newErrors: { [key: string]: string } = {};
    
    if (!formData.senderCardID) {
      newErrors.senderCardID = 'Please select a sender card';
    }
    
    if (!formData.recipientCardNumber) {
      newErrors.recipientCardNumber = 'Please enter recipient card number';
    } else if (formData.recipientCardNumber.length < 8) {
      newErrors.recipientCardNumber = 'Card number must be at least 8 characters';
    }
    
    if (!formData.amount) {
      newErrors.amount = 'Please enter transfer amount';
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      setIsSubmitting(false);
      return;
    }

    try {
      // Create transfer using the transfer service
      const transferData = {
        senderCardID: parseInt(formData.senderCardID),
        recipientCardNumber: formData.recipientCardNumber,
        amount: parseFloat(formData.amount)
      };

      console.log('Sending transfer data:', transferData);
      console.log('Auth token:', localStorage.getItem('token'));

      const result = await transferService.createTransfer(transferData);
      
      console.log('Transfer result:', result);
      
      if (result.success) {
        setValidationMessage('✅ Transfer completed successfully!');
        
        // Reset form
        setFormData({
          senderCardID: '',
          recipientCardNumber: '',
          amount: ''
        });
        setErrors({});
        
        onSubmit?.(result);
        
        // Navigate to transactions page after a short delay to show the success message
        setTimeout(() => {
          navigate('/transactions');
        }, 1500);
      } else {
        setValidationMessage(`❌ Transfer failed: ${result.message}`);
      }
    } catch (error: any) {
      console.error('Transfer error:', error);
      console.error('Error response:', error.response);
      setValidationMessage(`❌ Transfer failed: ${error.message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const hasErrors = Object.values(errors).some(error => error !== '');

  if (loadingCards) {
    return (
      <div className="max-w-md mx-auto p-6 bg-white rounded-lg shadow-md">
        <LoadingSpinner />
        <p className="text-center text-gray-600 mt-4">Loading your cards...</p>
      </div>
    );
  }

  return (
    <div className="max-w-md mx-auto p-6 bg-white rounded-lg shadow-md">
      <h2 className="text-2xl font-bold mb-6 text-gray-800">New Transfer</h2>
      
      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Sender Card Selection */}
        <div>
          <label htmlFor="senderCardID" className="block text-sm font-medium text-gray-700 mb-1">
            Your Card (Sender)
          </label>
          <select
            id="senderCardID"
            value={formData.senderCardID}
            onChange={handleSenderCardChange}
            className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
              errors.senderCardID ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
            }`}
            required
          >
            <option value="">Select your card</option>
            {userCards.map((card) => (
              <option key={card.cardID} value={card.cardID}>
                {card.cardNumber} - Balance: ₺{card.balance}
              </option>
            ))}
          </select>
          {errors.senderCardID && (
            <p className="text-red-500 text-sm mt-1">{errors.senderCardID}</p>
          )}
        </div>

        {/* Recipient Card Number */}
        <div>
          <label htmlFor="recipientCardNumber" className="block text-sm font-medium text-gray-700 mb-1">
            Recipient Card Number
          </label>
          <input
            type="text"
            id="recipientCardNumber"
            value={formData.recipientCardNumber}
            onChange={handleRecipientCardNumberChange}
            className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
              errors.recipientCardNumber ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
            }`}
            placeholder="Enter recipient card number (TRK90 XXXX XXXX XXX)"
            required
          />
          {errors.recipientCardNumber && (
            <p className="text-red-500 text-sm mt-1">{errors.recipientCardNumber}</p>
          )}
        </div>

        {/* Amount */}
        <div>
          <label htmlFor="amount" className="block text-sm font-medium text-gray-700 mb-1">
            Transfer Amount
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

        {/* Validation Message */}
        {validationMessage && (
          <div className={`p-3 rounded-md ${
            validationMessage.includes('✅') ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
          }`}>
            {validationMessage}
          </div>
        )}

        <button
          type="submit"
          disabled={isSubmitting || hasErrors}
          className={`w-full py-2 px-4 rounded-md font-medium ${
            isSubmitting || hasErrors
              ? 'bg-gray-400 cursor-not-allowed'
              : 'bg-blue-600 hover:bg-blue-700'
          } text-white transition-colors`}
        >
          {isSubmitting ? 'Processing Transfer...' : 'Send Transfer'}
        </button>
      </form>

      <div className="mt-4 p-3 bg-blue-50 rounded-md">
        <h3 className="font-medium text-blue-800 mb-2">Transfer Features:</h3>
        <ul className="text-sm text-blue-700 space-y-1">
          <li>• <strong>Select your card</strong> from your available cards</li>
          <li>• <strong>Enter recipient card number</strong> manually</li>
          <li>• <strong>Specify transfer amount</strong> with validation</li>
          <li>• <strong>Linked transactions</strong> for traceability</li>
          <li>• <strong>Real-time validation</strong> and error handling</li>
        </ul>
      </div>
    </div>
  );
};

export default TransferForm; 