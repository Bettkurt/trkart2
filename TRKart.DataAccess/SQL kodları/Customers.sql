CREATE TABLE Customers (
    Id SERIAL PRIMARY KEY,
    FullName VARCHAR(100) NOT NULL,
    Email VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash VARCHAR(200) NOT NULL
);

INSERT INTO Customers (FullName, Email, PasswordHash)
VALUES ('Test Kullanıcı', 'simal@example.com', 'simal123');
