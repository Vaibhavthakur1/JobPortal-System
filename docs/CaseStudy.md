# JobMart — Full Project Case Study

---

## 1. Executive Summary

JobMart is a full-stack job portal platform built as a distributed microservices system. It connects job seekers with recruiters through a feature-rich web application — covering everything from resume building and job search to applicant pipeline management and Razorpay-powered payments.

The backend is built on **.NET 10** using a microservices architecture with **9 independent services**, an **Ocelot API Gateway**, **RabbitMQ** for async messaging, **MassTransit** for saga orchestration, and **SQL Server** as the persistence layer. The frontend is an **Angular 21** single-page application using standalone components, Angular Signals for reactive state, and Tailwind CSS for styling.

---

## 2. Problem Statement

Traditional job portals suffer from several pain points:

- **For job seekers**: No visibility into where their application stands after submission. No structured resume builder. No real-time updates.
- **For recruiters**: No organized pipeline to track candidates. No way to filter or view candidate resumes efficiently. No integrated payment for premium features.
- **For platform admins**: No audit trail. No moderation tools for flagged content. No centralized user management.

JobMart was designed to solve all three perspectives in a single, cohesive platform.

---

## 3. Goals & Objectives

| Goal | Implementation |
|------|---------------|
| Structured job application lifecycle | MassTransit Saga state machine (Draft → Submitted → Screening → Interview → Offered → Accepted/Rejected/Withdrawn) |
| Real-time status notifications | RabbitMQ + NotificationService (in-app + email via SMTP) |
| Resume builder + PDF upload | ResumeService with structured builder and PDF file storage |
| Recruiter candidate pipeline | RecruiterService with stage management and points-based resume access |
| Integrated payments | Razorpay gateway (INR) with wallet and transaction history |
| Admin oversight | AdminService with audit logs, user management, flagged job moderation |
| Secure authentication | JWT + OTP email verification via IdentityService |

---

## 4. System Architecture

### 4.1 High-Level Overview

```
┌─────────────────────────────────────────────────────────────┐
│                     Angular 21 Frontend                      │
│         (Standalone Components · Signals · Tailwind)         │
└──────────────────────────┬──────────────────────────────────┘
                           │ HTTP
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                  Ocelot API Gateway (:5000)                  │
│              Route-based reverse proxy + JWT auth            │
└──┬──────┬──────┬──────┬──────┬──────┬──────┬──────┬────────┘
   │      │      │      │      │      │      │      │
   ▼      ▼      ▼      ▼      ▼      ▼      ▼      ▼
 :5001  :5002  :5003  :5004  :5005  :5006  :5007  :5008
Identity  Job   App   Resume Recruiter Pay  Notify  Admin
Service Catalog Service Service Service Service Service Service
   │      │      │      │      │      │      │      │
   └──────┴──────┴──────┴──────┴──────┴──────┴──────┘
                           │
                    ┌──────▼──────┐
                    │  RabbitMQ   │
                    │ (MassTransit│
                    │  + Sagas)   │
                    └─────────────┘
                           │
              ┌────────────┼────────────┐
              ▼            ▼            ▼
         SQL Server   SQL Server   SQL Server
         (per-service databases)
```

### 4.2 Service Inventory

| Service | Port | Responsibility |
|---------|------|---------------|
| **ApiGateway** | 5000 | Ocelot reverse proxy, single entry point |
| **IdentityService** | 5001 | Auth, JWT, OTP email verification |
| **JobCatalogService** | 5002 | Job listings CRUD, search, filtering |
| **ApplicationService** | 5003 | Job applications, saga orchestration |
| **ResumeService** | 5004 | Resume builder, PDF upload/download |
| **RecruiterService** | 5005 | Pipeline management, resume access |
| **PaymentService** | 5006 | Razorpay integration, wallet, transactions |
| **NotificationService** | 5007 | In-app + email notifications |
| **AdminService** | 5008 | Audit logs, user management, flagged jobs |

### 4.3 Design Patterns Used

