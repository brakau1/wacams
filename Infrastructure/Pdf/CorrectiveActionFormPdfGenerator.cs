using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WACAMS.Core.Disciplinary.Models;

namespace WACAMS.Infrastructure.Pdf;

/// <summary>
/// Data model for corrective action form PDF generation
/// </summary>
public class CorrectiveActionFormData
{
    // Employee Information
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    
    // Incident Details
    public DateTime IncidentDate { get; set; }
    public string OffenceType { get; set; } = string.Empty;
    public string IncidentDescription { get; set; } = string.Empty;
    
    // Disciplinary Information
    public string DisciplinaryLevel { get; set; } = string.Empty;
    public int OffenceNumber { get; set; }
    public DateTime IssuedDate { get; set; }
    public string IssuedBy { get; set; } = string.Empty;
    public string IssuerPosition { get; set; } = string.Empty;
    
    // Form Sections
    public string SituationDescription { get; set; } = string.Empty;
    public string EmployeeReasons { get; set; } = string.Empty;
    public string RootCauses { get; set; } = string.Empty;
    public string ActionTaken { get; set; } = string.Empty;
    public string RecommendedSanction { get; set; } = string.Empty;
    public DateTime FollowUpDate { get; set; }
    
    // Signatures
    public string? EmployeeSignature { get; set; }
    public DateTime? EmployeeSignatureDate { get; set; }
    public string? ManagerSignature { get; set; }
    public DateTime? ManagerSignatureDate { get; set; }
}

