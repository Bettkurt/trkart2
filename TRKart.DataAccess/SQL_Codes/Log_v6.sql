CREATE TABLE "Log" (
    "LogID" SERIAL PRIMARY KEY,
    -- Name of the table the changes took place
    "LogTable" VARCHAR(50) NOT NULL,
    -- 0: INSERT, 1: UPDATE, 2: DELETE, 3: ERROR
    "LogType" INT NOT NULL,
    "LogMessage" TEXT NOT NULL,
    "LogTimestamp" TIMESTAMPTZ DEFAULT NOW(),
    "LogDetails" TEXT
);

------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION LogTriggerFunction()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO Log (LogTable, LogType, LogMessage, LogDetails)
    VALUES (TG_TABLE_NAME, 
    CASE 
        WHEN TG_OP = 'INSERT' THEN 0
        WHEN TG_OP = 'UPDATE' THEN 1
        WHEN TG_OP = 'DELETE' THEN 2
        ELSE 3
    END,
    
    CASE 
        WHEN TG_OP = 'INSERT' THEN 'New row inserted'
        WHEN TG_OP = 'UPDATE' THEN 'Row updated'
        WHEN TG_OP = 'DELETE' THEN 'Row deleted'
        ELSE 'Unknown operation'
    END,
    ROW(NEW));
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;    

------------------------------------------------------------------------

CREATE TRIGGER LogTrigger
AFTER INSERT OR UPDATE OR DELETE ON "Customers", "SessionToken", 
    "TokenBlacklist", "UserCard", "CardBlacklist",
     "CardLimits", "Transaction"
FOR EACH ROW EXECUTE FUNCTION LogTriggerFunction();