- **Database-per-Service** — each microservice owns its own SQL Server database
- **API Gateway Pattern** — Ocelot routes all frontend traffic through a single host
- **Saga Pattern** — MassTransit state machine manages the full application lifecycle
- **Outbox Pattern** — MassTransit inbox/outbox in ApplicationDbContext for reliable messaging
- **Event-Driven Architecture** — services communicate via RabbitMQ events, not direct HTTP calls
- **Repository Pattern** — all data access abstracted behind repository interfaces
- **CQRS-lite** — read and write operations separated at the service layer

---

## 5. Backend Deep Dive

### 5.1 IdentityService

Handles all authentication and user management.

**Key flows:**
1. **Registration** → hashes password with BCrypt → generates 6-digit OTP → sends verification email via MailKit SMTP → publishes `UserRegisteredEvent`
2. **Login** → validates credentials → generates JWT (1hr) + refresh token (7 days) → returns `fullName`, `role`, `userId` in response
3. **JWT Claims** — `sub` (userId), `email`, `role`, `fullName` — all downstream services validate the same JWT secret

**Internal endpoint** (`/internal/users/{userId}`) — used by ApplicationService to resolve user email/name for notification enrichment without cross-service JWT calls.

### 5.2 ApplicationService + Saga

The most architecturally complex service. Manages the full lifecycle of a job application.

**Application states:**
```
Draft → Submitted → Screening → Interview → Offered → Accepted
                                                    ↘ Rejected
                              ↘ Rejected
              ↘ Rejected
                                    ↕ (any state)
                                  Withdrawn
```

**Saga mechanics:**
- `ApplicationSaga` is a `MassTransitStateMachine<ApplicationSagaState>`
- `CorrelationId = ApplicationId` — MassTransit routes events to the correct saga instance automatically
- On every state transition, the saga publishes a `SendNotificationEvent` with `Type: "Both"` (in-app + email)
- `JobSeekerEmail` and `JobSeekerName` are resolved at event publish time by calling IdentityService's internal endpoint, then carried through the saga state

**Outbox pattern** — `ApplicationDbContext` includes MassTransit inbox/outbox tables, ensuring events are never lost even if RabbitMQ is temporarily unavailable.

### 5.3 NotificationService

Consumes `SendNotificationEvent` from RabbitMQ and:
1. Persists the notification to `JobPortal_Notifications` DB (always)
2. Sends a real HTML email via MailKit SMTP when `Type` is `"Email"` or `"Both"`

**Email template** — branded HTML with JobMart indigo header, candidate name, message body, and a "View My Applications" CTA button.

**Resilience** — email failures are caught and logged but do not fail the notification persistence. The in-app notification is always saved.

### 5.4 RecruiterService

Manages the recruiter's candidate pipeline with a points-based access model.

**Pipeline flow:**
1. When a job seeker submits an application, `ApplicationSubmittedConsumer` auto-adds them to the recruiter's pipeline with stage `"New"`
2. Recruiter can move candidates through stages: New → Shortlisted → Contacted → Rejected
3. **View Resume (10 pts, 30-day access)** — deducts 10 points from wallet, fetches resume from ResumeService, returns full structured data or PDF depending on resume type
4. **Withdrawal sync** — `ApplicationWithdrawnConsumer` listens for `ApplicationStatusChangedEvent` with `NewStatus == "Withdrawn"` and locks the pipeline entry

**Resume access logic:**
```
First view → deduct 10 pts → set ResumeAccessExpiresAt = now + 30 days
Within 30 days → free re-access
After 30 days → deduct 10 pts again
```

### 5.5 PaymentService

Integrates with **Razorpay** for INR payments.

**Flow:**
1. Frontend calls `POST /api/payment/initiate` with desired points
2. Backend calls Razorpay REST API (`/v1/orders`) with Basic Auth (KeyId:KeySecret) to create a real order
3. Returns `orderId`, `amount` (INR), `currency`, `gatewayKey` to frontend
4. Frontend opens Razorpay checkout modal with the order
5. On payment success, frontend calls `POST /api/payment/confirm` with `razorpay_payment_id`
6. Backend credits points to wallet, records completed transaction, publishes `PaymentCompletedEvent`

