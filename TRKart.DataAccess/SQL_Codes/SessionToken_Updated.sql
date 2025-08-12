-------------------------------------------------------------------------------------------
-------------------------------------SessionToken------------------------------------------
-------------------------------------------------------------------------------------------

-- Create the SessionToken table
CREATE TABLE "SessionToken"
(
    "SessionID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER NOT NULL,
    "AccessToken" VARCHAR(500) UNIQUE,
    "RefreshToken" VARCHAR(500) NOT NULL UNIQUE,
    "AccessTokenExpiration" TIMESTAMP,
    "RefreshTokenExpiration" TIMESTAMP NOT NULL,
    "RefreshTokenCreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsRevoked" BOOLEAN NOT NULL DEFAULT false,
    "DeviceInfo" TEXT,
    "IPAddress" TEXT,
    
    -- Foreign key constraint
    CONSTRAINT "FK_SessionToken_Customers_CustomerID" 
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID")
        ON DELETE CASCADE
);