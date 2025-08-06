-------------------------------------------------------------------------------------------
-------------------------------------SessionToken------------------------------------------
-------------------------------------------------------------------------------------------

-- Drop the existing table if it exists to avoid conflicts
DROP TABLE IF EXISTS public."SessionToken" CASCADE;

-- Create the SessionToken table
CREATE TABLE public."SessionToken"
(
    "SessionID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER NOT NULL,
    "AccessToken" TEXT,
    "RefreshToken" TEXT NOT NULL UNIQUE,
    "AccessTokenExpiration" TIMESTAMP WITH,
    "RefreshTokenExpiration" TIMESTAMP WITH NOT NULL,
    "RefreshTokenCreatedAt" TIMESTAMP WITH NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "IsRevoked" BOOLEAN NOT NULL DEFAULT false,
    "DeviceInfo" TEXT,
    "IPAddress" TEXT,
    
    -- Foreign key constraint
    CONSTRAINT "FK_SessionToken_Customers_CustomerID" 
        FOREIGN KEY ("CustomerID") 
        REFERENCES public."Customers" ("CustomerID")
        ON DELETE CASCADE,
);
