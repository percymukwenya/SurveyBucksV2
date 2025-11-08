# SurveyBucks - Codebase Architecture Guide

This document provides a comprehensive guide to the SurveyBucks codebase architecture, designed to help Claude AI instances quickly understand and navigate this project.

## Project Overview

SurveyBucks is a survey management platform that allows users to participate in surveys and earn rewards. The system includes both user-facing features and administrative tools for survey management, analytics, and user verification.

**Technology Stack:**
- **Backend:** .NET 8.0 Web API with Clean Architecture
- **Frontend:** Angular 19 with Angular Material
- **Database:** SQL Server with Dapper ORM and Entity Framework Core for Identity
- **Authentication:** JWT with ASP.NET Core Identity, OAuth providers (Google, Facebook, Microsoft)
- **File Storage:** Local file storage with Azure Blob Storage support
- **Email:** MailKit for email services

## Backend Architecture (.NET 8.0)

The backend follows Clean Architecture principles with clear separation of concerns:

### Project Structure

```
SurveyBucks.sln
├── Domain/              # Core business entities and interfaces
├── Application/         # Business logic and services  
├── Infrastructure/      # Data access and external services
└── WebApi/             # API controllers and configuration
```

### Layer Dependencies

```
WebApi → Application → Domain
WebApi → Infrastructure → Domain
```

### Key Components

#### 1. Domain Layer (`Domain/`)
- **Models:** Core entities (ApplicationUser, Survey DTOs, Admin models)
- **Interfaces:** Repository and service contracts
- **Enums:** Business constants and types
- **Key Files:**
  - `ApplicationUser.cs` - Extends IdentityUser for custom user properties
  - `Admin/BranchingModels.cs` - Survey branching logic models
  - `Response/` - Survey response and participation DTOs

#### 2. Application Layer (`Application/`)
- **Services:** Business logic implementation
- **Key Services:**
  - `SurveyParticipationService` - Core survey participation logic
  - `SurveyBranchingService` - Survey logic and branching
  - `SurveyManagementService` - Admin survey management
  - `UserProfileService` - User profile management
  - `GamificationService` - Points and rewards system
  - `DocumentService` - Document verification
  - `BankingService` - Banking detail verification
- **Models:** Service-specific DTOs and configurations
- **Middleware:** Security and file upload middleware

#### 3. Infrastructure Layer (`Infrastructure/`)
- **Data Access:** Dapper-based repositories with SQL Server
- **Key Files:**
  - `Data/ApplicationDbContext.cs` - EF Core context for Identity
  - `Shared/SqlConnectionFactory.cs` - Dapper connection factory
  - `Repositories/` - Data access implementations
- **External Services:** Email, file storage, etc.

#### 4. WebApi Layer (`WebApi/`)
- **Controllers:** API endpoints organized by feature
- **Key Controllers:**
  - `SurveyController` - Public survey endpoints
  - `Admin/SurveyManagementController` - Admin survey management
  - `AuthController` - Authentication endpoints
  - `GamificationController` - Rewards and points
- **Configuration:** JWT, CORS, services registration in `Program.cs`

### Database Configuration

- **Connection String:** Uses "SmarterConnection" for SQL Server
- **Schema:** All tables use "SurveyBucks" schema
- **ORM:** 
  - Entity Framework Core for Identity management
  - Dapper for all other data access (performance-focused)

### Key Features

1. **Survey System:**
   - Multi-section surveys with branching logic
   - Question types: Multiple choice, text, rating, etc.
   - Response tracking and analytics
   - Preview system for administrators

2. **User Management:**
   - Profile completion tracking with weighted sections
   - Document verification system
   - Banking detail verification
   - Demographic targeting

3. **Gamification:**
   - Points system
   - Achievements and challenges
   - Leaderboards
   - Reward redemption

4. **Administrative Tools:**
   - Survey analytics and reporting
   - User management and verification
   - Document and banking verification workflows
   - Real-time dashboard

## Frontend Architecture (Angular 19)

### Project Structure

```
survey-bucks-fe/
├── src/app/
│   ├── core/           # Singleton services, guards, interceptors
│   ├── features/       # Feature modules
│   ├── shared/         # Reusable components and pipes
│   ├── layout/         # Layout components
│   └── public/         # Public pages
```

### Key Architectural Patterns

1. **Lazy Loading:** All feature modules are lazy-loaded
2. **State Management:** NgRx for complex state management
3. **Reactive Programming:** RxJS throughout
4. **Angular Material:** Consistent UI components
5. **Route Guards:** Authentication and role-based access

### Core Services

- **AuthService:** JWT authentication and user management
- **DataService:** Base HTTP service with interceptors
- **SurveyService:** Survey participation and management
- **AdminServices:** Various admin-specific services
- **GamificationService:** Points and rewards