**Pricing:**
| Package | Points | Price (INR) |
|---------|--------|-------------|
| Starter | 50 pts | ₹49 |
| Standard | 150 pts | ₹149 |
| Pro | 500 pts | ₹449 |

**Internal deduction** — `POST /api/payment/deduct` (no auth) is called by RecruiterService when a recruiter views a resume. Validates sufficient balance before deducting.

### 5.6 ResumeService

Supports two resume types:

**Built resumes** — structured data (personal info, education, experience, projects, skills) stored in SQL Server. Exportable as a text-based PDF.

**Uploaded resumes** — PDF files stored on the server filesystem under `uploads/resumes/{userId}/`. Only PDF format accepted (max 5 MB). Accessible via:
- `GET /api/resumes/{id}/download-uploaded` — for the job seeker (authenticated)
- `GET /api/resumes/{id}/download-uploaded-internal` — for RecruiterService (no auth, service-to-service)

**Candidate default endpoint** (`GET /api/resumes/candidate/{userId}/default`) — used by RecruiterService to fetch a candidate's default resume without requiring the candidate's JWT.

---

## 6. Frontend Deep Dive

### 6.1 Technology Stack

| Technology | Version | Usage |
|-----------|---------|-------|
| Angular | 21.2.9 | SPA framework |
| Tailwind CSS | 3.4.19 | Utility-first styling |
| Angular Signals | built-in | Reactive state management |
| Axios | 1.6.2 | HTTP client |
| Razorpay Checkout.js | CDN | Payment modal |
| Material Symbols | Google CDN | Icon font |
| Poppins | Google Fonts | Typography |

### 6.2 Application Structure

```
src/app/
├── pages/
│   ├── landing/          ← Public landing page
│   ├── auth/             ← Login, Register, OTP, Forgot Password
│   ├── jobseeker/        ← Dashboard, Job Search, Applications, Resume Builder
│   ├── recruiter/        ← Dashboard, Post Job, Listings, Pipeline, Wallet
│   └── admin/            ← Dashboard, Users, Flagged Jobs, Audit Logs
├── shared/
│   ├── components/navbar/
│   └── components/toast/
├── services/             ← One service per backend microservice
├── store/                ← auth.signal.ts, ui.signal.ts
├── models/               ← TypeScript interfaces matching backend DTOs
└── guards/               ← JobSeekerGuard, RecruiterGuard, AdminGuard
```

### 6.3 State Management

Uses **Angular Signals** (no NgRx/Zustand for UI state):

- `AuthSignalStore` — persists `accessToken`, `refreshToken`, `user` to `localStorage`. Loaded on app init.
- `UISignalStore` — manages dark mode toggle, toast notifications, notification panel open/close state.

### 6.4 Routing & Guards

Three role-based guards protect routes:

```typescript
JobSeekerGuard  → /dashboard, /jobs, /my-applications, /resumes
RecruiterGuard  → /recruiter/dashboard, /recruiter/post-job, /recruiter/listings, /recruiter/wallet
AdminGuard      → /admin/dashboard, /admin/users, /admin/flagged-jobs, /admin/audit-logs
```

The root `/` route renders the public landing page. Unauthenticated users are redirected to `/login`.

### 6.5 Key UI Features

**Landing Page** — dark-themed hero, features grid, "How it works" steps, recruiter section with stats, Razorpay-secured CTA.

**Job Search** — keyword + location search with filters (type, industry, salary range, experience). Paginated grid of job cards with inline SVG icons.

**Resume Builder** — multi-step wizard (Basic Info → Personal → Skills → Preview) for structured resumes. Drag-and-drop PDF upload for uploaded resumes.

**Application Tracking** — status timeline showing every state transition. Custom confirmation modal (no browser `confirm()`) for withdrawal.

**Candidate Pipeline** — stage management (New/Shortlisted/Contacted/Rejected). "View Resume (10 pts)" button with 30-day access badge. PDF viewer modal for uploaded resumes using `DomSanitizer` + `<iframe>`. Withdrawn candidates are visually locked with a grey banner.

