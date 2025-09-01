export enum TransactionType {
  Load = 0,
  TopUp = 1,
  Refund = 2,
  TransferIn = 3,
  TransferOut = 4,
  Pay = 5,
  SystemTransferIn = 6,
  SystemTransferOut = 7
}

// Helper function to get the string name of a transaction type
export const getTransactionTypeName = (type: TransactionType): string => {
  switch (type) {
    case TransactionType.Load:
      return 'Load';
    case TransactionType.TopUp:
      return 'Top-Up';
    case TransactionType.Refund:
      return 'Refund';
    case TransactionType.TransferIn:
      return 'Incoming Transfer';
    case TransactionType.TransferOut:
      return 'Outgoing Transfer';
    case TransactionType.Pay:
      return 'Payment';
    case TransactionType.SystemTransferIn:
      return 'System Transfer In';
    case TransactionType.SystemTransferOut:
      return 'System Transfer Out';
    default:
      return 'Unknown';
  }
};

// Helper function to parse transaction type from number
export const parseTransactionType = (value: number): TransactionType | null => {
  if (Object.values(TransactionType).includes(value)) {
    return value as TransactionType;
  }
  return null;
};

// Get transaction types that users can create (for form dropdowns)
export const getUserCreatableTransactionTypes = (): TransactionType[] => {
  return [
    TransactionType.Load,
    TransactionType.TopUp,
    TransactionType.Refund,      // !!!Only for testing!!!
    TransactionType.TransferOut,
    TransactionType.Pay
  ];
};

// Get all transaction types that users can see in transaction history
export const getUserViewableTransactionTypes = (): TransactionType[] => {
  return Object.values(TransactionType).filter(value => typeof value === 'number') as TransactionType[];
};