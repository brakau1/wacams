# WACAMS — Workforce Attendance & Corrective Action Management System

> Production-ready workforce attendance monitoring and disciplinary action management system.

![WACAMS](https://img.shields.io/badge/.NET-8.0-blue)
![React](https://img.shields.io/badge/React-19-blue)
![TypeScript](https://img.shields.io/badge/TypeScript-5-blue)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Quick Start (Docker)](#quick-start-docker)
- [Local Development Setup](#local-development-setup)
  - [Backend](#backend)
  - [Frontend](#frontend)
- [Database Setup](#database-setup)
- [Configuration](#configuration)
- [API Endpoints](#api-endpoints)
- [RBAC Roles](#rbac-roles)
- [Deployment](#deployment)
  - [Docker Compose](#docker-compose)
  - [Azure App Service](#azure-app-service)
- [Core Business Rules](#core-business-rules)
- [Troubleshooting](#troubleshooting)

---

## Overview

WACAMS is a comprehensive workforce attendance management system designed for manufacturing environments. It monitors employee attendance in real-time, automatically detects exceptions, tracks corrective actions through a structured escalation chain, and generates audit-compliant documentation.

### Key Features

- **Real-time Attendance Monitoring** — Clock in/out tracking with automatic late/early detection
- **Exception Management** — Track 12+ exception types through investigation workflow
- **Disciplinary Actions** — Counselling → Verbal Warning → Written Warning → Final Written Warning
- **Supervisor Escalation Chain** — Automatic escalation when disciplinary actions aren't completed within 24 hours
- **Signed Warning Forms** — Upload and store signed PDF warning documents
- **Comprehensive Reporting** — Department-level and individual attendance reports with CSV export
- **Full Audit Trail** — Every change logged with old/new values, performer, timestamp, and IP address
- **PWA Support** — Installable mobile experience for clock-in/out on the factory floor

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                    Frontend (React)                  │
│   React + TypeScript + Tailwind CSS + ShadCN UI     │
│   PWA | SWR Data Fetching | Role-based Routing      │
└──────────────────────┬──────────────────────────────┘
                       │ HTTP/REST (JWT Bearer)
                       ▼
┌─────────────────────────────────────────────────────┐
│                   Backend API (.NET 8)               │
│   Controllers │ Services │ Middleware │ JWT Auth     │
│   RBAC Authorization │ Global Exception Handling     │
└──────────────────────┬──────────────────────────────┘
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
    ┌──────────┐ ┌──────────┐ ┌──────────┐
    │   Core   │ │ Infrastr.│ │  Api     │
    │ Entities │ │ EF Core  │ │Services  │
    │ Enums    │ │ Repos    │ │ JWT/RBAC │
    │ DTOs     │ │ Migrations│ │ Audit   │
    │ Interfaces│ │ SeedData │ │ Reports │
    └──────────┘ └──────────┘ └──────────┘
                       │
                       ▼
              ┌─────────────────┐
              │ PostgreSQL 16   │
              │ • All Tables    │
              │ • Migrations    │
              │ • Seed Data     │
              └─────────────────┘
```

### Clean Architecture Layers

| Layer | Project | Responsibility |
|-------|---------|----------------|
| **Core** | `WACAMS.Core` | Domain entities, enums, interfaces, DTOs, value objects — business rules only |
| **Infrastructure** | `WACAMS.Infrastructure` | EF Core DbContext, repositories, migrations, seed data, calculations |
| **Api** | `WACAMS.Api` | REST endpoints, authentication, authorization, middleware, services |

---

## Tech Stack

| Component | Technology | Version |
|-----------|-----------|---------|
| **Backend Framework** | ASP.NET Core Web API | .NET 8 LTS |
| **ORM** | Entity Framework Core | 8.x |
| **Database** | PostgreSQL | 16 |
| **Authentication** | JWT Bearer + BCrypt | Microsoft.AspNetCore.Authentication.JwtBearer |
| **Frontend Framework** | React | 19 |
| **Language** | TypeScript | 5.x |
| **Styling** | Tailwind CSS + ShadCN UI | Latest |
| **Build Tool** | Vite | 6.x |
| **Data Fetching** | SWR | 2.x |
| **Routing** | React Router | 6.x |
| **Package Manager** | npm | 10.x |
| **Containerization** | Docker + Docker Compose | Latest |
| **Logging** | Serilog | 3.x |
| **API Documentation** | Swagger/OpenAPI | Swashbuckle |

---

## Project Structure

```
Workforce Attendance/
├── Core/                              # Domain layer (no dependencies)
│   ├── Entities/                      # Employee, AttendanceRecord, WarningLetter, etc.
│   ├── Enums/                         # AttendanceStatus, WarningType, Role, etc.
│   ├── Interfaces/                    # IRepository, IUnitOfWork, domain-specific repos
│   ├── DTOs/                          # Request/response data transfer objects
│   ├── Services/                      # Domain service interfaces
│   ├── ValueObjects/                  # AttendancePeriod, WorkDuration
│   ├── Constants/                     # ShiftConstants, WarningThresholds
│   └── WACAMS.Core.csproj
│
├── Infrastructure/                    # Persistence layer
│   ├── Data/                          # WacamContext, SeedData
│   ├── Repositories/                  # Repository<T>, specific repo implementations
│   ├── Services/                      # Calculation services, escalation logic
│   ├── Persistence/Configuration/     # FluentAPI entity configurations
│   ├── Persistence/Indexes/           # Database index definitions
│   ├── Extensions/                    # ServiceCollection extensions
│   └── WACAMS.Infrastructure.csproj
│
├── Api/                               # Presentation/API layer
│   ├── Controllers/                   # REST endpoints (Auth, Employees, Attendance, etc.)
│   ├── Services/                      # Application services orchestrating use cases
│   ├── Middleware/                    # Exception handling, auth, audit logging
│   ├── Extensions/                    # DI registration extensions
│   ├── Filters/                       # Attribute filters
│   ├── Utils/                         # Password hasher, token generator, helpers
│   ├── appsettings.json               # Configuration
│   ├── Program.cs                     # Application startup
│   └── WACAMS.Api.csproj
│
├── Frontend/                          # React SPA
│   ├── src/
│   │   ├── core/                      # Auth context, API client, hooks, theme, utils
│   │   ├── features/                  # Feature modules (auth, dashboard, employees, etc.)
│   │   ├── shared/                    # Layout, DataTable, Modal, Toast, Sidebar, etc.
│   │   ├── pages/                     # NotFoundPage, redirects
│   │   ├── App.tsx                    # Route configuration
│   │   ├── main.tsx                   # Entry point
│   │   └── index.css                  # Tailwind + custom theme
│   ├── public/                        # PWA manifest, icons, favicon
│   ├── vite.config.ts                 # Build config with PWA plugin
│   ├── tailwind.config.js             # Theme customization
│   └── package.json                   # Dependencies
│
├── azure/                             # Azure deployment configs
│   ├── backend.deploy.yaml
│   └── frontend.deploy.yaml
│
├── Dockerfile.backend                 # Multi-stage .NET build
├── Dockerfile.frontend                # Node build + nginx serve
├── docker-compose.yml                 # Full stack orchestration
├── nginx.conf                         # Reverse proxy + SPA routing
├── .gitignore
└── README.md
```

---

## Prerequisites

| Software | Minimum Version | Notes |
|----------|----------------|-------|
| [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0.x | For building and running the backend |
| [Node.js](https://nodejs.org/) | 20.x LTS | For building and running the frontend |
| [PostgreSQL](https://www.postgresql.org/) | 16.x | Or use Docker Compose (included) |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | Latest | Optional — for containerized development |
| [Git](https://git-scm.com/) | Latest | For version control |

---

## Quick Start (Docker)

The fastest way to get everything running:

```bash
# Navigate to project root
cd "Workforce Attendance"

# Set environment variables (or use defaults)
set DB_PASSWORD=WacamStr0ng!2024
set JWT_KEY=WACAMS-Production-JWT-Secret-Key-MustBe-Very-Long!

# Start all services (PostgreSQL, Backend, Frontend, Adminer)
docker-compose up -d

# Wait for database to initialize (~30 seconds)
# The backend will auto-run EF Core migrations on first start

# Access the application:
# Frontend:  http://localhost:80
# Backend:   http://localhost:8080/api/health
# Adminer:   http://localhost:8081 (for database management)
```

### First-Time Login

After the database initializes, a default super admin user is created via seed data:

| Field | Value |
|-------|-------|
| Username | `admin` |
| Password | `Admin@2024` |
| Role | SuperAdmin |
| Employee ID | `ADMIN001` |

**Change this password immediately after first login!**

---

## Local Development Setup

### Backend

```bash
# Navigate to the Api project
cd Api

# Restore NuGet packages
dotnet restore

# Update connection string in appsettings.json or appsettings.Development.json:
# "ConnectionStrings": {
#   "WacamDb": "Host=localhost;Port=5432;Database=wacam_db;Username=wacam_user;Password=your_password;"
# }

# Run EF Core migrations to create/update database schema
dotnet ef database update --project ../Infrastructure/WACAMS.Infrastructure.csproj --startup-project Api.csproj

# Start the API server (with hot reload)
dotnet run --launch-profile Development

# API available at: http://localhost:5000/api/health
# Swagger UI at: http://localhost:5000/swagger
```

### Frontend

```bash
# Navigate to the Frontend project
cd Frontend

# Install dependencies
npm install

# Start development server with hot module replacement
npm run dev

# Frontend available at: http://localhost:5173
```

#### Building for Production

```bash
cd Frontend
npm run build        # Optimized production build
npm run preview      # Preview production build locally
```

---

## Database Setup

### Manual Setup (without Docker)

```sql
-- Create database
CREATE DATABASE wacam_db;
CREATE USER wacam_user WITH PASSWORD 'your_strong_password';
GRANT ALL PRIVILEGES ON DATABASE wacam_db TO wacam_user;

-- Connect to the database
\c wacam_db

-- Apply EF Core migrations from the Infrastructure project
# In the Infrastructure project directory:
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Database Schema Overview

| Table | Description | Key Fields |
|-------|-------------|------------|
| `employees` | Employee master records | `employee_id` (PK string), `department_id`, `manager_id` |
| `departments` | Department definitions | `code`, `name`, `attendance_target_percentage` |
| `users` | Authentication accounts | `employee_id` (FK), `username`, `password_hash`, `role` |
| `attendance_records` | Daily clock-in/out data | `employee_id`, `date`, `clock_in_time`, `clock_out_time` |
| `attendance_exceptions` | Tracked violations | `employee_id`, `exception_type`, `investigation_status` |
| `warning_letters` | Disciplinary actions | `employee_id`, `type`, `signed` |
| `escalations` | Supervisory escalation chain | `warning_letter_id`, `level`, `status` |
| `shift_schedules` | Per-day shift assignments | `employee_id`, `date`, `shift_code` |
| `audit_logs` | Immutable change history | `entity_type`, `action`, `old_value`, `new_value` |

### Entity Relationship Diagram

```
departments 1──N employees N──1 users
                │
                ├── N attendance_records
                ├── N attendance_exceptions
                ├── N warning_letters
                │       │
                │       └── N escalations
                ├── N shift_schedules
                └── N direct_reports (self-referencing via manager_id)

audit_logs ← all tables (polymorphic reference)
```

---

## Configuration

### Backend (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "WacamDb": "Host=localhost;Port=5432;Database=wacam_db;Username=wacam_user;Password=your_password;"
  },
  "Jwt": {
    "Key": "WACAMS-Default-JWT-Secret-Key-MustBe-At-Least-32-Characters!",
    "Issuer": "WACAMS",
    "Audience": "WACAMS-API"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Frontend Environment Variables

Create `.env.local` in the `Frontend/` directory:

```env
VITE_API_BASE_URL=http://localhost:5000/api
VITE_APP_NAME="WACAMS"
VITE_PWA_ENABLED=true
```

---

## API Endpoints

### Authentication
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/auth/login` | Authenticate and receive JWT tokens | No |
| POST | `/api/auth/register` | Register new user (Admin/SuperAdmin only) | Yes (Admin+) |
| POST | `/api/auth/refresh` | Refresh expired access token | No |
| POST | `/api/auth/logout` | Invalidate refresh token | Yes |
| PUT | `/api/auth/change-password` | Change current user's password | Yes |

### Employees
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/employees` | List/search employees with filters | Yes |
| GET | `/api/employees/{id}` | Get employee detail with hierarchy | Yes |
| PUT | `/api/employees/{id}` | Update employee information | Yes (Supervisor+) |
| POST | `/api/employees/bulk` | Bulk upload employees (CSV) | Yes (Admin+) |

### Attendance
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| POST | `/api/attendance/clockin` | Record clock-in event | Yes |
| POST | `/api/attendance/clockout` | Record clock-out event | Yes |
| GET | `/api/attendance/daily` | Get daily attendance for all employees | Yes |
| GET | `/api/attendance/summary` | Aggregated attendance summary | Yes |

### Exceptions
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/exceptions` | List exceptions with filters | Yes |
| POST | `/api/exceptions` | Create new exception | Yes (TeamLeader+) |
| PUT | `/api/exceptions/{id}/investigate` | Mark exception as investigated | Yes (Supervisor+) |

### Warnings
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/warnings` | List warning letters | Yes |
| POST | `/api/warnings/generate` | Generate warning letter | Yes (Supervisor+) |
| POST | `/api/warnings/{id}/upload-signed` | Upload signed warning form | Yes (Supervisor+) |

### Escalations
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/escalations` | List all escalations | Yes |
| PUT | `/api/escalations/{id}/progress` | Progress an escalation step | Yes |

### Reports
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/reports/attendance-summary` | Generate attendance report | Yes |
| GET | `/api/reports/export-csv` | Export report to CSV | Yes |

### Audit
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/audit` | View complete audit trail | Yes (Admin+) |

### Admin
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/admin/users` | List all users | Yes (Admin+) |
| PUT | `/api/admin/users/{id}/toggle` | Enable/disable user | Yes (SuperAdmin) |
| GET | `/api/departments` | List departments | Yes |
| PUT | `/api/departments/{id}` | Update department settings | Yes (Admin+) |
| GET | `/api/settings` | Get system settings | Yes (SuperAdmin) |
| PUT | `/api/settings` | Update system settings | Yes (SuperAdmin) |

### Health Check
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/health` | Returns 200 OK if database connection is healthy |

---

## RBAC Roles

| Role | Privilege Level | Key Permissions |
|------|----------------|-----------------|
| **SuperAdmin** | Highest | Full system access, user management, all reports, system settings |
| **Admin** | High | All except system settings and super-admin user operations |
| **Supervisor** | Medium-High | Own department full access + view all employees, approve investigations, generate warnings |
| **TeamLeader** | Medium | Own team attendance + exceptions + generate counselling/verbal warnings |
| **HR** | Specialized | All HR functions (warnings, escalations, reports) but not system settings |
| **Operator** | Basic | Self-only view, clock in/out, view own attendance history |

### Policy-Based Authorization

The backend uses named authorization policies for fine-grained control:

- `SuperAdminOnly` — Only SuperAdmin role
- `AdminOrAbove` — SuperAdmin or Admin
- `SupervisorOrAbove` — SuperAdmin, Admin, or Supervisor
- `TeamLeaderOrAbove` — Includes TeamLeader
- `HROrAbove` — Includes HR role
- `AuthenticatedUser` — Any authenticated user

---

## Deployment

### Docker Compose

Complete multi-container setup with PostgreSQL, .NET backend, React frontend, and Adminer:

```bash
# Start all services
docker-compose up -d

# View logs
docker-compose logs -f backend

# Stop all services
docker-compose down

# Stop and remove volumes (WARNING: deletes all data)
docker-compose down -v
```

### Azure App Service

#### Backend Deployment

```bash
# Publish the API
cd Api
dotnet publish -c Release -o ./publish

# Create resource group
az group create --name wacam-rg --location eastus

# Create App Service Plan
az appservice plan create --name wacam-plan --resource-group wacam-rg --sku B1 --is-linux

# Create Web App
az webapp create --name wacam-api --resource-group wacam-rg --plan wacam-plan --runtime "DOTNET|8.0"

# Configure environment variables
az webapp config appsettings set --name wacam-api --resource-group wacam-rg \
  --settings ConnectionStrings__WacamDb="Host=<postgres-host>;Port=5432;Database=wacam_db;Username=wacam_user;Password=<password>;"

# Deploy
az webapp deploy --name wacam-api --resource-group wacam-rg --src-path ./publish --type zip
```

#### Frontend Deployment

Deploy to Azure Static Web Apps or Azure Blob Storage with CDN:

```bash
# Build for production
cd Frontend
npm run build

# Deploy to Azure Static Web Apps
az staticwebapp create --name wacam-frontend --resource-group wacam-rg --source ./dist --output-location dist --root-folder ./dist
```

### Docker Swarm / Kubernetes

For production-scale deployments, convert `docker-compose.yml` to Kubernetes manifests using tools like `kompose`:

```bash
kompose convert -f docker-compose.yml -o kubernetes/
kubectl apply -f kubernetes/
```

---

## Core Business Rules

### 1. EmployeeId is the Master Key
Every record links to an employee via their `EmployeeId` (string, e.g., `"708513"`). Display names are purely cosmetic. This ensures consistency across attendance records, exceptions, warnings, and escalations.

### 2. Grace Period Before Late Counting
Employees have a **10-minute grace period** after their expected clock-in time. An employee clocking in 8 minutes late is recorded as present-on-time. After 10 minutes, the excess minutes count toward lost production time.

### 3. No Disciplinary Action Until Investigation Complete
A warning letter can **only** be generated after an `AttendanceException` has been investigated (status = `"Investigated"` or `"Closed"`). This prevents premature punitive action without supervisory review.

### 4. Escalation Chain Follows Manager Hierarchy
When a disciplinary action isn't completed within 24 hours, it automatically escalates to the next manager up the reporting chain:
- **Level 1**: Team Leader (immediate supervisor)
- **Level 2**: Production Manager
- **Level 3+**: Higher management (continues until resolved)

Each level has exactly **24 hours** to mark the action as completed.

### 5. Auto-Detect Exceptions
The system automatically creates exception records when:
- No clock-in by **06:30** for morning shift → `UnplannedAbsenteeism`
- No clock-out after expected end time → `MissingClockOut`
- Clock-in time exceeds late threshold → `LateArrival`
- Clock-out before expected end by significant margin → `EarlyDeparture`

### 6. Warning Thresholds
| Warning Level | Trigger Condition | Auto-Escalation |
|--------------|-------------------|-----------------|
| Counselling | 3+ exceptions/month | No |
| Verbal Warning | 5+ exceptions/month | No |
| Written Warning | 7+ exceptions/month | No |
| Final Written Warning | 9+ exceptions/month | **Yes** → Level 2 Escalation |

### 7. All Changes Are Audit Logged
Every Create, Update, and Delete operation generates an immutable `AuditLog` entry capturing:
- Entity type and ID
- Action performed
- Old and new values (JSON snapshots)
- Performer identity
- Timestamp
- IP address

---

## Troubleshooting

### Backend Issues

| Problem | Solution |
|---------|----------|
| Connection refused to PostgreSQL | Ensure PostgreSQL is running: `docker-compose up postgres` or check local service status |
| Migration fails with duplicate key | Clear migrations folder and re-run: `dotnet ef migrations remove && dotnet ef migrations add InitialCreate && dotnet ef database update` |
| JWT validation errors | Verify `Jwt.Key` is at least 32 characters in `appsettings.json` |
| CORS errors in development | Ensure frontend URL matches the CORS allow list in `Program.cs` |

### Frontend Issues

| Problem | Solution |
|---------|----------|
| Blank page after login | Check browser console for 401 errors; verify JWT token is being stored correctly |
| API calls failing | Verify `VITE_API_BASE_URL` in `.env.local` points to correct backend URL |
| Build errors | Run `npm ci` instead of `npm install` to ensure lockfile consistency |
| PWA not installing | Ensure HTTPS is enabled in production; check `manifest.json` validity |

### Docker Issues

| Problem | Solution |
|---------|----------|
| Container won't start | Check logs: `docker-compose logs <service-name>` |
| Port conflicts | Modify port mappings in `docker-compose.yml` (e.g., `"8082:8080"`) |
| Database initialization slow | First migration takes longer; check `docker-compose logs postgres` for progress |
| Volume permissions | Run `docker-compose down -v` to clean volumes and restart |

---

**Built by Curi Lab.** — Production-ready workforce management for modern manufacturing.
