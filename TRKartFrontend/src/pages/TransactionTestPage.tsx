import React from 'react';
import TransactionForm from '@/components/TransactionForm';

const TransactionTestPage: React.FC = () => {
  const handleTransactionSubmit = (transaction: any) => {
    console.log('Transaction created:', transaction);
  };

  return (
    <div className="min-h-screen bg-gray-50 py-8">
      <div className="max-w-4xl mx-auto px-4">
        <div className="text-center mb-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-4">
            Optimized Transaction Validation Test
          </h1>
          <p className="text-gray-600 max-w-2xl mx-auto">
            This page demonstrates the optimized validation approach. Transaction type and description are validated on the frontend, 
            while amount validation is simplified and handled by the backend. Try entering invalid characters or values to see the validation in action.
          </p>
        </div>

        <div className="grid md:grid-cols-2 gap-8">
          {/* Transaction Form */}
          <div>
            <TransactionForm onSubmit={handleTransactionSubmit} />
          </div>

          {/* Test Cases */}
          <div className="bg-white p-6 rounded-lg shadow-md">
            <h2 className="text-xl font-bold mb-4 text-gray-800">Updated Test Cases</h2>
            
            <div className="space-y-4">
              <div>
                <h3 className="font-medium text-gray-700 mb-2">Amount Field Tests (Backend Validation):</h3>
                <ul className="text-sm text-gray-600 space-y-1">
                  <li>• Try: <code className="bg-gray-100 px-1">abc123</code> (letters + numbers)</li>
                  <li>• Try: <code className="bg-gray-100 px-1">-50</code> (negative number)</li>
                  <li>• Try: <code className="bg-gray-100 px-1">0</code> (zero)</li>
                  <li>• Try: <code className="bg-gray-100 px-1">100.50</code> (valid positive decimal)</li>
                </ul>
              </div>

              <div>
                <h3 className="font-medium text-gray-700 mb-2">Transaction Type Tests (Frontend Validation):</h3>
                <ul className="text-sm text-gray-600 space-y-1">
                  <li>• Try: <code className="bg-gray-100 px-1">Pay</code> (valid)</li>
                  <li>• Try: <code className="bg-gray-100 px-1">Load</code> (valid)</li>
                  <li>• Try: <code className="bg-gray-100 px-1">Refund</code> (valid)</li>
                  <li>• Try: <code className="bg-gray-100 px-1">TransferIn</code> (valid)</li>
                  <li>• Try: <code className="bg-gray-100 px-1">TransferOut</code> (valid)</li>
                  <li>• Try: <code className="bg-gray-100 px-1">InvalidType</code> (invalid)</li>
                </ul>
              </div>

              <div>
                <h3 className="font-medium text-gray-700 mb-2">Description Tests (Frontend Validation):</h3>
                <ul className="text-sm text-gray-600 space-y-1">
                  <li>• Try: <code className="bg-gray-100 px-1">Valid description 123</code> (valid)</li>
                  <li>• Try: <code className="bg-gray-100 px-1">Description with @#$%</code> (invalid symbols)</li>
                  <li>• Try: <code className="bg-gray-100 px-1">Description with dots...</code> (invalid punctuation)</li>
                  <li>• Try: <code className="bg-gray-100 px-1">Empty</code> (required field)</li>
                </ul>
              </div>
            </div>

            <div className="mt-6 p-4 bg-green-50 rounded-md">
              <h3 className="font-medium text-green-800 mb-2">Optimization Benefits:</h3>
              <ul className="text-sm text-green-700 space-y-1">
                <li>• <strong>Faster validation</strong> - Frontend validation is instant</li>
                <li>• <strong>Reduced server load</strong> - Less backend validation calls</li>
                <li>• <strong>Better UX</strong> - Real-time feedback without network delays</li>
                <li>• <strong>Simplified backend</strong> - Only essential validations on server</li>
                <li>• <strong>New transaction types</strong> - Support for TransferIn/TransferOut</li>
              </ul>
            </div>

            <div className="mt-4 p-4 bg-blue-50 rounded-md">
              <h3 className="font-medium text-blue-800 mb-2">Validation Strategy:</h3>
              <ul className="text-sm text-blue-700 space-y-1">
                <li>• <strong>Frontend</strong>: Transaction type, description format, real-time sanitization</li>
                <li>• <strong>Backend</strong>: Amount validation (positive number check), business logic</li>
                <li>• <strong>Database</strong>: Final data integrity and business rules</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default TransactionTestPage; 