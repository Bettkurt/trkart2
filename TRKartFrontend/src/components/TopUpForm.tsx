import React, { useState } from 'react';
import { topUpService } from '@/services/topUpService';
import { TopUpRequest, TopUpResponse, TopUpValidation } from '@/types';

interface TopUpFormProps {
  onSubmit?: (response: TopUpResponse) => void;
  onCancel?: () => void;
}

const TopUpForm: React.FC<TopUpFormProps> = ({ onSubmit, onCancel }) => {
  const [formData, setFormData] = useState<TopUpRequest>({
    targetCardNumber: '',
    amount: 0,
    paymentMethod: '',
    externalRef: '',
    feeAmount: 0,
    note: ''
  });

  const [errors, setErrors] = useState<{ [key: string]: string }>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [validationMessage, setValidationMessage] = useState('');
  const [cardValidation, setCardValidation] = useState<TopUpValidation | null>(null);
  const [showAdvanced, setShowAdvanced] = useState(false);

  const paymentMethods = topUpService.getPaymentMethods();

  // Real-time validation handlers
  const handleCardNumberChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    // Remove spaces and other whitespace, then filter non-alphanumeric (no length limit yet)
    const value = e.target.value
      .replace(/\s/g, '') // Remove spaces first
      .toUpperCase()
      .replace(/[^A-Z0-9]/g, '') // Remove other non-alphanumeric characters
      .slice(0, 16); // Limit to 16 characters
    setFormData(prev => ({ ...prev, targetCardNumber: value }));
    
    // Clear previous validation
    setCardValidation(null);
    setErrors(prev => ({ ...prev, targetCardNumber: '' }));

    if (value.length !== 16) {
      setErrors(prev => ({ ...prev, targetCardNumber: 'Card number must be exactly 16 characters' }));
    } else {
      // Validate card with backend
      validateCard(value);
    }
  };

  const validateCard = async (cardNumber: string) => {
    try {
      const result = await topUpService.checkCardForTopUp(cardNumber);
      if (result.isValid && result.cardInfo) {
        setCardValidation({
          isValid: true,
          message: `Card found - Status: ${result.cardInfo.cardStatus}, 
            Balance: ${topUpService.formatCurrency(result.cardInfo.currentBalance)}`,
          cardNumber: result.cardInfo.cardNumber,
          cardStatus: result.cardInfo.cardStatus,
          currentBalance: result.cardInfo.currentBalance,
          duplicateExternalRef: false
        });
        setErrors(prev => ({ ...prev, targetCardNumber: '' }));
      } else {
        setCardValidation({
          isValid: false,
          message: result.message,
          duplicateExternalRef: false
        });
        setErrors(prev => ({ ...prev, targetCardNumber: result.message }));
      }
    } catch (error) {
      setErrors(prev => ({ ...prev, targetCardNumber: 'Error validating card' }));
    }
  };

  const handleAmountChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = parseFloat(e.target.value) || 0;
    setFormData(prev => ({ ...prev, amount: value }));
    
    const validation = topUpService.validateAmount(value);
    setErrors(prev => ({
      ...prev,
      amount: validation.isValid ? '' : validation.message
    }));

    // Update projected balance if card is validated
    if (cardValidation?.isValid && cardValidation.currentBalance) {
      const netAmount = topUpService.calculateNetAmount(value, formData.feeAmount);
      setCardValidation(prev => prev ? {
        ...prev,
        projectedBalance: cardValidation.currentBalance! + netAmount
      } : null);
    }
  };

  const handleFeeAmountChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = parseFloat(e.target.value) || 0;
    setFormData(prev => ({ ...prev, feeAmount: value }));
    
    const validation = topUpService.validateFeeAmount(formData.amount, value);
    setErrors(prev => ({
      ...prev,
      feeAmount: validation.isValid ? '' : validation.message
    }));
  };

  const handlePaymentMethodChange = (e: React.ChangeEvent<HTMLSelectElement>) => {
    const value = e.target.value;
    setFormData(prev => ({ ...prev, paymentMethod: value }));
    
    if (!value) {
      setErrors(prev => ({ ...prev, paymentMethod: 'Payment method is required' }));
    } else {
      setErrors(prev => ({ ...prev, paymentMethod: '' }));
    }
  };

  const handleExternalRefChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value.replace(/[^a-zA-Z0-9\-_]/g, '').slice(0, 100);
    setFormData(prev => ({ ...prev, externalRef: value }));
  };

  const handleNoteChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const value = e.target.value.slice(0, 500);
    setFormData(prev => ({ ...prev, note: value }));
  };

  const generateExternalRef = () => {
    const ref = topUpService.generateExternalRef();
    setFormData(prev => ({ ...prev, externalRef: ref }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setValidationMessage('');

    // Prepare form data - don't send empty ExternalRef to avoid constraint issues
    const submitData = {
      ...formData,
      externalRef: formData.externalRef?.trim() || undefined // Send undefined instead of empty string
    };

    try {
      // Final validation
      if (!cardValidation?.isValid) {
        setValidationMessage('❌ Please enter a valid and active card number');
        setIsSubmitting(false);
        return;
      }

      const amountValidation = topUpService.validateAmount(formData.amount);
      if (!amountValidation.isValid) {
        setValidationMessage(`❌ ${amountValidation.message}`);
        setIsSubmitting(false);
        return;
      }

      if (formData.feeAmount) {
        const feeValidation = topUpService.validateFeeAmount(formData.amount, formData.feeAmount);
        if (!feeValidation.isValid) {
          setValidationMessage(`❌ ${feeValidation.message}`);
          setIsSubmitting(false);
          return;
        }
      }

      // Submit top-up request with retry logic for duplicate ExternalRef
      let result;
      let retryCount = 0;
      const maxRetries = 3;

      while (retryCount <= maxRetries) {
        try {
          result = await topUpService.topUp(submitData);
          break; // Success, exit retry loop
        } catch (error: any) {
          // Check if it's a duplicate ExternalRef error
          if (error.message && error.message.includes('duplicate') && retryCount < maxRetries) {
            console.log(`Retry ${retryCount + 1}: Duplicate ExternalRef detected, generating new one...`);
            const newRef = topUpService.generateExternalRef();
            submitData.externalRef = newRef;
            setFormData(prev => ({ ...prev, externalRef: newRef }));
            retryCount++;
            continue; // Retry with new ExternalRef
          }
          throw error; // Re-throw if not a duplicate error or max retries reached
        }
      }
      
      if (result && result.success) {
        setValidationMessage('✅ Top-up completed successfully!');
        onSubmit?.(result);
        
        // Reset form
        setFormData({
          targetCardNumber: '',
          amount: 0,
          paymentMethod: '',
          externalRef: '',
          feeAmount: 0,
          note: ''
        });
        setErrors({});
        setCardValidation(null);
      } else if (result) {
        setValidationMessage(`❌ Top-up failed: ${result.message}`);
      } else {
        setValidationMessage('❌ Top-up failed: No response from server');
      }
    } catch (error: any) {
      console.error('Top-up submission error:', error);
      setValidationMessage(`❌ Error: ${error.message || 'An unexpected error occurred'}`);
    } finally {
      setIsSubmitting(false);
    }
  };

  const hasErrors = Object.values(errors).some(error => error !== '') || !cardValidation?.isValid;
  const netAmount = topUpService.calculateNetAmount(formData.amount, formData.feeAmount);

  return (
    <div className="max-w-2xl mx-auto p-6 bg-white rounded-lg shadow-md">
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-bold text-gray-800">Top-Up Card</h2>
        {onCancel && (
          <button
            onClick={onCancel}
            className="text-gray-500 hover:text-gray-700 text-xl"
          >
            ×
          </button>
        )}
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        {/* Card Number */}
        <div>
          <label htmlFor="targetCardNumber" className="block text-sm font-medium text-gray-700 mb-1">
            Target Card Number
          </label>
          <input
            type="text"
            id="targetCardNumber"
            value={formData.targetCardNumber}
            onChange={handleCardNumberChange}
            className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
              errors.targetCardNumber ? 'border-red-500 focus:ring-red-500' : 
              cardValidation?.isValid ? 'border-green-500 focus:ring-green-500' :
              'border-gray-300 focus:ring-blue-500'
            }`}
            placeholder="Enter card number (TRK90 XXXX XXXX XXX)"
            required
          />
          {errors.targetCardNumber && (
            <p className="text-red-500 text-sm mt-1">{errors.targetCardNumber}</p>
          )}
          {cardValidation?.isValid && (
            <p className="text-green-600 text-sm mt-1">{cardValidation.message}</p>
          )}
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* Amount */}
          <div>
            <label htmlFor="amount" className="block text-sm font-medium text-gray-700 mb-1">
              Amount ($)
            </label>
            <input
              type="number"
              id="amount"
              value={formData.amount || ''}
              onChange={handleAmountChange}
              min="10"
              max="10000"
              step="0.01"
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
                errors.amount ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
              }`}
              placeholder="10.00 - 10,000.00"
            />
            {errors.amount && (
              <p className="text-red-500 text-sm mt-1">{errors.amount}</p>
            )}
            <p className="text-gray-500 text-xs mt-1">Min: $10.00, Max: $10,000.00</p>
          </div>

          {/* Payment Method */}
          <div>
            <label htmlFor="paymentMethod" className="block text-sm font-medium text-gray-700 mb-1">
              Payment Method
            </label>
            <select
              id="paymentMethod"
              value={formData.paymentMethod}
              onChange={handlePaymentMethodChange}
              className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
                errors.paymentMethod ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
              }`}
            >
              <option value="">Select payment method</option>
              {paymentMethods.map(method => (
                <option key={method.value} value={method.value}>
                  {method.label}
                </option>
              ))}
            </select>
            {errors.paymentMethod && (
              <p className="text-red-500 text-sm mt-1">{errors.paymentMethod}</p>
            )}
          </div>
        </div>

        {/* Advanced Options Toggle */}
        <div>
          <button
            type="button"
            onClick={() => setShowAdvanced(!showAdvanced)}
            className="text-blue-600 hover:text-blue-800 text-sm font-medium"
          >
            {showAdvanced ? '▼' : '▶'} Advanced Options
          </button>
        </div>

        {showAdvanced && (
          <div className="space-y-4 p-4 bg-gray-50 rounded-md">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Fee Amount */}
              <div>
                <label htmlFor="feeAmount" className="block text-sm font-medium text-gray-700 mb-1">
                  Fee Amount ($)
                </label>
                <input
                  type="number"
                  id="feeAmount"
                  value={formData.feeAmount || ''}
                  onChange={handleFeeAmountChange}
                  min="0"
                  step="0.01"
                  className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 ${
                    errors.feeAmount ? 'border-red-500 focus:ring-red-500' : 'border-gray-300 focus:ring-blue-500'
                  }`}
                  placeholder="0.00"
                />
                {errors.feeAmount && (
                  <p className="text-red-500 text-sm mt-1">{errors.feeAmount}</p>
                )}
                <p className="text-gray-500 text-xs mt-1">Optional processing fee</p>
              </div>

              {/* External Reference */}
              <div>
                <label htmlFor="externalRef" className="block text-sm font-medium text-gray-700 mb-1">
                  External Reference
                </label>
                <div className="flex">
                  <input
                    type="text"
                    id="externalRef"
                    value={formData.externalRef || ''}
                    onChange={handleExternalRefChange}
                    className="flex-1 px-3 py-2 border border-r-0 rounded-l-md focus:outline-none focus:ring-2 focus:ring-blue-500 border-gray-300"
                    placeholder="Optional transaction ID"
                    maxLength={100}
                  />
                  <button
                    type="button"
                    onClick={generateExternalRef}
                    className="px-3 py-2 bg-gray-100 border border-l-0 rounded-r-md hover:bg-gray-200 text-sm"
                  >
                    Generate
                  </button>
                </div>
                <p className="text-gray-500 text-xs mt-1">For payment provider tracking</p>
              </div>
            </div>

            {/* Note */}
            <div>
              <label htmlFor="note" className="block text-sm font-medium text-gray-700 mb-1">
                Note
              </label>
              <textarea
                id="note"
                value={formData.note || ''}
                onChange={handleNoteChange}
                rows={3}
                className="w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 border-gray-300"
                placeholder="Optional note about this top-up"
                maxLength={500}
              />
              <p className="text-gray-500 text-xs mt-1">{(formData.note?.length || 0)}/500 characters</p>
            </div>
          </div>
        )}

        {/* Summary */}
        {formData.amount > 0 && (
          <div className="p-4 bg-blue-50 rounded-md">
            <h3 className="font-medium text-blue-800 mb-2">Top-Up Summary</h3>
            <div className="text-sm text-blue-700 space-y-1">
              <div className="flex justify-between">
                <span>Gross Amount:</span>
                <span>{topUpService.formatCurrency(formData.amount)}</span>
              </div>
              {formData.feeAmount && formData.feeAmount > 0 && (
                <div className="flex justify-between">
                  <span>Fee:</span>
                  <span>-{topUpService.formatCurrency(formData.feeAmount)}</span>
                </div>
              )}
              <div className="flex justify-between font-medium border-t pt-1">
                <span>Net Amount to Card:</span>
                <span>{topUpService.formatCurrency(netAmount)}</span>
              </div>
              {cardValidation?.currentBalance && (
                <div className="flex justify-between">
                  <span>New Balance:</span>
                  <span>{topUpService.formatCurrency(cardValidation.currentBalance + netAmount)}</span>
                </div>
              )}
            </div>
          </div>
        )}

        {validationMessage && (
          <div className={`p-3 rounded-md ${
            validationMessage.includes('✅') ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'
          }`}>
            {validationMessage}
          </div>
        )}

        <div className="flex space-x-4">
          <button
            type="submit"
            disabled={isSubmitting || hasErrors || !formData.paymentMethod}
            className={`flex-1 py-3 px-4 rounded-md font-medium ${
              isSubmitting || hasErrors || !formData.paymentMethod
                ? 'bg-gray-400 cursor-not-allowed'
                : 'bg-blue-600 hover:bg-blue-700'
            } text-white transition-colors`}
          >
            {isSubmitting ? 'Processing...' : `Top-Up ${topUpService.formatCurrency(netAmount)}`}
          </button>
          
          {onCancel && (
            <button
              type="button"
              onClick={onCancel}
              className="px-6 py-3 border border-gray-300 rounded-md text-gray-700 hover:bg-gray-50 transition-colors"
            >
              Cancel
            </button>
          )}
        </div>
      </form>

      <div className="mt-6 p-3 bg-yellow-50 rounded-md">
        <h3 className="font-medium text-yellow-800 mb-2">Top-Up Features:</h3>
        <ul className="text-sm text-yellow-700 space-y-1">
          <li>• <strong>Secure:</strong> Only active cards can be topped up</li>
          <li>• <strong>Limits:</strong> $10 - $10,000 per transaction</li>
          <li>• <strong>Fees:</strong> Optional processing fees are deducted from gross amount</li>
          <li>• <strong>Tracking:</strong> External reference for payment provider integration</li>
          <li>• <strong>Authorization:</strong> You can only top up your own cards</li>
        </ul>
      </div>
    </div>
  );
};

export default TopUpForm;
