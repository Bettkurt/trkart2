CREATE TABLE "SessionToken" (
    "SessionID" SERIAL PRIMARY KEY,
    "CustomerID" INT NOT NULL,
    "Token" VARCHAR(500) UNIQUE NOT NULL,
    "Expiration" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP + INTERVAL '1 hour',  -- We can change this to 1 day or 1 week etc. if needed
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY ("CustomerID") REFERENCES "Customers"("CustomerID") ON DELETE CASCADE
);
