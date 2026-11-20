-- Migration: Add Notification Platform Tables
-- Description: Creates tables for multi-channel notification system (Email, Teams, SMS, WhatsApp)

-- Create Notifications table
CREATE TABLE "Notifications" (
    "Id" UUID PRIMARY KEY,
    "EventType" TEXT NOT NULL,
    "Channel" TEXT NOT NULL,
    "RecipientEmployeeId" TEXT NOT NULL,
    "RecipientName" TEXT,
    "RecipientAddress" TEXT NOT NULL,
    "Subject" TEXT NOT NULL,
    "Body" TEXT NOT NULL,
    "Priority" TEXT NOT NULL,
    "Status" TEXT NOT NULL,
    "RelatedEntityId" TEXT,
    "RelatedEntityType" TEXT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "SentAt" TIMESTAMP WITH TIME ZONE,
    "DeliveredAt" TIMESTAMP WITH TIME ZONE,
    "ReadAt" TIMESTAMP WITH TIME ZONE,
    "ErrorMessage" TEXT,
    "RetryCount" INTEGER NOT NULL DEFAULT 0
);

-- Create NotificationTemplates table
CREATE TABLE "NotificationTemplates" (
    "Id" UUID PRIMARY KEY,
    "Name" TEXT NOT NULL UNIQUE,
    "EventType" TEXT NOT NULL,
    "Channel" TEXT NOT NULL,
    "SubjectTemplate" TEXT NOT NULL,
    "BodyTemplate" TEXT NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP WITH TIME ZONE,
    UNIQUE("EventType", "Channel")
);

-- Create NotificationPreferences table
CREATE TABLE "NotificationPreferences" (
    "Id" UUID PRIMARY KEY,
    "EmployeeId" TEXT NOT NULL,
    "EventType" TEXT NOT NULL,
    "Channel" TEXT NOT NULL,
    "IsEnabled" BOOLEAN NOT NULL DEFAULT TRUE,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE("EmployeeId", "EventType", "Channel")
);

-- Create indexes for performance
CREATE INDEX "IX_Notifications_RecipientEmployeeId" ON "Notifications" ("RecipientEmployeeId");
CREATE INDEX "IX_Notifications_CreatedAt" ON "Notifications" ("CreatedAt" DESC);
CREATE INDEX "IX_Notifications_Status" ON "Notifications" ("Status");
CREATE INDEX "IX_Notifications_EventType" ON "Notifications" ("EventType");
CREATE INDEX "IX_Notifications_Channel" ON "Notifications" ("Channel");

CREATE INDEX "IX_NotificationTemplates_EventType_Channel" ON "NotificationTemplates" ("EventType", "Channel");
CREATE INDEX "IX_NotificationTemplates_IsActive" ON "NotificationTemplates" ("IsActive");

CREATE INDEX "IX_NotificationPreferences_EmployeeId" ON "NotificationPreferences" ("EmployeeId");
CREATE INDEX "IX_NotificationPreferences_EventType" ON "NotificationPreferences" ("EventType");

-- Add comments
COMMENT ON TABLE "Notifications" IS 'Stores all notification records sent through various channels';
COMMENT ON TABLE "NotificationTemplates" IS 'Reusable templates for notification content by event type and channel';
COMMENT ON TABLE "NotificationPreferences" IS 'User preferences for which notification channels to receive';

COMMENT ON COLUMN "Notifications"."EventType" IS 'Type of event: AttendanceException, CorrectiveActionGenerated, ApprovalRequired, Escalation, DocumentUploaded';
COMMENT ON COLUMN "Notifications"."Channel" IS 'Delivery channel: Email, MicrosoftTeams, SMS, WhatsApp';
COMMENT ON COLUMN "Notifications"."Status" IS 'Delivery status: Pending, Sent, Delivered, Failed, Read';
COMMENT ON COLUMN "Notifications"."Priority" IS 'Priority level: Low, Normal, High, Urgent';
