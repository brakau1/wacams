using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;
using WACAMS.Infrastructure.Data;

namespace WACAMS.Infrastructure.Data;

/// <summary>
/// Seeds the database with default notification templates for all event types and channels
/// </summary>
public static class NotificationTemplateSeeder
{
    public static async Task SeedAsync(WacamContext context)
    {
        if (await context.NotificationTemplates.AnyAsync())
        {
            return; // Already seeded
        }

        var templates = new List<NotificationTemplate>
        {
            // ═══════════════════════════════════════════════════════════════════
            // EVENT: AttendanceException
            // ═══════════════════════════════════════════════════════════════════

            // Email
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000001-0000-0000-0000-000000000001"),
                Name = "AttendanceException_Email",
                EventType = NotificationEventType.AttendanceException,
                Channel = NotificationChannel.Email,
                SubjectTemplate = "⚠️ Attendance Exception: {{ExceptionType}} — {{EmployeeName}}",
                BodyTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
  <div style='background-color: #dc2626; color: white; padding: 16px; border-radius: 8px 8px 0 0;'>
    <h2 style='margin: 0;'>Attendance Exception Detected</h2>
  </div>
  <div style='border: 1px solid #e5e7eb; padding: 20px; border-radius: 0 0 8px 8px;'>
    <p>An attendance exception has been recorded and requires investigation.</p>
    <table style='width: 100%; border-collapse: collapse; margin: 16px 0;'>
      <tr><td style='padding: 8px; font-weight: bold;'>Employee:</td><td style='padding: 8px;'>{{EmployeeName}} ({{EmployeeId}})</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Department:</td><td style='padding: 8px;'>{{DepartmentName}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Exception Type:</td><td style='padding: 8px;'>{{ExceptionType}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Date:</td><td style='padding: 8px;'>{{Date}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Lost Production:</td><td style='padding: 8px;'>{{LostMinutes}} minutes</td></tr>
    </table>
    <p style='margin-top: 16px;'>Please investigate this exception at your earliest convenience.</p>
    <a href='{{AppUrl}}/exceptions/{{ExceptionId}}' style='display: inline-block; background-color: #2563eb; color: white; padding: 10px 20px; text-decoration: none; border-radius: 6px; margin-top: 12px;'>View Exception Details</a>
  </div>
  <p style='color: #6b7280; font-size: 12px; margin-top: 16px;'>This is an automated notification from WACAMS. Do not reply to this email.</p>
</div>",
                IsActive = true
            },

            // Microsoft Teams
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000001-0000-0000-0000-000000000002"),
                Name = "AttendanceException_Teams",
                EventType = NotificationEventType.AttendanceException,
                Channel = NotificationChannel.MicrosoftTeams,
                SubjectTemplate = "⚠️ Attendance Exception: {{ExceptionType}} — {{EmployeeName}}",
                BodyTemplate = "An attendance exception has been recorded for **{{EmployeeName}}** ({{EmployeeId}}) in {{DepartmentName}}.\n\n**Type:** {{ExceptionType}}\n**Date:** {{Date}}\n**Lost Production:** {{LostMinutes}} min\n\nPlease investigate this exception.",
                IsActive = true
            },

            // SMS
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000001-0000-0000-0000-000000000003"),
                Name = "AttendanceException_SMS",
                EventType = NotificationEventType.AttendanceException,
                Channel = NotificationChannel.SMS,
                SubjectTemplate = "WACAMS Alert",
                BodyTemplate = "WACAMS: Attendance exception for {{EmployeeName}} — {{ExceptionType}} on {{Date}}. Lost: {{LostMinutes}}min. Investigate at {{AppUrl}}/exceptions/{{ExceptionId}}",
                IsActive = true
            },

            // WhatsApp
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000001-0000-0000-0000-000000000004"),
                Name = "AttendanceException_WhatsApp",
                EventType = NotificationEventType.AttendanceException,
                Channel = NotificationChannel.WhatsApp,
                SubjectTemplate = "WACAMS Alert",
                BodyTemplate = "⚠️ *Attendance Exception*\n\nEmployee: {{EmployeeName}} ({{EmployeeId}})\nDept: {{DepartmentName}}\nType: {{ExceptionType}}\nDate: {{Date}}\nLost: {{LostMinutes}} min\n\nPlease investigate.",
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════════
            // EVENT: CorrectiveActionGenerated
            // ═══════════════════════════════════════════════════════════════════

            // Email
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000002-0000-0000-0000-000000000001"),
                Name = "CorrectiveActionGenerated_Email",
                EventType = NotificationEventType.CorrectiveActionGenerated,
                Channel = NotificationChannel.Email,
                SubjectTemplate = "📋 Corrective Action Generated: {{WarningType}} — {{EmployeeName}}",
                BodyTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
  <div style='background-color: #f59e0b; color: white; padding: 16px; border-radius: 8px 8px 0 0;'>
    <h2 style='margin: 0;'>Corrective Action Generated</h2>
  </div>
  <div style='border: 1px solid #e5e7eb; padding: 20px; border-radius: 0 0 8px 8px;'>
    <p>A new corrective action has been generated and requires your attention.</p>
    <table style='width: 100%; border-collapse: collapse; margin: 16px 0;'>
      <tr><td style='padding: 8px; font-weight: bold;'>Employee:</td><td style='padding: 8px;'>{{EmployeeName}} ({{EmployeeId}})</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Department:</td><td style='padding: 8px;'>{{DepartmentName}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Warning Type:</td><td style='padding: 8px;'>{{WarningType}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Title:</td><td style='padding: 8px;'>{{WarningTitle}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Generated By:</td><td style='padding: 8px;'>{{IssuedByName}}</td></tr>
    </table>
    <p style='margin-top: 16px;'>Please review and issue this corrective action.</p>
    <a href='{{AppUrl}}/warnings/{{WarningId}}' style='display: inline-block; background-color: #2563eb; color: white; padding: 10px 20px; text-decoration: none; border-radius: 6px; margin-top: 12px;'>View Warning Details</a>
  </div>
  <p style='color: #6b7280; font-size: 12px; margin-top: 16px;'>This is an automated notification from WACAMS. Do not reply to this email.</p>
</div>",
                IsActive = true
            },

            // Teams
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000002-0000-0000-0000-000000000002"),
                Name = "CorrectiveActionGenerated_Teams",
                EventType = NotificationEventType.CorrectiveActionGenerated,
                Channel = NotificationChannel.MicrosoftTeams,
                SubjectTemplate = "📋 Corrective Action Generated: {{WarningType}} — {{EmployeeName}}",
                BodyTemplate = "A new corrective action has been generated.\n\n**Employee:** {{EmployeeName}} ({{EmployeeId}})\n**Dept:** {{DepartmentName}}\n**Warning Type:** {{WarningType}}\n**Title:** {{WarningTitle}}\n**Generated By:** {{IssuedByName}}\n\nPlease review and issue this corrective action.",
                IsActive = true
            },

            // SMS
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000002-0000-0000-0000-000000000003"),
                Name = "CorrectiveActionGenerated_SMS",
                EventType = NotificationEventType.CorrectiveActionGenerated,
                Channel = NotificationChannel.SMS,
                SubjectTemplate = "WACAMS Alert",
                BodyTemplate = "WACAMS: Corrective action generated for {{EmployeeName}} — {{WarningType}}. Review at {{AppUrl}}/warnings/{{WarningId}}",
                IsActive = true
            },

            // WhatsApp
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000002-0000-0000-0000-000000000004"),
                Name = "CorrectiveActionGenerated_WhatsApp",
                EventType = NotificationEventType.CorrectiveActionGenerated,
                Channel = NotificationChannel.WhatsApp,
                SubjectTemplate = "WACAMS Alert",
                BodyTemplate = "📋 *Corrective Action Generated*\n\nEmployee: {{EmployeeName}} ({{EmployeeId}})\nDept: {{DepartmentName}}\nType: {{WarningType}}\nTitle: {{WarningTitle}}\n\nPlease review and issue.",
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════════
            // EVENT: ApprovalRequired
            // ═══════════════════════════════════════════════════════════════════

            // Email
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000003-0000-0000-0000-000000000001"),
                Name = "ApprovalRequired_Email",
                EventType = NotificationEventType.ApprovalRequired,
                Channel = NotificationChannel.Email,
                SubjectTemplate = "✅ Approval Required: {{EntityType}} from {{RequesterName}}",
                BodyTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
  <div style='background-color: #2563eb; color: white; padding: 16px; border-radius: 8px 8px 0 0;'>
    <h2 style='margin: 0;'>Approval Required</h2>
  </div>
  <div style='border: 1px solid #e5e7eb; padding: 20px; border-radius: 0 0 8px 8px;'>
    <p>An item requires your approval.</p>
    <table style='width: 100%; border-collapse: collapse; margin: 16px 0;'>
      <tr><td style='padding: 8px; font-weight: bold;'>Type:</td><td style='padding: 8px;'>{{EntityType}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Requested By:</td><td style='padding: 8px;'>{{RequesterName}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Description:</td><td style='padding: 8px;'>{{Description}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Submitted:</td><td style='padding: 8px;'>{{SubmittedDate}}</td></tr>
    </table>
    <p style='margin-top: 16px;'>Please review and take action.</p>
    <a href='{{AppUrl}}/{{EntityPath}}' style='display: inline-block; background-color: #2563eb; color: white; padding: 10px 20px; text-decoration: none; border-radius: 6px; margin-top: 12px;'>Review & Approve</a>
  </div>
  <p style='color: #6b7280; font-size: 12px; margin-top: 16px;'>This is an automated notification from WACAMS. Do not reply to this email.</p>
</div>",
                IsActive = true
            },

            // Teams
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000003-0000-0000-0000-000000000002"),
                Name = "ApprovalRequired_Teams",
                EventType = NotificationEventType.ApprovalRequired,
                Channel = NotificationChannel.MicrosoftTeams,
                SubjectTemplate = "✅ Approval Required: {{EntityType}} from {{RequesterName}}",
                BodyTemplate = "An item requires your approval.\n\n**Type:** {{EntityType}}\n**Requested By:** {{RequesterName}}\n**Description:** {{Description}}\n**Submitted:** {{SubmittedDate}}\n\nPlease review and take action.",
                IsActive = true
            },

            // SMS
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000003-0000-0000-0000-000000000003"),
                Name = "ApprovalRequired_SMS",
                EventType = NotificationEventType.ApprovalRequired,
                Channel = NotificationChannel.SMS,
                SubjectTemplate = "WACAMS Alert",
                BodyTemplate = "WACAMS: Approval required — {{EntityType}} from {{RequesterName}}. {{Description}}. Review at {{AppUrl}}/{{EntityPath}}",
                IsActive = true
            },

            // WhatsApp
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000003-0000-0000-0000-000000000004"),
                Name = "ApprovalRequired_WhatsApp",
                EventType = NotificationEventType.ApprovalRequired,
                Channel = NotificationChannel.WhatsApp,
                SubjectTemplate = "WACAMS Alert",
                BodyTemplate = "✅ *Approval Required*\n\nType: {{EntityType}}\nFrom: {{RequesterName}}\nDescription: {{Description}}\nSubmitted: {{SubmittedDate}}\n\nPlease review.",
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════════
            // EVENT: Escalation
            // ═══════════════════════════════════════════════════════════════════

            // Email
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000004-0000-0000-0000-000000000001"),
                Name = "Escalation_Email",
                EventType = NotificationEventType.Escalation,
                Channel = NotificationChannel.Email,
                SubjectTemplate = "🔺 ESCALATION: Level {{EscalationLevel}} — {{EmployeeName}}",
                BodyTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
  <div style='background-color: #dc2626; color: white; padding: 16px; border-radius: 8px 8px 0 0;'>
    <h2 style='margin: 0;'>🔺 Escalation Alert — Action Required</h2>
  </div>
  <div style='border: 1px solid #e5e7eb; padding: 20px; border-radius: 0 0 8px 8px;'>
    <p>An attendance issue has been escalated to you for immediate action.</p>
    <table style='width: 100%; border-collapse: collapse; margin: 16px 0;'>
      <tr><td style='padding: 8px; font-weight: bold;'>Employee:</td><td style='padding: 8px;'>{{EmployeeName}} ({{EmployeeId}})</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Department:</td><td style='padding: 8px;'>{{DepartmentName}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Escalation Level:</td><td style='padding: 8px;'>Level {{EscalationLevel}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Escalated From:</td><td style='padding: 8px;'>{{EscalatedFromName}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Reason:</td><td style='padding: 8px;'>{{EscalationReason}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Deadline:</td><td style='padding: 8px; color: #dc2626; font-weight: bold;'>{{Deadline}}</td></tr>
    </table>
    <p style='margin-top: 16px; color: #dc2626; font-weight: bold;'>⏰ You have 24 hours to resolve this escalation.</p>
    <a href='{{AppUrl}}/escalations/{{EscalationId}}' style='display: inline-block; background-color: #dc2626; color: white; padding: 10px 20px; text-decoration: none; border-radius: 6px; margin-top: 12px;'>View Escalation</a>
  </div>
  <p style='color: #6b7280; font-size: 12px; margin-top: 16px;'>This is an automated notification from WACAMS. Do not reply to this email.</p>
</div>",
                IsActive = true
            },

            // Teams
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000004-0000-0000-0000-000000000002"),
                Name = "Escalation_Teams",
                EventType = NotificationEventType.Escalation,
                Channel = NotificationChannel.MicrosoftTeams,
                SubjectTemplate = "🔺 ESCALATION: Level {{EscalationLevel}} — {{EmployeeName}}",
                BodyTemplate = "🔺 **ESCALATION — Action Required**\n\n**Employee:** {{EmployeeName}} ({{EmployeeId}})\n**Dept:** {{DepartmentName}}\n**Level:** {{EscalationLevel}}\n**Escalated From:** {{EscalatedFromName}}\n**Reason:** {{EscalationReason}}\n**Deadline:** {{Deadline}}\n\n⏰ You have 24 hours to resolve this.",
                IsActive = true
            },

            // SMS
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000004-0000-0000-0000-000000000003"),
                Name = "Escalation_SMS",
                EventType = NotificationEventType.Escalation,
                Channel = NotificationChannel.SMS,
                SubjectTemplate = "WACAMS URGENT",
                BodyTemplate = "WACAMS URGENT: Escalation Level {{EscalationLevel}} for {{EmployeeName}}. Deadline: {{Deadline}}. Act now at {{AppUrl}}/escalations/{{EscalationId}}",
                IsActive = true
            },

            // WhatsApp
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000004-0000-0000-0000-000000000004"),
                Name = "Escalation_WhatsApp",
                EventType = NotificationEventType.Escalation,
                Channel = NotificationChannel.WhatsApp,
                SubjectTemplate = "WACAMS URGENT",
                BodyTemplate = "🔺 *ESCALATION — Action Required*\n\nEmployee: {{EmployeeName}} ({{EmployeeId}})\nDept: {{DepartmentName}}\nLevel: {{EscalationLevel}}\nFrom: {{EscalatedFromName}}\nReason: {{EscalationReason}}\nDeadline: {{Deadline}}\n\n⏰ You have 24 hours to resolve.",
                IsActive = true
            },

            // ═══════════════════════════════════════════════════════════════════
            // EVENT: DocumentUploaded
            // ═══════════════════════════════════════════════════════════════════

            // Email
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000005-0000-0000-0000-000000000001"),
                Name = "DocumentUploaded_Email",
                EventType = NotificationEventType.DocumentUploaded,
                Channel = NotificationChannel.Email,
                SubjectTemplate = "📎 Document Uploaded: {{DocumentType}} for {{EmployeeName}}",
                BodyTemplate = @"
<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
  <div style='background-color: #16a34a; color: white; padding: 16px; border-radius: 8px 8px 0 0;'>
    <h2 style='margin: 0;'>Document Uploaded</h2>
  </div>
  <div style='border: 1px solid #e5e7eb; padding: 20px; border-radius: 0 0 8px 8px;'>
    <p>A document has been uploaded and is ready for review.</p>
    <table style='width: 100%; border-collapse: collapse; margin: 16px 0;'>
      <tr><td style='padding: 8px; font-weight: bold;'>Document Type:</td><td style='padding: 8px;'>{{DocumentType}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Employee:</td><td style='padding: 8px;'>{{EmployeeName}} ({{EmployeeId}})</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Uploaded By:</td><td style='padding: 8px;'>{{UploadedByName}}</td></tr>
      <tr><td style='padding: 8px; font-weight: bold;'>Upload Date:</td><td style='padding: 8px;'>{{UploadDate}}</td></tr>
    </table>
    <a href='{{AppUrl}}/{{DocumentPath}}' style='display: inline-block; background-color: #2563eb; color: white; padding: 10px 20px; text-decoration: none; border-radius: 6px; margin-top: 12px;'>View Document</a>
  </div>
  <p style='color: #6b7280; font-size: 12px; margin-top: 16px;'>This is an automated notification from WACAMS. Do not reply to this email.</p>
</div>",
                IsActive = true
            },

            // Teams
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000005-0000-0000-0000-000000000002"),
                Name = "DocumentUploaded_Teams",
                EventType = NotificationEventType.DocumentUploaded,
                Channel = NotificationChannel.MicrosoftTeams,
                SubjectTemplate = "📎 Document Uploaded: {{DocumentType}} for {{EmployeeName}}",
                BodyTemplate = "A document has been uploaded.\n\n**Type:** {{DocumentType}}\n**Employee:** {{EmployeeName}} ({{EmployeeId}})\n**Uploaded By:** {{UploadedByName}}\n**Date:** {{UploadDate}}",
                IsActive = true
            },

            // SMS
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000005-0000-0000-0000-000000000003"),
                Name = "DocumentUploaded_SMS",
                EventType = NotificationEventType.DocumentUploaded,
                Channel = NotificationChannel.SMS,
                SubjectTemplate = "WACAMS",
                BodyTemplate = "WACAMS: {{DocumentType}} uploaded for {{EmployeeName}} by {{UploadedByName}}. View at {{AppUrl}}/{{DocumentPath}}",
                IsActive = true
            },

            // WhatsApp
            new NotificationTemplate
            {
                Id = Guid.Parse("A0000005-0000-0000-0000-000000000004"),
                Name = "DocumentUploaded_WhatsApp",
                EventType = NotificationEventType.DocumentUploaded,
                Channel = NotificationChannel.WhatsApp,
                SubjectTemplate = "WACAMS",
                BodyTemplate = "📎 *Document Uploaded*\n\nType: {{DocumentType}}\nEmployee: {{EmployeeName}} ({{EmployeeId}})\nUploaded By: {{UploadedByName}}\nDate: {{UploadDate}}",
                IsActive = true
            }
        };

        await context.NotificationTemplates.AddRangeAsync(templates);
        await context.SaveChangesAsync();
    }
}
