CREATE TABLE Transaction (
    TransactionID INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    CardID INT NOT NULL,
    Amount DECIMAL(10,2) NOT NULL,
    TransactionType VARCHAR(20) NOT NULL,
    Description TEXT,
    TransactionDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    TransactionStatus VARCHAR(20) DEFAULT 'Completed',
    FOREIGN KEY (CardID) REFERENCES UserCard(CardID)
);
CREATE INDEX IX_Transaction_CardID ON Transaction(CardID); 