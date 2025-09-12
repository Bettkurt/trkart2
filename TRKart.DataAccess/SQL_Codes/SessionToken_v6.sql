-------------------------------------------------------------------------------------------
-------------------------------------SessionToken------------------------------------------
-------------------------------------------------------------------------------------------

-- Create the SessionToken table
CREATE TABLE "SessionToken" (
    "SessionID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER NOT NULL,
    "AccessToken" VARCHAR(500) UNIQUE,
    "RefreshToken" VARCHAR(500) NOT NULL UNIQUE,
    "AccessTokenExpiration" TIMESTAMPTZ,
    "RefreshTokenExpiration" TIMESTAMPTZ NOT NULL,
    "RefreshTokenCreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "LastUsedAt" TIMESTAMPTZ,
    "UsageCount" INTEGER DEFAULT 0,
    "IsRevoked" BOOLEAN NOT NULL DEFAULT FALSE,
    "RevokedAt" TIMESTAMPTZ,
    "RevokeReason" VARCHAR(200),
    "DeviceInfo" TEXT,
    "DeviceFingerprint" VARCHAR(255),
    "IPAddress" TEXT,
    "UserAgent" TEXT,
    "IsSuspicious" BOOLEAN DEFAULT FALSE,
    "SuspiciousReason" VARCHAR(200),
    "CreatedAt" TIMESTAMPTZ DEFAULT NOW(),
    "UpdatedAt" TIMESTAMPTZ DEFAULT NOW(),

    CONSTRAINT "FK_SessionToken_Customers_CustomerID" 
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID")
        ON DELETE CASCADE
);