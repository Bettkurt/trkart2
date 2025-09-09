-------------------------------------------------------------------------------------------
------------------------------------AuditEvents--------------------------------------------
-------------------------------------------------------------------------------------------

CREATE TABLE "AuditEvents" (
    "AuditID" SERIAL PRIMARY KEY,
    "CustomerID" INTEGER,
    "EventType" VARCHAR(50) NOT NULL,
    "EventSubType" VARCHAR(50),
    "EventDetails" TEXT,
    "IPAddress" TEXT,
    "UserAgent" TEXT,
    "SessionID" INTEGER,
    "RiskLevel" VARCHAR(20) DEFAULT 'LOW',
    "ComplianceRequired" BOOLEAN DEFAULT FALSE,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT "FK_AuditEvents_Customers_CustomerID"
        FOREIGN KEY ("CustomerID") 
        REFERENCES "Customers"("CustomerID")
        ON DELETE CASCADE,
    
    CONSTRAINT "FK_AuditEvents_SessionToken_SessionID"
        FOREIGN KEY ("SessionID") 
        REFERENCES "SessionToken"("SessionID")
        ON DELETE CASCADE
);