### Layout System

- **PublicLayout:** For landing pages and public content
- **MainLayout:** For authenticated user features
- **AdminLayout:** For administrative interface

### Key Features

1. **Survey Taking:**
   - Dynamic question rendering
   - Progress tracking
   - Branching logic support
   - Mobile-responsive design

2. **User Dashboard:**
   - Profile completion tracking
   - Available surveys
   - Rewards and points display
   - Achievement tracking

3. **Administrative Interface:**
   - Survey builder with drag-and-drop
   - Logic builder for branching
   - Analytics dashboards
   - User management tools

## Development Workflow

### Backend Development

```bash
# Build and run the API
cd WebApi
dotnet run

# Run tests
dotnet test

# Database migrations (if using EF migrations)
dotnet ef migrations add MigrationName
dotnet ef database update
```

### Frontend Development

```bash
cd survey-bucks-fe

# Install dependencies
npm install

# Development server
npm start  # or ng serve

# Build for production
npm run build  # or ng build

# Run tests
npm test  # or ng test
```

### Configuration

#### Backend Configuration (`WebApi/appsettings.json`)

Key configuration sections:
- **ConnectionStrings:** Database connections
- **JwtSettings:** JWT authentication configuration
- **EmailSettings:** SMTP configuration
- **FileStorage:** File storage settings (Local/Azure)
- **SurveyMatching:** Survey targeting algorithms
- **ProfileCompletion:** Profile completion requirements

#### Frontend Configuration (`src/environments/`)

- **environment.ts:** Development API endpoints
- **environment.prod.ts:** Production configuration

## Key Business Logic

### Survey Participation Flow

1. User authentication and profile completion check
2. Survey eligibility verification based on demographics
3. Question presentation with branching logic
4. Response collection and validation
5. Completion tracking and reward calculation

### Survey Branching System

The system supports complex survey branching logic:
- **Actions:** Show/hide questions, jump to sections, end survey
- **Conditions:** Based on previous responses
- **Validation:** Business rule validation

### Profile Completion System

Multi-stage profile completion with weighted sections:
- **Demographics:** 25%
- **Banking:** 25%
- **Documents:** 25%
- **Interests:** 25%

### Gamification System

- Points awarded for survey completion
- Achievement system with unlockable rewards
- Leaderboard functionality
- Reward redemption workflow

## Testing Strategy

### Backend Testing
- Unit tests for services
- Integration tests for controllers
- Repository pattern enables easy mocking

### Frontend Testing
- Unit tests with Jasmine/Karma
- Component testing with Angular Testing Utilities
- E2E testing capability

## Deployment Architecture

### Backend Deployment
- Azure App Service deployment configured
- SQL Server database
- Azure Blob Storage for file uploads (configurable)

### Frontend Deployment
- Angular Universal for SSR support
- Static file serving capability
- CDN-friendly build output

## Security Considerations

1. **Authentication:** JWT with refresh tokens
2. **Authorization:** Role-based access control
3. **File Upload Security:** Middleware for file validation
4. **CORS:** Configured for specific origins
5. **Data Protection:** ASP.NET Core Identity with proper hashing

## Performance Optimizations

1. **Database:** Dapper for high-performance queries
2. **Frontend:** Lazy loading and OnPush change detection
3. **Caching:** Survey matching with configurable cache expiration
4. **File Storage:** Separate storage service abstraction

## Common Development Tasks

### Adding a New Survey Question Type
1. Update domain models in `Domain/Models/Response/`
2. Add repository methods in `Infrastructure/Repositories/`
3. Update service logic in `Application/Services/`
4. Add controller endpoints in `WebApi/Controllers/`
5. Update frontend question renderer component

### Adding New Admin Features
1. Create DTOs in `Domain/Models/Admin/`
2. Add repository interface and implementation
3. Create service in `Application/Services/`
4. Add admin controller
5. Create Angular components in `features/admin/`

### Modifying User Profile Requirements
1. Update `ProfileCompletion` configuration in `appsettings.json`
2. Modify `ProfileCompletionCalculator` service
3. Update frontend profile completion components

## Troubleshooting

### Common Issues
1. **CORS Errors:** Check `AllowedOrigins` in appsettings.json
2. **JWT Issues:** Verify JWT settings and token expiration
3. **Database Connection:** Ensure SQL Server is running and connection string is correct
4. **File Upload Issues:** Check file storage configuration and permissions

### Debugging Tips
1. Use Swagger UI at `/swagger` for API testing
2. Check browser developer tools for frontend issues
3. Review application logs for backend issues
4. Use Angular DevTools for state debugging

This architecture guide should provide a solid foundation for understanding and working with the SurveyBucks codebase. The clean architecture approach ensures maintainability and testability while supporting the complex business requirements of a survey platform.