/// <summary>
/// Generates corrective action form PDFs using QuestPDF
/// </summary>
public class CorrectiveActionFormPdfGenerator
{
    public byte[] Generate(CorrectiveActionFormData data)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));
                
                page.Header().Element(GenerateHeader);
                page.Content().Element(content => GenerateContent(content, data));
                page.Footer().Element(GenerateFooter);
            });
        }).GeneratePdf();
    }
    
    private void GenerateHeader(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text("CORRECTIVE ACTION FORM")
                .FontSize(20).Bold().FontColor(Colors.Blue.Darken3);
            
            column.Item().Height(5).BorderBottom(2).BorderColor(Colors.Blue.Darken3);
            column.Item().Height(10);
        });
    }
    
    private void GenerateContent(IContainer container, CorrectiveActionFormData data)
    {
        container.PaddingVertical(10).Column(column =>
        {
            column.Spacing(15);
            
            // Employee Information Section
            column.Item().Element(c => GenerateEmployeeInfoSection(c, data));
            
            // Section 1: Describe Situation
            column.Item().Element(c => GenerateSection(c, "1. DESCRIBE SITUATION", data.SituationDescription));
            
            // Section 2: Ask For Reasons
            column.Item().Element(c => GenerateSection(c, "2. EMPLOYEE'S REASONS", data.EmployeeReasons));
            
            // Section 3: Discuss Causes
            column.Item().Element(c => GenerateSection(c, "3. ROOT CAUSES DISCUSSED", data.RootCauses));
            
            // Section 4: Action Taken
            column.Item().Element(c => GenerateSection(c, "4. ACTION TAKEN", data.ActionTaken));
            
            // Section 5: Recommended Sanction
            column.Item().Element(c => GenerateSection(c, "5. RECOMMENDED SANCTION", data.RecommendedSanction));
            
            // Section 6: Follow Up Date
            column.Item().Element(c => GenerateFollowUpSection(c, data.FollowUpDate));
            
            // Signatures Section
            column.Item().Element(c => GenerateSignaturesSection(c, data));
        });
    }
    
    private void GenerateEmployeeInfoSection(IContainer container, CorrectiveActionFormData data)
    {
        container.Column(column =>
        {
            column.Item().Text("EMPLOYEE INFORMATION")
                .FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            
            column.Item().Height(5);
            
            column.Item().Grid(grid =>
            {
                grid.Columns(2);
                grid.Spacing(10);
                
                grid.Item().ColumnSpan(1).Element(c => GenerateInfoField(c, "Employee ID:", data.EmployeeId));
                grid.Item().ColumnSpan(1).Element(c => GenerateInfoField(c, "Employee Name:", data.EmployeeName));
                grid.Item().ColumnSpan(1).Element(c => GenerateInfoField(c, "Department:", data.Department));
                grid.Item().ColumnSpan(1).Element(c => GenerateInfoField(c, "Position:", data.Position));
                grid.Item().ColumnSpan(1).Element(c => GenerateInfoField(c, "Incident Date:", data.IncidentDate.ToString("dd MMM yyyy")));
                grid.Item().ColumnSpan(1).Element(c => GenerateInfoField(c, "Offence Type:", data.OffenceType));
                grid.Item().ColumnSpan(1).Element(c => GenerateInfoField(c, "Disciplinary Level:", data.DisciplinaryLevel));
                grid.Item().ColumnSpan(1).Element(c => GenerateInfoField(c, "Offence Number:", $"#{data.OffenceNumber}"));
                grid.Item().ColumnSpan(1).Element(c => GenerateInfoField(c, "Issued Date:", data.IssuedDate.ToString("dd MMM yyyy")));
                grid.Item().ColumnSpan(1).Element(c => GenerateInfoField(c, "Issued By:", data.IssuedBy));
            });
        });
    }
    
    private void GenerateInfoField(IContainer container, string label, string value)
    {
        container.Column(column =>
        {
            column.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Darken2);
            column.Item().Text(value).FontSize(10).Bold();
        });
    }
    
    private void GenerateSection(IContainer container, string title, string content)
    {
        container.Column(column =>
        {
            column.Item().Text(title)
                .FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            
            column.Item().Height(5);
            
            column.Item()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(10)
                .MinHeight(80)
                .Text(string.IsNullOrEmpty(content) ? "No content provided." : content)
                .FontSize(10);
        });
    }
    
    private void GenerateFollowUpSection(IContainer container, DateTime followUpDate)
    {
        container.Column(column =>
        {
            column.Item().Text("6. FOLLOW-UP DATE")
                .FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            
            column.Item().Height(5);
            
            column.Item()
                .Border(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(10)
                .Row(row =>
                {
                    row.RelativeItem().Text($"Follow-up scheduled for: {followUpDate:dd MMM yyyy}")
                        .FontSize(10);
                });
        });
    }
    
    private void GenerateSignaturesSection(IContainer container, CorrectiveActionFormData data)
    {
        container.Column(column =>
        {
            column.Item().Text("SIGNATURES")
                .FontSize(12).Bold().FontColor(Colors.Blue.Darken2);
            
            column.Item().Height(5);
            
            column.Item().Grid(grid =>
            {
                grid.Columns(2);
                grid.Spacing(20);
                
                // Employee Signature
                grid.Item().ColumnSpan(1).Column(col =>
                {
                    col.Item().Text("Employee Signature").FontSize(10).Bold();
                    col.Item().Height(40);
                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Darken2);
                    col.Item().Height(5);
                    col.Item().Text(data.EmployeeSignatureDate.HasValue 
                        ? $"Signed: {data.EmployeeSignatureDate:dd MMM yyyy}" 
                        : "Date: ___________________")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
                
                // Manager Signature
                grid.Item().ColumnSpan(1).Column(col =>
                {
                    col.Item().Text("Manager Signature").FontSize(10).Bold();
                    col.Item().Height(40);
                    col.Item().BorderBottom(1).BorderColor(Colors.Grey.Darken2);
                    col.Item().Height(5);
                    col.Item().Text(data.ManagerSignatureDate.HasValue 
                        ? $"Signed: {data.ManagerSignatureDate:dd MMM yyyy}" 
                        : "Date: ___________________")
                        .FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            });
        });
    }
    
    private void GenerateFooter(IContainer container)
    {
        container.AlignCenter().Column(column =>
        {
            column.Item().Text("This document is confidential and forms part of the employee's disciplinary record.")
                .FontSize(8).FontColor(Colors.Grey.Darken1);
            
            column.Item().Text(text =>
            {
                text.Span("Generated on: ").FontSize(8).FontColor(Colors.Grey.Darken1);
                text.Span($"{DateTime.Now:dd MMM yyyy HH:mm}").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
            });
        });
    }
}
