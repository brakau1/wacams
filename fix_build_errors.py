#!/usr/bin/env python3
"""
Comprehensive script to fix WACAMS build errors.
Addresses property name mismatches, missing usings, and type conversions.
"""

import os
import re
from pathlib import Path

def fix_file(filepath: str, replacements: list[tuple[str, str]]):
    """Apply a list of regex replacements to a file."""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        original = content
        for pattern, replacement in replacements:
            content = re.sub(pattern, replacement, content)
        
        if content != original:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            print(f"Fixed: {filepath}")
            return True
        return False
    except Exception as e:
        print(f"Error processing {filepath}: {e}")
        return False

def add_using_statement(filepath: str, using_statement: str):
    """Add a using statement if not already present."""
    try:
        with open(filepath, 'r', encoding='utf-8') as f:
            content = f.read()
        
        if using_statement in content:
            return False
        
        # Find the last using statement or namespace declaration
        lines = content.split('\n')
        insert_pos = 0
        for i, line in enumerate(lines):
            if line.startswith('using '):
                insert_pos = i + 1
            elif line.startswith('namespace ') and insert_pos > 0:
                break
        
        if insert_pos > 0:
            lines.insert(insert_pos, using_statement)
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write('\n'.join(lines))
            print(f"Added using to: {filepath}")
            return True
        return False
    except Exception as e:
        print(f"Error adding using to {filepath}: {e}")
        return False

def main():
    base_path = Path(r"C:\Users\brakau1\.curi\defaultWorkspace\Workforce Attendance")
    
    # Common property name fixes
    property_fixes = [
        # AttendanceRecord properties
        (r'\.ClockIn\b', '.ClockInTime'),
        (r'\.ClockOut\b', '.ClockOutTime'),
        (r'\.MinutesLate\b', '.LateMinutes'),
        (r'\.MinutesEarly\b', '.EarlyMinutes'),
        (r'\.IsLate\b', '.LateMinutes > 0'),
        (r'\.IsEarlyDeparture\b', '.EarlyMinutes > 0'),
        
        # AuditLog/SecurityEvent timestamp
        (r'\.Timestamp\b', '.PerformedAt'),
        
        # Employee properties
        (r'\.FirstName\b', '.DisplayName'),
        (r'\.LastName\b', ''),
        (r'\.FullName\b', '.DisplayName'),
        (r'\.PhoneNumber\b', '.Phone'),
        
        # Escalation properties
        (r'\.ResponsibleManagerId\b', '.AssignedTo'),
        (r'\.AssignedToManagerId\b', '.AssignedTo'),
        
        # WarningLetter properties
        (r'\.WarningType\b', '.Type'),
        (r'\.IssueDate\b', '.DateIssued'),
    ]
    
    # EscalationStatus enum fixes
    enum_fixes = [
        (r'EscalationStatus\.Open\b', 'EscalationStatus.Pending'),
        (r'EscalationStatus\.Resolved\b', 'EscalationStatus.Closed'),
        (r'EscalationStatus\.Acknowledged\b', 'EscalationStatus.Accepted'),
        (r'EscalationStatus\.ActionRequired\b', 'EscalationStatus.ActionTaken'),
    ]
    
    # ExceptionStatus to InvestigationStatus
    exception_status_fixes = [
        (r'ExceptionStatus\.Open\b', 'InvestigationStatus.Open'),
        (r'ExceptionStatus\.UnderReview\b', 'InvestigationStatus.UnderReview'),
        (r'ExceptionStatus\.Investigated\b', 'InvestigationStatus.Investigated'),
        (r'ExceptionStatus\.Closed\b', 'InvestigationStatus.Closed'),
    ]
    
    # Files that need NotFoundException/ConflictException using
    files_needing_exceptions = [
        "Api/Services/AttendanceService.cs",
        "Api/Services/AuthService.cs",
        "Api/Services/DashboardService.cs",
        "Api/Services/EmployeeService.cs",
        "Api/Services/EscalationService.cs",
        "Api/Services/ExceptionService.cs",
        "Api/Services/ReportService.cs",
    ]
    
    # Apply fixes to all C# files
    api_path = base_path / "Api"
    for cs_file in api_path.rglob("*.cs"):
        # Apply property fixes
        fix_file(str(cs_file), property_fixes)
        
        # Apply enum fixes
        fix_file(str(cs_file), enum_fixes)
        
        # Apply exception status fixes
        fix_file(str(cs_file), exception_status_fixes)
    
    # Add using statements for exceptions
    for rel_path in files_needing_exceptions:
        filepath = base_path / rel_path
        if filepath.exists():
            add_using_statement(str(filepath), "using WACAMS.Api.Middleware;")
    
    # Fix specific LogActionAsync calls (remove actorId parameter)
    for cs_file in api_path.rglob("*.cs"):
        try:
            with open(cs_file, 'r', encoding='utf-8') as f:
                content = f.read()
            
            # Remove actorId: parameter from LogActionAsync calls
            new_content = re.sub(
                r'LogActionAsync\([^)]*actorId:\s*[^,]+,\s*',
                'LogActionAsync(',
                content
            )
            
            if new_content != content:
                with open(cs_file, 'w', encoding='utf-8') as f:
                    f.write(new_content)
                print(f"Fixed LogActionAsync in: {cs_file}")
        except Exception as e:
            print(f"Error fixing LogActionAsync in {cs_file}: {e}")
    
    # Fix DepartmentId type conversions (Guid vs int)
    # This is complex - need to handle case by case
    print("\nNote: DepartmentId type mismatches (Guid vs int) require manual review.")
    print("The entities use Guid, but some services/controllers use int.")
    
    # Fix SecurityOperationsService Timestamp references
    security_ops = base_path / "Api/Services/SecurityOperationsService.cs"
    if security_ops.exists():
        fix_file(str(security_ops), [
            (r'\.Timestamp\b', '.OccurredAt'),
        ])
    
    # Fix AccessReviewService
    access_review = base_path / "Api/Services/AccessReviewService.cs"
    if access_review.exists():
        fix_file(str(access_review), [
            (r'\.Timestamp\b', '.PerformedAt'),
        ])
    
    # Fix GISAdminService
    gis_admin = base_path / "Api/Services/GISAdminService.cs"
    if gis_admin.exists():
        fix_file(str(gis_admin), [
            (r'\.Timestamp\b', '.PerformedAt'),
        ])
    
    print("\nFix script completed.")

if __name__ == "__main__":
    main()