**Wallet** — Razorpay checkout integration. Package selection (50/150/500 pts). Transaction history with INR amounts.

---

## 7. Event-Driven Communication

### 7.1 RabbitMQ Queues

| Queue | Producer | Consumer | Purpose |
|-------|----------|----------|---------|
| `application-saga` | ApplicationService | ApplicationService (Saga) | Application lifecycle state machine |
| `notification-send` | ApplicationSaga | NotificationService | Persist + email notifications |
| `notification-email-verification` | IdentityService | NotificationService | OTP email delivery |
| `recruiter-application-submitted` | ApplicationService | RecruiterService | Auto-add candidate to pipeline |
| `recruiter-application-withdrawn` | ApplicationService | RecruiterService | Lock pipeline entry on withdrawal |

### 7.2 Event Flow — Application Submission

```
JobSeeker submits application
        │
        ▼
ApplicationService.SubmitApplicationAsync()
  ├── Calls IdentityService /internal/users/{id} → gets email + name
  ├── Publishes ApplicationStatusChangedEvent (NewStatus: "Submitted", with email/name)
  └── Publishes ApplicationSubmittedEvent
        │
        ├──▶ ApplicationSaga (application-saga queue)
        │      └── Stores email/name in saga state
        │      └── Publishes SendNotificationEvent (Type: "Both")
        │              │
        │              └──▶ NotificationService (notification-send queue)
        │                     ├── Saves notification to DB
        │                     └── Sends HTML email via SMTP
        │
        └──▶ RecruiterService (recruiter-application-submitted queue)
               └── Creates pipeline entry (Stage: "New")
               └── Publishes SendNotificationEvent to recruiter (Type: "Push")
```

### 7.3 Event Flow — Status Update by Recruiter

```
Recruiter updates application status (e.g., Screening)
        │
        ▼
ApplicationService.UpdateStatusAsync()
  ├── Calls IdentityService /internal/users/{jobSeekerId}
  └── Publishes ApplicationStatusChangedEvent (NewStatus: "Screening", with email/name)
        │
        └──▶ ApplicationSaga
               └── Transitions state: Submitted → Screening
               └── Publishes SendNotificationEvent (Type: "Both")
                       │
                       └──▶ NotificationService
                              ├── Saves in-app notification
                              └── Sends email: "Your Application is Under Review"
```

---

## 8. Security Design

### 8.1 Authentication

- **JWT tokens** — 1-hour expiry, signed with HMAC-SHA256
- **Refresh tokens** — 7-day expiry, stored in DB, rotated on each refresh
- **OTP verification** — 6-digit numeric OTP, 10-minute expiry, required before first login
- **Password hashing** — BCrypt with default work factor

### 8.2 Authorization

- Role-based: `JobSeeker`, `Recruiter`, `Admin`
- All API endpoints protected with `[Authorize]` + `[Authorize(Roles = "...")]`
- Internal service-to-service endpoints use `[AllowAnonymous]` but are not exposed through the API Gateway (Ocelot routes only public paths)

### 8.3 Input Validation

- Reactive Forms with Angular validators on the frontend
- Model validation via ASP.NET Core model binding on the backend
- File upload restricted to PDF only (extension + MIME check), max 5 MB

---

## 9. Database Design

Each service has its own isolated SQL Server database:

| Database | Key Tables |
|----------|-----------|
| `JobPortal_Identity` | Users (Id, FullName, Email, PasswordHash, Role, OtpCode, RefreshToken) |
| `JobPortal_Jobs` | Jobs (Id, RecruiterId, Title, Company, Location, JobType, Industry, Salary, Skills, Status) |
| `JobPortal_Applications` | JobApplications, ApplicationStatusHistory, MassTransit Saga State, Outbox |
| `JobPortal_Resumes` | Resumes, PersonalInfo, Educations, Experiences, Projects |
| `JobPortal_Recruiter` | RecruiterProfiles, CandidatePipelines (with IsWithdrawn, ResumeAccessExpiresAt) |
| `JobPortal_Payment` | Wallets, Transactions |
| `JobPortal_Notifications` | Notifications (UserId, Type, Subject, Body, IsRead) |
| `JobPortal_Admin` | AuditLogs, FlaggedJobs |

