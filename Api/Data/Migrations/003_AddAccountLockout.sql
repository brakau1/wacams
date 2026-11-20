-- Migration: 003_AddAccountLockout
-- Description: Add account lockout columns to employees table for security hotfix
-- Date: 2026-11-19
-- Author: Curi Security Hotfix

-- Add lockout tracking columns to employees table
-- Column names match C# entity property names (EF Core default mapping)
ALTER TABLE "Employees" 
ADD COLUMN "FailedLoginCount" INTEGER NOT NULL DEFAULT 0,
ADD COLUMN "LockoutEnd" TIMESTAMP WITH TIME ZONE NULL,
ADD COLUMN "LastFailedLoginAt" TIMESTAMP WITH TIME ZONE NULL;

-- Add index for efficient lockout checks
CREATE INDEX idx_employees_lockout ON "Employees"("LockoutEnd") 
WHERE "LockoutEnd" IS NOT NULL;

-- Add index for failed login tracking
CREATE INDEX idx_employees_failed_logins ON "Employees"("FailedLoginCount") 
WHERE "FailedLoginCount" > 0;

-- Add comment for documentation
COMMENT ON COLUMN "Employees"."FailedLoginCount" IS 'Number of consecutive failed login attempts';
COMMENT ON COLUMN "Employees"."LockoutEnd" IS 'Account locked until this timestamp (UTC)';
COMMENT ON COLUMN "Employees"."LastFailedLoginAt" IS 'Timestamp of last failed login attempt (UTC)';
