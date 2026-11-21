using WACAMS.Core.Disciplinary.Models;

namespace WACAMS.Infrastructure.Services.Pdf;

/// <summary>
/// Auto-generates professional narratives for corrective action forms
/// </summary>
public class CorrectiveActionNarrativeGenerator
{
    /// <summary>
    /// Generates all narrative sections for a corrective action form
    /// </summary>
    public (string situation, string reasons, string causes, string action, string sanction) 
        GenerateNarratives(CorrectiveAction action, string employeeName, string department)
    {
        var offenceType = action.OffenceType;
        var offenceNumber = action.OffenceNumber;
        var issuedDate = action.IssuedAt.ToString("dd MMM yyyy");
        
        var situation = GenerateSituationDescription(offenceType, employeeName, department, issuedDate);
        var reasons = GenerateReasonsSection(offenceType, employeeName);
        var causes = GenerateCausesSection(offenceType);
        var actionTaken = GenerateActionTaken(offenceType, offenceNumber);
        var sanction = GenerateRecommendedSanction(offenceType, offenceNumber, action.Level);
        
        return (situation, reasons, causes, actionTaken, sanction);
    }
    
    /// <summary>
    /// Generates the "Describe Situation" section
    /// </summary>
    private string GenerateSituationDescription(OffenceType offenceType, string employeeName, 
        string department, string issuedDate)
    {
        return offenceType switch
        {
            OffenceType.LateArrival => $"""
                On {issuedDate}, {employeeName} from the {department} department arrived late for their scheduled shift. 
                The employee's scheduled start time was not met, resulting in lost productivity and disruption to team operations. 
                This incident was documented and reviewed by management. Attendance records confirm the late arrival, and the 
                employee was notified of this corrective action meeting.
                """,
                
            OffenceType.EarlyDeparture => $"""
                On {issuedDate}, {employeeName} from the {department} department departed work before their scheduled end time 
                without proper authorization. The employee left the workstation early, which impacted workflow continuity and 
                placed additional burden on remaining team members. This incident was documented and reviewed by management. 
                Attendance records confirm the early departure, and the employee was notified of this corrective action meeting.
                """,
                
            OffenceType.AWOL => $"""
                On {issuedDate}, {employeeName} from the {department} department was absent from work without notice or 
                authorization (Absent Without Leave). The employee failed to report for their scheduled shift and did not 
                communicate their absence to supervisors. This unexplained absence disrupted operations and violated company 
                attendance policies. The incident was documented, and the employee was notified of this corrective action meeting 
                upon their return.
                """,
                
            OffenceType.UnauthorizedAbsence => $"""
                On {issuedDate}, {employeeName} from the {department} department was absent from work without proper 
                authorization or approved leave. The employee did not follow the required call-in procedures and failed to 
                provide documentation for their absence. This unauthorized absence violated company attendance policies and 
                created operational challenges for the team. The incident was documented, and the employee was notified of 
                this corrective action meeting.
                """,
                
            _ => $"""
                On {issuedDate}, {employeeName} from the {department} department was involved in an attendance-related 
                incident that requires corrective action. The specific details of the incident have been documented and 
                reviewed by management. The employee was notified of this corrective action meeting to discuss the matter 
                and determine appropriate next steps.
                """
        };
    }
    
    /// <summary>
    /// Generates the "Ask For Reasons" section
    /// </summary>
    private string GenerateReasonsSection(OffenceType offenceType, string employeeName)
    {
        return offenceType switch
        {
            OffenceType.LateArrival => $"""
                {employeeName} was asked to explain the reasons for their late arrival. The employee was given the opportunity 
                to provide their perspective on the circumstances that led to this incident. [Employee's explanation to be 
                documented here during the meeting].
                """,
                
            OffenceType.EarlyDeparture => $"""
                {employeeName} was asked to explain the reasons for leaving work early without authorization. The employee 
                was given the opportunity to provide their perspective on the circumstances that led to this incident. 
                [Employee's explanation to be documented here during the meeting].
                """,
                
            OffenceType.AWOL => $"""
                {employeeName} was asked to explain the reasons for their absence without notice or authorization. The 
                employee was given the opportunity to provide their perspective on the circumstances that led to this 
                incident. [Employee's explanation to be documented here during the meeting].
                """,
                
            OffenceType.UnauthorizedAbsence => $"""
                {employeeName} was asked to explain the reasons for their unauthorized absence and failure to follow 
                call-in procedures. The employee was given the opportunity to provide their perspective on the circumstances 
                that led to this incident. [Employee's explanation to be documented here during the meeting].
                """,
                
            _ => $"""
                {employeeName} was asked to explain the circumstances surrounding this incident. The employee was given 
                the opportunity to provide their perspective and any relevant context. [Employee's explanation to be 
                documented here during the meeting].
                """
        };
    }
    