---

## 10. Challenges & Solutions

### Challenge 1 — Cross-service user data without coupling
**Problem:** NotificationService needed the job seeker's email to send emails, but only had a `UserId`. Calling IdentityService from NotificationService would create tight coupling.

**Solution:** Added `JobSeekerEmail` and `JobSeekerName` to `ApplicationStatusChangedEvent`. ApplicationService resolves these at publish time via an internal IdentityService endpoint (`/internal/users/{id}`). The saga carries them through state and passes them to `SendNotificationEvent`. NotificationService receives everything it needs in the message — no cross-service HTTP calls.

### Challenge 2 — Saga state persistence under concurrent events
**Problem:** Multiple status changes could arrive out of order or simultaneously, corrupting saga state.

**Solution:** Used MassTransit's Entity Framework saga repository with **optimistic concurrency** (`ISagaVersion` with a `Version` int column). Concurrent updates are detected and retried automatically.

### Challenge 3 — Uploaded PDF viewing in recruiter pipeline
**Problem:** Recruiters needed to view candidate PDFs, but the file is stored in ResumeService's filesystem. RecruiterService can't serve it directly.

**Solution:** Added an internal proxy endpoint in ResumeService (`/api/resumes/{id}/download-uploaded-internal`, `[AllowAnonymous]`). RecruiterService fetches the blob and streams it to the recruiter. The frontend uses `DomSanitizer.bypassSecurityTrustResourceUrl()` to render it in an `<iframe>` modal.

### Challenge 4 — Razorpay .NET SDK incompatibility with .NET 10
**Problem:** The official Razorpay NuGet package targets .NET Framework 4.x and is incompatible with .NET 10.

**Solution:** Replaced the SDK with a direct `HttpClient` call to Razorpay's REST API (`POST /v1/orders`) using Basic Auth (`KeyId:KeySecret`). This is cleaner, has no framework dependency, and is fully compatible.

### Challenge 5 — Angular template extraction breaking builds
**Problem:** Extracting inline templates to `.html` files caused the sub-agent to corrupt several `.ts` files by leaving class method code inside the `@Component` decorator, and `stub-components.ts` was rewritten to re-declare all real components as empty stubs.

**Solution:** Manually reconstructed the corrupted files, restored `stub-components.ts` to only contain `ForgotPasswordComponent`, and verified with a clean `ng build`.

---

## 11. Tech Stack Summary

### Backend
| Technology | Version | Purpose |
|-----------|---------|---------|
| .NET | 10.0 | Runtime |
| ASP.NET Core | 10.0 | Web API framework |
| Entity Framework Core | 10.0 | ORM |
| SQL Server | — | Database |
| MassTransit | 8.3.6 | Message bus abstraction + Saga |
| RabbitMQ | — | Message broker |
| Ocelot | — | API Gateway |
| BCrypt.Net | — | Password hashing |
| MailKit | 4.8.0 | SMTP email sending |
| Serilog | — | Structured logging |
| JWT Bearer | 10.0 | Authentication |

### Frontend
| Technology | Version | Purpose |
|-----------|---------|---------|
| Angular | 21.2.9 | SPA framework |
| TypeScript | 5.9.2 | Language |
| Tailwind CSS | 3.4.19 | Styling |
| Axios | 1.6.2 | HTTP client |
| Razorpay Checkout.js | CDN | Payment UI |
| Angular Signals | built-in | Reactive state |

---

## 12. API Reference Summary

### Auth (`/api/auth`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/register` | Register new user, sends OTP |
| POST | `/verify-email-otp` | Verify email with OTP |
| POST | `/login` | Login, returns JWT + fullName |
| POST | `/refresh` | Refresh access token |
| POST | `/forgot-password` | Send password reset OTP |
| POST | `/reset-password-otp` | Reset password with OTP |
| POST | `/logout` | Invalidate refresh token |

