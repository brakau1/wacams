#!/usr/bin/env python3
"""
WACAMS UAT (User Acceptance Testing) Automated Test Script

This script performs automated testing of critical WACAMS functionality
before production deployment.

Usage:
    python uat_test_script.py --env production --url https://wacams.company.com

Requirements:
    - Python 3.10+
    - requests library
    - Valid test user credentials
"""

import argparse
import json
import sys
import time
from datetime import datetime, timedelta
from typing import Dict, List, Optional, Tuple
import requests
import urllib3

# Suppress SSL warnings for testing
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)


class UATTestResult:
    """Test result container"""
    def __init__(self, test_name: str):
        self.test_name = test_name
        self.passed = False
        self.message = ""
        self.duration = 0.0
        self.timestamp = datetime.now()


class WACAMSTestSuite:
    """WACAMS UAT Test Suite"""
    
    def __init__(self, base_url: str, verify_ssl: bool = True):
        self.base_url = base_url.rstrip('/')
        self.api_url = f"{self.base_url}/api"
        self.verify_ssl = verify_ssl
        self.session = requests.Session()
        self.session.verify = verify_ssl
        self.tokens: Dict[str, str] = {}
        self.results: List[UATTestResult] = []
        
    def log(self, message: str):
        """Log message with timestamp"""
        timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
        print(f"[{timestamp}] {message}")
    
    def run_test(self, test_name: str, test_func) -> UATTestResult:
        """Run a single test and capture results"""
        result = UATTestResult(test_name)
        start_time = time.time()
        
        try:
            self.log(f"Running: {test_name}")
            passed, message = test_func()
            result.passed = passed
            result.message = message
        except Exception as e:
            result.passed = False
            result.message = f"Exception: {str(e)}"
        
        result.duration = time.time() - start_time
        self.results.append(result)
        
        status = "✓ PASS" if result.passed else "✗ FAIL"
        self.log(f"  {status}: {result.message} ({result.duration:.2f}s)")
        
        return result
    
    def authenticate_user(self, username: str, password: str, role: str) -> bool:
        """Authenticate user and store token"""
        try:
            response = self.session.post(
                f"{self.api_url}/auth/login",
                json={
                    "username": username,
                    "password": password
                },
                timeout=10
            )
            
            if response.status_code == 200:
                data = response.json()
                self.tokens[role] = data.get("token")
                return True
            return False
        except Exception as e:
            self.log(f"  Authentication failed: {str(e)}")
            return False
    
    # ═══════════════════════════════════════════════════════════════
    # TEST GROUP 1: Authentication & Authorization
    # ═══════════════════════════════════════════════════════════════
    
    def test_health_check(self) -> Tuple[bool, str]:
        """Test API health endpoint"""
        try:
            response = self.session.get(
                f"{self.api_url}/health",
                timeout=5
            )
            if response.status_code == 200:
                data = response.json()
                if data.get("status") == "healthy":
                    return True, "Health check passed"
            return False, f"Health check failed: {response.status_code}"
        except Exception as e:
            return False, f"Health check error: {str(e)}"
    
    def test_admin_login(self) -> Tuple[bool, str]:
        """Test admin user authentication"""
        # TODO: Replace with actual test credentials
        success = self.authenticate_user("admin@wacams.com", "Admin123!", "admin")
        if success:
            return True, "Admin login successful"
        return False, "Admin login failed"
    
    def test_hr_login(self) -> Tuple[bool, str]:
        """Test HR user authentication"""
        # TODO: Replace with actual test credentials
        success = self.authenticate_user("hr@wacams.com", "HR123!", "hr")
        if success:
            return True, "HR login successful"
        return False, "HR login failed"
    
    def test_manager_login(self) -> Tuple[bool, str]:
        """Test manager user authentication"""
        # TODO: Replace with actual test credentials
        success = self.authenticate_user("manager@wacams.com", "Manager123!", "manager")
        if success:
            return True, "Manager login successful"
        return False, "Manager login failed"
    
    def test_employee_login(self) -> Tuple[bool, str]:
        """Test employee user authentication"""
        # TODO: Replace with actual test credentials
        success = self.authenticate_user("employee@wacams.com", "Employee123!", "employee")
        if success:
            return True, "Employee login successful"
        return False, "Employee login failed"
    
    def test_invalid_login(self) -> Tuple[bool, str]:
        """Test login with invalid credentials"""
        try:
            response = self.session.post(
                f"{self.api_url}/auth/login",
                json={
                    "username": "invalid@wacams.com",
                    "password": "WrongPassword123!"
                },
                timeout=10
            )
            if response.status_code == 401:
                return True, "Invalid login correctly rejected"
            return False, f"Expected 401, got {response.status_code}"
        except Exception as e:
            return False, f"Invalid login test error: {str(e)}"
    
    def test_unauthorized_access(self) -> Tuple[bool, str]:
        """Test accessing protected endpoint without token"""
        try:
            response = self.session.get(
                f"{self.api_url}/attendance/records",
                timeout=5
            )
            if response.status_code == 401:
                return True, "Unauthorized access correctly blocked"
            return False, f"Expected 401, got {response.status_code}"
        except Exception as e:
            return False, f"Unauthorized access test error: {str(e)}"
    
    # ═══════════════════════════════════════════════════════════════
    # TEST GROUP 2: Attendance Management
    # ═══════════════════════════════════════════════════════════════
    
    def test_get_attendance_records(self) -> Tuple[bool, str]:
        """Test retrieving attendance records"""
        if "hr" not in self.tokens:
            return False, "HR token not available"
        
        try:
            headers = {"Authorization": f"Bearer {self.tokens['hr']}"}
            response = self.session.get(
                f"{self.api_url}/attendance/records",
                headers=headers,
                params={"limit": 10},
                timeout=10
            )
            if response.status_code == 200:
                data = response.json()
                if "records" in data:
                    return True, f"Retrieved {len(data['records'])} attendance records"
            return False, f"Failed to get records: {response.status_code}"
        except Exception as e:
            return False, f"Get attendance records error: {str(e)}"
    
    def test_clock_in(self) -> Tuple[bool, str]:
        """Test clock-in functionality"""
        if "employee" not in self.tokens:
            return False, "Employee token not available"
        
        try:
            headers = {"Authorization": f"Bearer {self.tokens['employee']}"}
            response = self.session.post(
                f"{self.api_url}/attendance/clock-in",
                headers=headers,
                json={
                    "timestamp": datetime.now().isoformat(),
                    "location": "UAT Test Location"
                },
                timeout=10
            )
            if response.status_code in [200, 201]:
                return True, "Clock-in successful"
            return False, f"Clock-in failed: {response.status_code}"
        except Exception as e:
            return False, f"Clock-in error: {str(e)}"
    
    def test_clock_out(self) -> Tuple[bool, str]:
        """Test clock-out functionality"""
        if "employee" not in self.tokens:
            return False, "Employee token not available"
        
        try:
            headers = {"Authorization": f"Bearer {self.tokens['employee']}"}
            response = self.session.post(
                f"{self.api_url}/attendance/clock-out",
                headers=headers,
                json={
                    "timestamp": datetime.now().isoformat(),
                    "location": "UAT Test Location"
                },
                timeout=10
            )
            if response.status_code in [200, 201]:
                return True, "Clock-out successful"
            return False, f"Clock-out failed: {response.status_code}"
        except Exception as e:
            return False, f"Clock-out error: {str(e)}"
    
    # ═══════════════════════════════════════════════════════════════
    # TEST GROUP 3: Warning System
    # ═══════════════════════════════════════════════════════════════
    
    def test_get_warnings(self) -> Tuple[bool, str]:
        """Test retrieving warnings"""
        if "hr" not in self.tokens:
            return False, "HR token not available"
        
        try:
            headers = {"Authorization": f"Bearer {self.tokens['hr']}"}
            response = self.session.get(
                f"{self.api_url}/warnings",
                headers=headers,
                params={"limit": 10},
                timeout=10
            )
            if response.status_code == 200:
                data = response.json()
                if "warnings" in data:
                    return True, f"Retrieved {len(data['warnings'])} warnings"
            return False, f"Failed to get warnings: {response.status_code}"
        except Exception as e:
            return False, f"Get warnings error: {str(e)}"
    
    def test_create_warning(self) -> Tuple[bool, str]:
        """Test creating a warning"""
        if "hr" not in self.tokens:
            return False, "HR token not available"
        
        try:
            headers = {"Authorization": f"Bearer {self.tokens['hr']}"}
            response = self.session.post(
                f"{self.api_url}/warnings",
                headers=headers,
                json={
                    "employeeId": "EMP001",
                    "warningType": "Verbal",
                    "reason": "UAT Test Warning",
                    "issuedDate": datetime.now().strftime("%Y-%m-%d"),
                    "description": "This is a test warning for UAT purposes"
                },
                timeout=10
            )
            if response.status_code in [200, 201]:
                return True, "Warning created successfully"
            return False, f"Failed to create warning: {response.status_code}"
        except Exception as e:
            return False, f"Create warning error: {str(e)}"
    
    # ═══════════════════════════════════════════════════════════════
    # TEST GROUP 4: Notification System
    # ═══════════════════════════════════════════════════════════════
    
    def test_send_test_email(self) -> Tuple[bool, str]:
        """Test email notification"""
        if "admin" not in self.tokens:
            return False, "Admin token not available"
        
        try:
            headers = {"Authorization": f"Bearer {self.tokens['admin']}"}
            response = self.session.post(
                f"{self.api_url}/notifications/test-email",
                headers=headers,
                json={
                    "to": "test@wacams.com",
                    "subject": "WACAMS UAT Test",
                    "body": f"This is a test email sent at {datetime.now().isoformat()}"
                },
                timeout=30
            )
            if response.status_code == 200:
                return True, "Test email sent successfully"
            return False, f"Failed to send test email: {response.status_code}"
        except Exception as e:
            return False, f"Send test email error: {str(e)}"
    
    def test_get_notifications(self) -> Tuple[bool, str]:
        """Test retrieving notifications"""
        if "employee" not in self.tokens:
            return False, "Employee token not available"
        
        try:
            headers = {"Authorization": f"Bearer {self.tokens['employee']}"}
            response = self.session.get(
                f"{self.api_url}/notifications",
                headers=headers,
                params={"limit": 10},
                timeout=10
            )
            if response.status_code == 200:
                data = response.json()
                if "notifications" in data:
                    return True, f"Retrieved {len(data['notifications'])} notifications"
            return False, f"Failed to get notifications: {response.status_code}"
        except Exception as e:
            return False, f"Get notifications error: {str(e)}"
    
    # ═══════════════════════════════════════════════════════════════
    # TEST GROUP 5: Dashboard & Reports
    # ═══════════════════════════════════════════════════════════════
    
    def test_get_dashboard_data(self) -> Tuple[bool, str]:
        """Test retrieving dashboard data"""
        if "manager" not in self.tokens:
            return False, "Manager token not available"
        
        try:
            headers = {"Authorization": f"Bearer {self.tokens['manager']}"}
            response = self.session.get(
                f"{self.api_url}/dashboard/summary",
                headers=headers,
                timeout=10
            )
            if response.status_code == 200:
                data = response.json()
                if "summary" in data:
                    return True, "Dashboard data retrieved successfully"
            return False, f"Failed to get dashboard data: {response.status_code}"
        except Exception as e:
            return False, f"Get dashboard data error: {str(e)}"
    
    def test_generate_report(self) -> Tuple[bool, str]:
        """Test generating a report"""
        if "hr" not in self.tokens:
            return False, "HR token not available"
        
        try:
            headers = {"Authorization": f"Bearer {self.tokens['hr']}"}
            response = self.session.post(
                f"{self.api_url}/reports/generate",
                headers=headers,
                json={
                    "reportType": "AttendanceSummary",
                    "startDate": (datetime.now() - timedelta(days=30)).strftime("%Y-%m-%d"),
                    "endDate": datetime.now().strftime("%Y-%m-%d"),
                    "format": "PDF"
                },
                timeout=30
            )
            if response.status_code in [200, 201, 202]:
                return True, "Report generation initiated"
            return False, f"Failed to generate report: {response.status_code}"
        except Exception as e:
            return False, f"Generate report error: {str(e)}"
    
    # ═══════════════════════════════════════════════════════════════
    # TEST GROUP 6: GIS Integration
    # ═══════════════════════════════════════════════════════════════
    
    def test_gis_connection(self) -> Tuple[bool, str]:
        """Test GIS system connectivity"""
        if "admin" not in self.tokens:
            return False, "Admin token not available"
        
        try:
            headers = {"Authorization": f"Bearer {self.tokens['admin']}"}
            response = self.session.get(
                f"{self.api_url}/gis/status",
                headers=headers,
                timeout=15
            )
            if response.status_code == 200:
                data = response.json()
                if data.get("connected"):
                    return True, "GIS system connected"
                return False, "GIS system not connected"
            return False, f"GIS status check failed: {response.status_code}"
        except Exception as e:
            return False, f"GIS connection test error: {str(e)}"
    
    def run_all_tests(self):
        """Run all UAT tests"""
        self.log("=" * 70)
        self.log("WACAMS UAT Test Suite")
        self.log("=" * 70)
        self.log(f"Target URL: {self.base_url}")
        self.log(f"SSL Verification: {'Enabled' if self.verify_ssl else 'Disabled'}")
        self.log("=" * 70)
        
        # Group 1: Authentication & Authorization
        self.log("\n[Group 1] Authentication & Authorization")
        self.run_test("Health Check", self.test_health_check)
        self.run_test("Admin Login", self.test_admin_login)
        self.run_test("HR Login", self.test_hr_login)
        self.run_test("Manager Login", self.test_manager_login)
        self.run_test("Employee Login", self.test_employee_login)
        self.run_test("Invalid Login Rejection", self.test_invalid_login)
        self.run_test("Unauthorized Access Block", self.test_unauthorized_access)
        
        # Group 2: Attendance Management
        self.log("\n[Group 2] Attendance Management")
        self.run_test("Get Attendance Records", self.test_get_attendance_records)
        self.run_test("Clock In", self.test_clock_in)
        self.run_test("Clock Out", self.test_clock_out)
        
        # Group 3: Warning System
        self.log("\n[Group 3] Warning System")
        self.run_test("Get Warnings", self.test_get_warnings)
        self.run_test("Create Warning", self.test_create_warning)
        
        # Group 4: Notification System
        self.log("\n[Group 4] Notification System")
        self.run_test("Send Test Email", self.test_send_test_email)
        self.run_test("Get Notifications", self.test_get_notifications)
        
        # Group 5: Dashboard & Reports
        self.log("\n[Group 5] Dashboard & Reports")
        self.run_test("Get Dashboard Data", self.test_get_dashboard_data)
        self.run_test("Generate Report", self.test_generate_report)
        
        # Group 6: GIS Integration
        self.log("\n[Group 6] GIS Integration")
        self.run_test("GIS Connection", self.test_gis_connection)
        
        # Summary
        self.log("\n" + "=" * 70)
        self.log("Test Summary")
        self.log("=" * 70)
        
        total = len(self.results)
        passed = sum(1 for r in self.results if r.passed)
        failed = total - passed
        total_duration = sum(r.duration for r in self.results)
        
        self.log(f"Total Tests: {total}")
        self.log(f"Passed: {passed} ({passed/total*100:.1f}%)")
        self.log(f"Failed: {failed} ({failed/total*100:.1f}%)")
        self.log(f"Total Duration: {total_duration:.2f}s")
        self.log("=" * 70)
        
        if failed > 0:
            self.log("\nFailed Tests:")
            for result in self.results:
                if not result.passed:
                    self.log(f"  - {result.test_name}: {result.message}")
        
        return failed == 0