    /// <summary>
    /// Generates the "Discuss Causes" section
    /// </summary>
    private string GenerateCausesSection(OffenceType offenceType)
    {
        return offenceType switch
        {
            OffenceType.LateArrival => """
                The following contributing factors were discussed:
                • Time management and planning challenges
                • Transportation or commute issues
                • Personal circumstances affecting punctuality
                • Understanding of attendance expectations and policies
                • Impact of tardiness on team operations and productivity
                """,
                
            OffenceType.EarlyDeparture => """
                The following contributing factors were discussed:
                • Understanding of work schedule requirements
                • Communication breakdown regarding departure procedures
                • Personal circumstances affecting work attendance
                • Awareness of authorization requirements for early departure
                • Impact on team workflow and colleague workload
                """,
                
            OffenceType.AWOL => """
                The following contributing factors were discussed:
                • Communication procedures for reporting absences
                • Understanding of call-in requirements and protocols
                • Personal or emergency circumstances
                • Awareness of attendance policy consequences
                • Impact of unexplained absence on operations and team
                """,
                
            OffenceType.UnauthorizedAbsence => """
                The following contributing factors were discussed:
                • Leave request and approval procedures
                • Understanding of authorization requirements
                • Communication breakdown with supervisors
                • Personal circumstances affecting attendance
                • Awareness of company attendance policies and expectations
                """,
                
            _ => """
                The following contributing factors were discussed:
                • Understanding of company policies and procedures
                • Communication and documentation requirements
                • Personal circumstances affecting work attendance
                • Awareness of expectations and consequences
                • Impact on team operations and productivity
                """
        };
    }
    
    /// <summary>
    /// Generates the "Action Taken" section
    /// </summary>
    private string GenerateActionTaken(OffenceType offenceType, int offenceNumber)
    {
        var offenceOrdinal = GetOrdinalNumber(offenceNumber);
        
        return offenceType switch
        {
            OffenceType.LateArrival => $"""
                This is the {offenceOrdinal} occurrence of late arrival for this employee. The following actions have been taken:
                • Formal discussion of attendance expectations and company policy
                • Review of work schedule and punctuality requirements
                • Documentation of this incident in the employee's record
                • Establishment of clear expectations for future attendance
                • Agreement on follow-up date to monitor improvement
                """,
                
            OffenceType.EarlyDeparture => $"""
                This is the {offenceOrdinal} occurrence of early departure for this employee. The following actions have been taken:
                • Formal discussion of work schedule requirement and departure procedures
                • Review of authorization requirements for leaving work early
                • Documentation of this incident in the employee's record
                • Establishment of clear expectations for future compliance
                • Agreement on follow-up date to monitor improvement
                """,
                
            OffenceType.AWOL => $"""
                This is the {offenceOrdinal} occurrence of AWOL (Absent Without Leave) for this employee. The following actions 
                have been taken:
                • Formal discussion of attendance policies and call-in procedures
                • Review of communication requirements for absences
                • Documentation of this incident in the employee's record
                • Establishment of clear expectations for future attendance and communication
                • Agreement on follow-up date to monitor improvement
                """,
                
            OffenceType.UnauthorizedAbsence => $"""
                This is the {offenceOrdinal} occurrence of unauthorized absence for this employee. The following actions have 
                been taken:
                • Formal discussion of leave request and authorization procedures
                • Review of attendance policies and communication requirements
                • Documentation of this incident in the employee's record
                • Establishment of clear expectations for future compliance
                • Agreement on follow-up date to monitor improvement
                """,
                
            _ => $"""
                This is the {offenceOrdinal} occurrence of this type of incident for this employee. The following actions have 
                been taken:
                • Formal discussion of relevant policies and expectations
                • Review of procedures and requirements
                • Documentation of this incident in the employee's record
                • Establishment of clear expectations for future compliance
                • Agreement on follow-up date to monitor improvement
                """
        };
    }
    
    /// <summary>
    /// Generates the "Recommended Sanction" section
    /// </summary>
    private string GenerateRecommendedSanction(OffenceType offenceType, int offenceNumber, 
        DisciplinaryLevel level)
    {
        var offenceOrdinal = GetOrdinalNumber(offenceNumber);
        var levelDescription = GetDisciplinaryLevelDescription(level);
        
        return $"""
            Based on the {offenceOrdinal} occurrence and the progressive discipline policy, the following sanction is 
            recommended:
            
            Disciplinary Level: {levelDescription}
            
            This sanction is in accordance with the company's progressive discipline policy and takes into account the 
            severity of the offence, the employee's disciplinary history, and the impact on operations. The employee has 
            been informed of their right to appeal this decision through the appropriate channels.
            
            Future occurrences may result in escalated disciplinary action, up to and including termination of employment.
            """;
    }
    
    /// <summary>
    /// Converts number to ordinal string (1st, 2nd, 3rd, etc.)
    /// </summary>
    private string GetOrdinalNumber(int number)
    {
        if (number <= 0) return number.ToString();
        
        var lastDigit = number % 10;
        var lastTwoDigits = number % 100;
        
        if (lastTwoDigits >= 11 && lastTwoDigits <= 13)
            return $"{number}th";
        
        return lastDigit switch
        {
            1 => $"{number}st",
            2 => $"{number}nd",
            3 => $"{number}rd",
            _ => $"{number}th"
        };
    }
    
    /// <summary>
    /// Gets human-readable description of disciplinary level
    /// </summary>
    private string GetDisciplinaryLevelDescription(DisciplinaryLevel level)
    {
        return level switch
        {
            DisciplinaryLevel.None => "No Disciplinary Action",
            DisciplinaryLevel.Counselling => "Counselling",
            DisciplinaryLevel.VerbalWarning => "Verbal Warning",
            DisciplinaryLevel.WrittenWarning => "Written Warning",
            DisciplinaryLevel.FinalWrittenWarning => "Final Written Warning",
            DisciplinaryLevel.DismissalHearing => "Dismissal Hearing",
            DisciplinaryLevel.Dismissal => "Dismissal",
            _ => "Unknown"
        };
    }
}
