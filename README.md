# Meeting Scheduler API

A robust, conflict-free meeting scheduling system built with ASP.NET Core using Clean Architecture principles. The system automatically finds the earliest available time slots for multiple participants while enforcing business hours and preventing scheduling conflicts.

## 🚀 Features

- **Conflict-Free Scheduling**: Automatically detects and prevents overlapping meetings
- **Multi-Participant Support**: Schedule meetings for multiple users simultaneously
- **Business Hours Enforcement**: Ensures all meetings fall within 09:00-17:00 UTC
- **Intelligent Algorithm**: Finds the earliest available time slot that works for all participants
- **Clean Architecture**: Follows SOLID principles with clear separation of concerns
- **Comprehensive Testing**: Unit, integration, and architecture tests included
- **Structured Logging**: Full observability with Serilog and Seq integration
- **Performance Monitoring**: Request tracking with correlation IDs and performance metrics

## 🏗️ Architecture

The system follows Clean Architecture principles with four distinct layers:

```
┌─────────────────────────────────────────────────────────────┐
│                        Web.Api                              │
│                    (Controllers/Endpoints)                  │
├─────────────────────────────────────────────────────────────┤
│                      Application                            │
│              (CQRS Handlers, Services, DTOs)               │
├─────────────────────────────────────────────────────────────┤
│                    Infrastructure                           │
│           (Repositories, Database, External Services)      │
├─────────────────────────────────────────────────────────────┤
│                       Domain                                │
│              (Entities, Business Rules, Events)            │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

- **Domain Layer**: Meeting and User entities with business rule validation
- **Application Layer**: CQRS pattern with MediatR, scheduling algorithm service
- **Infrastructure Layer**: Entity Framework Core, PostgreSQL, Serilog logging
- **API Layer**: RESTful endpoints with comprehensive error handling

## 🛠️ Technology Stack

- **Framework**: .NET 9.0
- **Database**: PostgreSQL (production) / In-Memory (development)
- **ORM**: Entity Framework Core
- **Logging**: Serilog with Seq integration
- **Testing**: xUnit, Moq, FluentAssertions
- **Architecture**: Clean Architecture, CQRS, Repository Pattern

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/) (for production)
- [Docker](https://www.docker.com/get-started) (optional, for Seq)

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone <repository-url>
cd meeting-scheduler
```

### 2. Run the Application

```bash
# Development mode (uses in-memory database)
dotnet run --project src/Web.Api

# The API will be available at:
# - HTTP: http://localhost:5000
# - HTTPS: https://localhost:5001
# - Swagger UI: https://localhost:5001/swagger
```

### 3. Optional: Start Seq for Log Analysis

```bash
# Using Docker
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest

# Seq will be available at: http://localhost:5341
```

## 📚 API Documentation

### Base URL

- Development: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

### Endpoints

#### Create User

```http
POST /users
Content-Type: application/json

{
  "name": "John Doe"
}
```

**Response:**

```json
{
  "id": 1,
  "name": "John Doe"
}
```

#### Schedule Meeting

```http
POST /meetings
Content-Type: application/json

{
  "participantIds": [1, 2, 3],
  "durationMinutes": 60,
  "earliestStart": "2024-06-15T09:00:00Z",
  "latestEnd": "2024-06-15T17:00:00Z"
}
```

**Response:**

```json
{
  "id": 1,
  "participants": [
    { "id": 1, "name": "John Doe" },
    { "id": 2, "name": "Jane Smith" },
    { "id": 3, "name": "Bob Johnson" }
  ],
  "startTime": "2024-06-15T10:00:00Z",
  "endTime": "2024-06-15T11:00:00Z"
}
```

#### Get User Meetings

```http
GET /users/{userId}/meetings
```

**Response:**

```json
[
  {
    "id": 1,
    "participants": [...],
    "startTime": "2024-06-15T10:00:00Z",
    "endTime": "2024-06-15T11:00:00Z"
  }
]
```

### Error Responses

All endpoints return consistent error responses:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "User name cannot be empty",
  "instance": "/users"
}
```

## 🧪 Testing

### Run All Tests

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/Application.UnitTests
dotnet test tests/Web.Api.IntegrationTests
```

### Test Categories

- **Unit Tests**: Domain logic, CQRS handlers, scheduling algorithm
- **Integration Tests**: API endpoints, database operations
- **Architecture Tests**: Dependency rules, layer isolation

## 🔧 Configuration

### Development Configuration

The application uses in-memory database by default for development. No additional setup required.

### Production Configuration

Update `appsettings.Production.json`:

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Database=MeetingScheduler;Username=your_user;Password=your_password"
  },
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://your-seq-server:5341",
          "apiKey": "your-api-key"
        }
      }
    ]
  }
}
```

### Environment Variables

```bash
# Database
ConnectionStrings__Database="Host=localhost;Database=MeetingScheduler;Username=user;Password=pass"

# Logging
Serilog__MinimumLevel__Default=Information
Serilog__WriteTo__1__Args__serverUrl=http://seq:5341
```

## 📊 Monitoring and Observability

### Health Checks

```http
GET /health
```

Monitor system health including database connectivity.

### Structured Logging

All requests include correlation IDs for tracing:

```
[10:30:15 INF] [abc123] Successfully created meeting 456 for participants [1, 2, 3]
```

### Seq Dashboard

Access Seq at `http://localhost:5341` for:

- Real-time log analysis
- Performance monitoring
- Error tracking
- Custom dashboards

## 🏢 Business Rules

### Meeting Scheduling Rules

1. **Business Hours**: Meetings must be within 09:00-17:00 UTC
2. **No Conflicts**: Participants cannot have overlapping meetings
3. **Minimum Duration**: Meetings must be at least 1 minute
4. **Participant Validation**: All participants must exist in the system
5. **Time Validation**: Start time must be before end time

### Scheduling Algorithm

The system uses an intelligent algorithm that:

1. Validates all participants exist
2. Retrieves existing meetings for conflict detection
3. Generates potential time slots in 15-minute increments
4. Checks each slot for business hours compliance
5. Verifies no conflicts with existing meetings
6. Returns the earliest available slot

## 🚀 Deployment

### Docker Deployment

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Web.Api.dll"]
```

### Database Migrations

```bash
# Create migration
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/Web.Api

# Update database
dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines

- Follow Clean Architecture principles
- Write comprehensive tests for new features
- Include structured logging for observability
- Update documentation for API changes
- Ensure all tests pass before submitting PR

## 📖 Additional Documentation

- [Database Setup Guide](docs/DATABASE_SETUP.md)
- [Logging and Monitoring](docs/LOGGING_AND_MONITORING.md)
- [Seq Monitoring Queries](docs/seq-queries.md)
- [API Examples](docs/API_EXAMPLES.md)

## 🐛 Troubleshooting

### Common Issues

1. **Database Connection Issues**

   - Verify PostgreSQL is running
   - Check connection string format
   - Ensure database exists

2. **Scheduling Failures**

   - Check business hours (09:00-17:00 UTC)
   - Verify participant availability
   - Review meeting duration requirements

3. **Performance Issues**
   - Monitor Seq logs for slow requests
   - Check database query performance
   - Review scheduling algorithm timing

### Getting Help

- Check the [troubleshooting guide](docs/LOGGING_AND_MONITORING.md#troubleshooting)
- Review Seq logs for detailed error information
- Open an issue with correlation ID and error details

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html) principles
- Inspired by Domain-Driven Design practices
- Uses [Serilog](https://serilog.net/) for structured logging
- Powered by [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/) and [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
#   z x c  
 