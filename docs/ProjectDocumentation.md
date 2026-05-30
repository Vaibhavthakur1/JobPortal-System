# Job Portal System Documentation

## Introduction
The Job Portal System is a microservices-based application designed to facilitate job seekers and recruiters. It consists of multiple services, each responsible for specific functionalities. This documentation provides a detailed overview of each service, their roles, and how they interact within the system.

---

## Services Overview

### 1. IdentityService
**Purpose:**
The IdentityService handles user authentication, authorization, and user management.

**Key Components:**
- **Controllers:**
  - `AuthController`: Manages user login, registration, and token generation.
  - `AdminUsersController`: Handles administrative user operations.
- **Services:**
  - `AuthService`: Implements authentication logic.
  - `SendGridEmailService`: Sends emails for account verification and password recovery.
- **Repositories:**
  - `UserRepository`: Manages database operations for user entities.
- **Database:**
  - `IdentityDbContext`: Defines the schema for user-related tables.
- **Models:**
  - `User`: Represents user entities.
  - `DTOs`: Data Transfer Objects for API communication.

**Flow:**
1. Users interact with `AuthController` for login and registration.
2. `AuthService` validates credentials and generates tokens.
3. `SendGridEmailService` sends verification or recovery emails.
4. `UserRepository` interacts with the database to store/retrieve user data.

**Key Configurations:**
- `appsettings.json`: Contains database connection strings and email service configurations.

---

### 2. PaymentService
**Purpose:**
The PaymentService manages recruiter wallets and transactions.

**Key Components:**
- **Controllers:**
  - `PaymentController`: Handles wallet and transaction-related APIs.
- **Services:**
  - `PaymentService`: Implements business logic for payments.
- **Repositories:**
  - `WalletRepository`: Manages database operations for wallets.
- **Database:**
  - `PaymentDbContext`: Defines the schema for wallet and transaction tables.
- **Models:**
  - `RecruiterWallet`: Represents wallet entities.
  - `DTOs`: Data Transfer Objects for API communication.

**Flow:**
1. Recruiters interact with `PaymentController` to manage wallets and transactions.
2. `PaymentService` processes payment logic.
3. `WalletRepository` interacts with the database to store/retrieve wallet data.

**Key Configurations:**
- `appsettings.json`: Contains database connection strings.

---

### 3. AdminService
**Purpose:**
The AdminService handles administrative tasks such as audit logging.

**Key Components:**
- **Controllers:**
  - `AdminController`: Manages administrative operations.
- **Repositories:**
  - `AuditRepository`: Manages database operations for audit logs.
- **Consumers:**
  - `AuditLogConsumer`: Listens to messages for audit logging.
- **Database:**
  - `AdminDbContext`: Defines the schema for audit logs.
- **Models:**
  - `AuditLog`: Represents audit log entities.
  - `DTOs`: Data Transfer Objects for API communication.

**Flow:**
1. Administrative actions are logged via `AuditLogConsumer`.
2. `AuditRepository` stores logs in the database.

**Key Configurations:**
- `appsettings.json`: Contains database connection strings.

---

### 4. ApplicationService
**Purpose:**
The ApplicationService manages job applications.

**Key Components:**
- **Controllers:**
  - `ApplicationsController`: Handles job application-related APIs.
- **Services:**
  - `ApplicationService`: Implements business logic for job applications.
- **Repositories:**
  - `ApplicationRepository`: Manages database operations for job applications.
- **Database:**
  - `ApplicationDbContext`: Defines the schema for job applications.
- **Models:**
  - `JobApplication`: Represents job application entities.
  - `DTOs`: Data Transfer Objects for API communication.
- **Sagas:**
  - `ApplicationSaga`: Manages long-running application processes.

**Flow:**
1. Users interact with `ApplicationsController` to manage job applications.
2. `ApplicationService` processes application logic.
3. `ApplicationRepository` interacts with the database to store/retrieve application data.
4. `ApplicationSaga` handles long-running processes.

**Key Configurations:**
- `appsettings.json`: Contains database connection strings.

---

## System Architecture
**High-Level Architecture:**
- The system follows a microservices architecture.
- Services communicate via REST APIs and message queues.
- Each service has its own database (Database-per-Service pattern).

**Communication:**
- REST APIs for synchronous communication.
- RabbitMQ for asynchronous messaging.

---

## Database Design
**Key Tables:**
- IdentityService: `Users`
- PaymentService: `Wallets`, `Transactions`
- AdminService: `AuditLogs`
- ApplicationService: `JobApplications`

**Relationships:**
- Recruiters and Wallets (1:1)
- Recruiters and Transactions (1:N)
- Applications and Jobs (N:1)

---

## Authentication and Authorization
- **Authentication:** Implemented using JWT tokens.
- **Authorization:** Role-based (e.g., Recruiter, Admin).

---

## Error Handling and Logging
- **Error Handling:** Middleware captures exceptions and returns appropriate HTTP responses.
- **Logging:**
  - Logs are written to files.
  - Common warnings/errors include database precision issues and missing configurations.

---

## Deployment and Configuration
- **Deployment:**
  - Services are containerized using Docker.
  - Kubernetes is used for orchestration.
- **Configuration:**
  - `appsettings.json` files contain environment-specific settings.

---

## Future Improvements
- Enhance error handling with detailed error codes.
- Optimize database queries for better performance.
- Implement caching for frequently accessed data.

---

This documentation provides a comprehensive overview of the Job Portal System, covering each service, their roles, and interactions. Use this as a reference for your viva preparation.