### Jobs (`/api/jobs`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/search` | Search jobs with filters |
| GET | `/{id}` | Get job detail |
| POST | `/` | Create job (Recruiter) |
| GET | `/my-listings` | Get recruiter's jobs |
| PATCH | `/{id}/close` | Close job listing |
| PATCH | `/{id}/archive` | Archive job listing |

### Applications (`/api/applications`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/` | Submit application |
| GET | `/my` | Get my applications |
| GET | `/{id}` | Get application detail |
| PATCH | `/{id}/status` | Update status (Recruiter) |
| PATCH | `/{id}/withdraw` | Withdraw application |

### Recruiter (`/api/recruiter`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET/POST/PUT | `/profile` | Manage company profile |
| GET | `/pipeline/{jobId}` | Get candidate pipeline |
| PATCH | `/pipeline/{id}/stage` | Update pipeline stage |
| POST | `/pipeline/{id}/view-resume` | View resume (10 pts) |
| GET | `/pipeline/{id}/resume-file` | Download PDF resume |

### Payment (`/api/payment`)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/initiate` | Create Razorpay order |
| POST | `/confirm` | Confirm payment, credit points |
| GET | `/wallet` | Get points balance |
| GET | `/transactions` | Transaction history |

---

## 13. Deployment Architecture

```
                    ┌─────────────────┐
                    │   Client Browser │
                    │  Angular 21 SPA  │
                    └────────┬────────┘
                             │ HTTPS
                    ┌────────▼────────┐
                    │  Ocelot Gateway  │
                    │    :5000         │
                    └────────┬────────┘
              ┌──────────────┼──────────────┐
              │              │              │
    ┌─────────▼──┐  ┌────────▼───┐  ┌──────▼──────┐
    │  Identity  │  │    Jobs    │  │ Application │
    │  :5001     │  │  :5002     │  │  :5003      │
    └────────────┘  └────────────┘  └─────────────┘
              │              │              │
    ┌─────────▼──┐  ┌────────▼───┐  ┌──────▼──────┐
    │  Resume    │  │ Recruiter  │  │  Payment    │
    │  :5004     │  │  :5005     │  │  :5006      │
    └────────────┘  └────────────┘  └─────────────┘
              │              │              │
    ┌─────────▼──┐  ┌────────▼───┐
    │ Notification│  │   Admin   │
    │  :5007     │  │  :5008    │
    └────────────┘  └───────────┘
              │
    ┌─────────▼──────────┐
    │     RabbitMQ        │
    │  (Message Broker)   │
    └─────────────────────┘
```

---

## 14. Future Improvements

1. **Real-time notifications** — Replace polling with WebSockets (SignalR) for instant in-app notification delivery
2. **Resume AI scoring** — Integrate an ML model to score candidate resumes against job requirements
3. **Video interviews** — Embed a WebRTC-based interview scheduling and recording feature
4. **Docker + Kubernetes** — Containerize all services with Docker Compose for local dev, Kubernetes for production
5. **Redis caching** — Cache job search results and pipeline data to reduce DB load
6. **Elasticsearch** — Replace SQL-based job search with full-text search for better relevance
7. **Multi-currency payments** — Extend Razorpay integration to support USD/EUR for international recruiters
8. **Analytics dashboard** — Add recruiter analytics (application funnel, time-to-hire, source tracking)

---

## 15. Conclusion

JobMart demonstrates a production-grade microservices architecture applied to a real-world domain. Key architectural achievements include:

- **Loose coupling** — services communicate exclusively through events and well-defined HTTP contracts, never sharing databases
- **Resilience** — the outbox pattern, saga state persistence, and non-fatal email failures ensure the system degrades gracefully
- **Scalability** — each service can be scaled independently; the message broker decouples producers from consumers
- **Developer experience** — clean separation of concerns, one `.html` per component, typed models matching backend DTOs, and consistent Tailwind design language throughout

The project covers the full spectrum of modern web development: authentication flows, file handling, payment gateways, event-driven messaging, saga orchestration, role-based access control, and a polished responsive UI — making it a strong demonstration of end-to-end software engineering capability.
