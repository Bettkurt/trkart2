CREATE TABLE SessionToken (
    SessionID INT PRIMARY KEY GENERATED ALWAYS AS IDENTITY,
    CustomerID INT NOT NULL,
    Token VARCHAR(500) UNIQUE NOT NULL,
    Expiration VARCHAR(20) NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID)
);
CREATE INDEX IX_SessionToken_CustomerID ON SessionToken(CustomerID);
CREATE INDEX IX_SessionToken_Token ON SessionToken(Token);