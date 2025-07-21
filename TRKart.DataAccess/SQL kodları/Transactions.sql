CREATE TABLE Transactions (
    TransactionId SERIAL PRIMARY KEY,
    CardId INT NOT NULL,
    Amount DECIMAL(10, 2) NOT NULL,
    Description TEXT,
    TransactionType VARCHAR(20) NOT NULL CHECK (TransactionType IN ('load', 'spend')),
    TransactionDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CardId) REFERENCES UserCards(CardId) ON DELETE CASCADE
);
