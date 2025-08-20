# Database Setup Guide

## Overview

The Meeting Scheduler system uses Entity Framework Core with PostgreSQL for production and an in-memory database for development and testing.

## Database Configuration

### Development Environment

By default, the application uses an in-memory database when no connection string is provided. This is perfect for development and testing as it requires no setup.

### Production Environment

For production, configure a PostgreSQL connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Database": "Host=localhost;Database=MeetingScheduler;Username=your_user;Password=your_password"
  }
}
```

## Database Schema

### Tables

#### meeting_users

- `id` (integer, primary key) - Auto-generated user identifier
- `name` (varchar(100), required) - User's display name

#### meetings

- `id` (integer, primary key) - Auto-generated meeting identifier
- `participant_ids` (text, required) - Comma-separated list of participant IDs
- `start_time` (timestamp with time zone, required) - Meeting start time
- `end_time` (timestamp with time zone, required) - Meeting end time

### Indexes

The following indexes are created for optimal query performance:

- `ix_meetings_start_time` - Index on start_time for time-based queries
- `ix_meetings_end_time` - Index on end_time for time-based queries
- `ix_meetings_start_time_end_time` - Composite index for time range queries

## Migrations

### Running Migrations

Migrations are automatically applied in development mode when the application starts.

For production, run migrations manually:

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api
```

### Creating New Migrations

```bash
dotnet ef migrations add MigrationName --project src/Infrastructure --startup-project src/Web.Api
```

## Database Seeding

### Development Data

The application automatically seeds the database with sample data in development mode:

- 5 sample users (Alice Johnson, Bob Smith, Carol Davis, David Wilson, Eve Brown)
- 6 sample meetings demonstrating various scenarios including back-to-back meetings

### Custom Seeding

To modify the seed data, edit `src/Infrastructure/Database/DatabaseSeeder.cs`.

## Performance Considerations

### Connection Pooling

The application is configured with:

- Connection retry logic (3 retries with 5-second delays)
- Service provider caching enabled
- Optimized for production performance

### Query Optimization

- Indexes on frequently queried columns (start_time, end_time)
- Composite indexes for range queries
- Efficient conflict detection queries

## Health Checks

The application includes health checks for database connectivity:

- PostgreSQL health check for production
- In-memory database health check for development

Access health checks at: `GET /health`

## Troubleshooting

### Common Issues

1. **Connection String Issues**

   - Ensure PostgreSQL is running
   - Verify connection string format
   - Check firewall settings

2. **Migration Issues**

   - Ensure database exists
   - Check user permissions
   - Verify Entity Framework tools are installed

3. **Performance Issues**
   - Monitor query execution plans
   - Consider additional indexes for specific query patterns
   - Review connection pool settings

### Logging

Database operations are logged using Serilog. Check logs for:

- Connection issues
- Query performance
- Migration status
- Seeding results

## Security Considerations

- Use parameterized queries (handled by Entity Framework)
- Implement proper connection string security
- Consider database user permissions
- Enable SSL for production connections

## Backup and Recovery

For production environments:

- Implement regular database backups
- Test restore procedures
- Consider point-in-time recovery
- Monitor database size and performance
