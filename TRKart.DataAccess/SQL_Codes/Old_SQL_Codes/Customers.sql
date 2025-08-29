CREATE TABLE "Customers" (
    "CustomerID" SERIAL PRIMARY KEY,
    "FullName" VARCHAR(100),
    "Email" VARCHAR(100) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(200) NOT NULL
);