-------------------------------------------------------------------------------------------
-------------------------------------TokenBlacklist----------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "TokenBlacklist" (
    "BlacklistID" SERIAL PRIMARY KEY,
    "SessionID" INTEGER NOT NULL,
    "RefreshToken" VARCHAR(500) NOT NULL,
    "BlacklistedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "BlacklistedBy" VARCHAR(50) DEFAULT 'System' CHECK ("BlacklistedBy" IN ('System', 'Customer', 'Admin')),
    "Reason" VARCHAR(200) NOT NULL,
    "IPAddress" TEXT,
    "UserAgent" TEXT,
    "SuspiciousActivity" BOOLEAN DEFAULT FALSE,
    "ComplianceRequired" BOOLEAN DEFAULT FALSE,

    CONSTRAINT "FK_TokenBlacklist_SessionToken_SessionID"
        FOREIGN KEY ("SessionID") 
        REFERENCES "SessionToken"("SessionID") 
        ON DELETE CASCADE
);