export interface WalletDto {
  walletId: number;
  customerId: number;
  walletNumber: string;
  balance: number;
  status: number; // Matches CardStatus enum (0: Deactivated, 1: Expired, 2: Lost, 3: Inactive, 4: Active)
  createdAt: string;
  updatedAt: string;
}

export enum TransactionType {
  Load = 'WalletLoad',
  Pay = 'WalletPay',
  System = 'System'
}

export enum TransactionStatus {
  Pending = 'Pending',
  Approved = 'Approved',
  Failed = 'Failed'
}

export interface WalletTransactionDto {
  transactionId: number;
  walletId: number;
  cardId?: number;
  amount: number;
  transactionType: TransactionType;
  description: string;
  referenceId?: string;
  transactionDate: string;
  status: TransactionStatus;
}

export interface CreateWalletTransactionDto {
  walletId: number;
  cardId?: number;
  amount: number;
  description: string;
  referenceId?: string;
}

export interface WalletBalanceDto {
  walletId: number;
  balance: number;
}

export interface WalletTransactionResponse {
  transaction: WalletTransactionDto;
  newBalance: number;
}