def main():
    parser = argparse.ArgumentParser(description="WACAMS UAT Test Suite")
    parser.add_argument(
        "--url",
        required=True,
        help="Base URL of WACAMS application (e.g., https://wacams.company.com)"
    )
    parser.add_argument(
        "--no-verify-ssl",
        action="store_true",
        help="Disable SSL verification (for testing only)"
    )
    parser.add_argument(
        "--output",
        help="Output results to JSON file"
    )
    
    args = parser.parse_args()
    
    # Run test suite
    suite = WACAMSTestSuite(args.url, verify_ssl=not args.no_verify_ssl)
    success = suite.run_all_tests()
    
    # Output results to JSON if requested
    if args.output:
        results_data = {
            "timestamp": datetime.now().isoformat(),
            "target_url": args.url,
            "total_tests": len(suite.results),
            "passed": sum(1 for r in suite.results if r.passed),
            "failed": sum(1 for r in suite.results if not r.passed),
            "results": [
                {
                    "test_name": r.test_name,
                    "passed": r.passed,
                    "message": r.message,
                    "duration": r.duration,
                    "timestamp": r.timestamp.isoformat()
                }
                for r in suite.results
            ]
        }
        
        with open(args.output, 'w', encoding='utf-8') as f:
            json.dump(results_data, f, indent=2, ensure_ascii=False)
        
        print(f"\nResults saved to: {args.output}")
    
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
