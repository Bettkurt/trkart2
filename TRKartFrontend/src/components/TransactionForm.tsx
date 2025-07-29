import React, { useState } from 'react';
import { validationUtils } from '@/utils/validationUtils';
import transactionService from '@/services/transactionService';

interface TransactionFormProps {
  onSubmit?: (transaction: any) => void;
}

const TransactionForm: React.FC<TransactionFormProps> = ({ onSubmit }) => {
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
      // Only validate amount with backend
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
      setValidationMessage(`❌ Backend validation error: ${error.response?.data?.message || error.message}`);
      return false;
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setValidationMessage('');

    try {
      // First validate with backend (amount only)
      const backendValid = await validateWithBackend();
      
      if (!backendValid) {
        setIsSubmitting(false);
        return;
      }

      // If backend validation passes, proceed with transaction
      const transactionData = {
        cardID: parseInt(formData.cardID),
        amount: parseFloat(formData.amount),
        transactionType: formData.transactionType,
        description: formData.description
      };

      const result = await transactionService.createTransaction(transactionData);
      
      if (result.success) {
        setValidationMessage('✅ Transaction created successfully!');
        onSubmit?.(result.transaction);
        // Reset form
        setFormData({
          cardID: '',
          amount: '',
          transactionType: '',
          description: ''
        });
        setErrors({});
      } else {
        setValidationMessage(`❌ Transaction failed: ${result.message}`);
      }
    } catch (error: any) {
      setValidationMessage(`❌ Error: ${error.response?.data?.message || error.message}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const hasErrors = Object.values(errors).some(error => error !== '');

  return (
    <div className="max-w-md mx-auto p-6 bg-white rounded-lg shadow-md">
      <h2 className="text-2xl font-bold mb-6 text-gray-800">Create Transaction</h2>
      
      <form onSubmit={handleSubmit} className="space-y-4">
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
          >
            <option value="">Select transaction type</option>
            <option value="Pay">Pay</option>
            <option value="Load">Load</option>
            <option value="Refund">Refund</option>
            <option value="TransferIn">TransferIn</option>
            <option value="TransferOut">TransferOut</option>
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
          />
          {errors.description && (
            <p className="text-red-500 text-sm mt-1">{errors.description}</p>
          )}
        </div>

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
          {isSubmitting ? 'Processing...' : 'Create Transaction'}
        </button>
      </form>

      <div className="mt-4 p-3 bg-blue-50 rounded-md">
        <h3 className="font-medium text-blue-800 mb-2">Updated Validation Features:</h3>
        <ul className="text-sm text-blue-700 space-y-1">
          <li>• <strong>Frontend validation</strong> for transaction type and description</li>
          <li>• <strong>Backend validation</strong> for amount (positive number check)</li>
          <li>• <strong>Real-time input sanitization</strong> and validation</li>
          <li>• <strong>New transaction types</strong>: Pay, Load, Refund, TransferIn, TransferOut</li>
          <li>• <strong>Strict description rules</strong>: letters and numbers only</li>
        </ul>
      </div>
    </div>
  );
};

export default TransactionForm; 