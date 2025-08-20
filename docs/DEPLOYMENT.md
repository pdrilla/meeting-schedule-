# Deployment Guide

This guide covers deploying the Meeting Scheduler API to various environments including Docker, cloud platforms, and on-premises servers.

## Prerequisites

- [.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/) (for production)
- [Docker](https://www.docker.com/) (optional)

## Environment Configuration

### Development Environment

Uses in-memory database by default. No additional configuration required.

```bash
dotnet run --project src/Web.Api
```

### Staging/Production Environment

Requires PostgreSQL database and proper configuration.

## Docker Deployment

### 1. Create Dockerfile

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Web.Api/Web.Api.csproj", "src/Web.Api/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/SharedKernel/SharedKernel.csproj", "src/SharedKernel/"]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]

# Restore dependencies
RUN dotnet restore "src/Web.Api/Web.Api.csproj"

# Copy source code
COPY . .

# Build application
WORKDIR "/src/src/Web.Api"
RUN dotnet build "Web.Api.csproj" -c Release -o /app/build

# Publish application
FROM build AS publish
RUN dotnet publish "Web.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published application
COPY --from=publish /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Web.Api.dll"]
```

### 2. Create Docker Compose

```yaml
# docker-compose.yml
version: "3.8"

services:
  meeting-scheduler:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__Database=Host=postgres;Database=MeetingScheduler;Username=postgres;Password=postgres123
      - Serilog__WriteTo__1__Args__serverUrl=http://seq:5341
    depends_on:
      - postgres
      - seq
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

  postgres:
    image: postgres:16
    environment:
      - POSTGRES_DB=MeetingScheduler
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=postgres123
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    restart: unless-stopped
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 10s
      timeout: 5s
      retries: 5

  seq:
    image: datalust/seq:latest
    environment:
      - ACCEPT_EULA=Y
    ports:
      - "5341:80"
    volumes:
      - seq_data:/data
    restart: unless-stopped

volumes:
  postgres_data:
  seq_data:
```

### 3. Build and Deploy

```bash
# Build and start services
docker-compose up -d

# Check service status
docker-compose ps

# View logs
docker-compose logs -f meeting-scheduler

# Apply database migrations
docker-compose exec meeting-scheduler dotnet ef database update --project /app/Infrastructure.dll --startup-project /app/Web.Api.dll
```

## Cloud Deployment

### Azure Container Instances

```bash
# Create resource group
az group create --name meeting-scheduler-rg --location eastus

# Create container registry
az acr create --resource-group meeting-scheduler-rg --name meetingscheduler --sku Basic

# Build and push image
az acr build --registry meetingscheduler --image meeting-scheduler:latest .

# Create PostgreSQL server
az postgres server create \
  --resource-group meeting-scheduler-rg \
  --name meeting-scheduler-db \
  --location eastus \
  --admin-user dbadmin \
  --admin-password YourSecurePassword123! \
  --sku-name GP_Gen5_2

# Create database
az postgres db create \
  --resource-group meeting-scheduler-rg \
  --server-name meeting-scheduler-db \
  --name MeetingScheduler

# Deploy container
az container create \
  --resource-group meeting-scheduler-rg \
  --name meeting-scheduler \
  --image meetingscheduler.azurecr.io/meeting-scheduler:latest \
  --dns-name-label meeting-scheduler-api \
  --ports 80 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Database="Host=meeting-scheduler-db.postgres.database.azure.com;Database=MeetingScheduler;Username=dbadmin@meeting-scheduler-db;Password=YourSecurePassword123!;SslMode=Require"
```

### AWS ECS with Fargate

```json
{
  "family": "meeting-scheduler",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "executionRoleArn": "arn:aws:iam::account:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "meeting-scheduler",
      "image": "your-account.dkr.ecr.region.amazonaws.com/meeting-scheduler:latest",
      "portMappings": [
        {
          "containerPort": 8080,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        },
        {
          "name": "ConnectionStrings__Database",
          "value": "Host=your-rds-endpoint;Database=MeetingScheduler;Username=dbuser;Password=dbpass"
        }
      ],
      "logConfiguration": {
        "logDriver": "awslogs",
        "options": {
          "awslogs-group": "/ecs/meeting-scheduler",
          "awslogs-region": "us-east-1",
          "awslogs-stream-prefix": "ecs"
        }
      },
      "healthCheck": {
        "command": [
          "CMD-SHELL",
          "curl -f http://localhost:8080/health || exit 1"
        ],
        "interval": 30,
        "timeout": 5,
        "retries": 3
      }
    }
  ]
}
```

### Google Cloud Run

```bash
# Build and push to Google Container Registry
gcloud builds submit --tag gcr.io/PROJECT_ID/meeting-scheduler

# Deploy to Cloud Run
gcloud run deploy meeting-scheduler \
  --image gcr.io/PROJECT_ID/meeting-scheduler \
  --platform managed \
  --region us-central1 \
  --allow-unauthenticated \
  --set-env-vars ASPNETCORE_ENVIRONMENT=Production \
  --set-env-vars ConnectionStrings__Database="Host=CLOUD_SQL_IP;Database=MeetingScheduler;Username=postgres;Password=PASSWORD"
```

## On-Premises Deployment

### Linux Server (Ubuntu/Debian)

```bash
# Install .NET runtime
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-9.0

# Create application user
sudo useradd --system --shell /bin/false --home /opt/meeting-scheduler meetingscheduler

# Create application directory
sudo mkdir -p /opt/meeting-scheduler
sudo chown meetingscheduler:meetingscheduler /opt/meeting-scheduler

# Copy application files
sudo cp -r /path/to/published/app/* /opt/meeting-scheduler/
sudo chown -R meetingscheduler:meetingscheduler /opt/meeting-scheduler

# Create systemd service
sudo tee /etc/systemd/system/meeting-scheduler.service > /dev/null <<EOF
[Unit]
Description=Meeting Scheduler API
After=network.target

[Service]
Type=notify
User=meetingscheduler
Group=meetingscheduler
WorkingDirectory=/opt/meeting-scheduler
ExecStart=/usr/bin/dotnet /opt/meeting-scheduler/Web.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=meeting-scheduler
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
EOF

# Enable and start service
sudo systemctl daemon-reload
sudo systemctl enable meeting-scheduler
sudo systemctl start meeting-scheduler

# Check status
sudo systemctl status meeting-scheduler
```

### Windows Server (IIS)

```powershell
# Install .NET runtime
Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/..." -OutFile "dotnet-runtime.exe"
.\dotnet-runtime.exe /quiet

# Install IIS and ASP.NET Core Module
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-WebServer, IIS-CommonHttpFeatures, IIS-HttpErrors, IIS-HttpLogging, IIS-RequestFiltering, IIS-StaticContent, IIS-DefaultDocument
Invoke-WebRequest -Uri "https://download.visualstudio.microsoft.com/download/pr/.../dotnet-hosting-win.exe" -OutFile "dotnet-hosting.exe"
.\dotnet-hosting.exe /quiet

# Create application pool
Import-Module WebAdministration
New-WebAppPool -Name "MeetingScheduler" -Force
Set-ItemProperty -Path "IIS:\AppPools\MeetingScheduler" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
Set-ItemProperty -Path "IIS:\AppPools\MeetingScheduler" -Name "managedRuntimeVersion" -Value ""

# Create website
New-Website -Name "MeetingScheduler" -ApplicationPool "MeetingScheduler" -PhysicalPath "C:\inetpub\wwwroot\meeting-scheduler" -Port 80

# Copy application files to C:\inetpub\wwwroot\meeting-scheduler\

# Configure environment variables in web.config
```

## Database Setup

### PostgreSQL Production Setup

```sql
-- Create database and user
CREATE DATABASE "MeetingScheduler";
CREATE USER meetingscheduler WITH PASSWORD 'secure_password_here';
GRANT ALL PRIVILEGES ON DATABASE "MeetingScheduler" TO meetingscheduler;

-- Connect to the database
\c MeetingScheduler

-- Grant schema permissions
GRANT ALL ON SCHEMA public TO meetingscheduler;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO meetingscheduler;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO meetingscheduler;
```

### Apply Migrations

```bash
# Set connection string
export ConnectionStrings__Database="Host=localhost;Database=MeetingScheduler;Username=meetingscheduler;Password=secure_password_here"

# Apply migrations
dotnet ef database update --project src/Infrastructure --startup-project src/Web.Api

# Or using the published application
dotnet Web.Api.dll --migrate
```

## Configuration Management

### Environment Variables

```bash
# Database
export ConnectionStrings__Database="Host=db-server;Database=MeetingScheduler;Username=user;Password=pass"

# Logging
export Serilog__MinimumLevel__Default="Information"
export Serilog__WriteTo__1__Args__serverUrl="http://seq-server:5341"
export Serilog__WriteTo__1__Args__apiKey="your-seq-api-key"

# Application
export ASPNETCORE_ENVIRONMENT="Production"
export ASPNETCORE_URLS="http://+:8080"
```

### Configuration Files

**appsettings.Production.json:**

```json
{
  "ConnectionStrings": {
    "Database": "Host=prod-db;Database=MeetingScheduler;Username=app_user;Password=secure_password"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "Application.MeetingScheduler": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://seq-prod:5341",
          "apiKey": "production-api-key"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "/app/logs/meeting-scheduler-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      }
    ]
  }
}
```

## Security Considerations

### HTTPS Configuration

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://+:8080"
      },
      "Https": {
        "Url": "https://+:8081",
        "Certificate": {
          "Path": "/app/certs/certificate.pfx",
          "Password": "certificate_password"
        }
      }
    }
  }
}
```

### Reverse Proxy (Nginx)

```nginx
server {
    listen 80;
    server_name meeting-scheduler.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name meeting-scheduler.yourdomain.com;

    ssl_certificate /etc/ssl/certs/meeting-scheduler.crt;
    ssl_certificate_key /etc/ssl/private/meeting-scheduler.key;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    location /health {
        proxy_pass http://localhost:8080/health;
        access_log off;
    }
}
```

## Monitoring and Alerting

### Health Check Monitoring

```bash
#!/bin/bash
# health-check.sh

ENDPOINT="https://meeting-scheduler.yourdomain.com/health"
TIMEOUT=10

response=$(curl -s -w "%{http_code}" --max-time $TIMEOUT "$ENDPOINT")
http_code="${response: -3}"

if [ "$http_code" != "200" ]; then
    echo "Health check failed: HTTP $http_code"
    # Send alert (email, Slack, etc.)
    exit 1
fi

echo "Health check passed"
```

### Log Monitoring

```bash
# Monitor error logs
tail -f /app/logs/meeting-scheduler-*.log | grep -i error

# Monitor performance
tail -f /app/logs/meeting-scheduler-*.log | grep "ElapsedMs" | awk '{if($NF > 1000) print}'
```

## Backup and Recovery

### Database Backup

```bash
#!/bin/bash
# backup-database.sh

DB_HOST="localhost"
DB_NAME="MeetingScheduler"
DB_USER="meetingscheduler"
BACKUP_DIR="/backups"
DATE=$(date +%Y%m%d_%H%M%S)

# Create backup
pg_dump -h $DB_HOST -U $DB_USER -d $DB_NAME -f "$BACKUP_DIR/meeting-scheduler-$DATE.sql"

# Compress backup
gzip "$BACKUP_DIR/meeting-scheduler-$DATE.sql"

# Remove old backups (keep 30 days)
find $BACKUP_DIR -name "meeting-scheduler-*.sql.gz" -mtime +30 -delete
```

### Application Backup

```bash
#!/bin/bash
# backup-application.sh

APP_DIR="/opt/meeting-scheduler"
BACKUP_DIR="/backups/app"
DATE=$(date +%Y%m%d_%H%M%S)

# Create application backup
tar -czf "$BACKUP_DIR/meeting-scheduler-app-$DATE.tar.gz" -C "$APP_DIR" .

# Remove old backups
find $BACKUP_DIR -name "meeting-scheduler-app-*.tar.gz" -mtime +7 -delete
```

## Scaling Considerations

### Horizontal Scaling

The application is stateless and can be scaled horizontally:

```yaml
# docker-compose.scale.yml
version: "3.8"

services:
  meeting-scheduler:
    # ... existing configuration
    deploy:
      replicas: 3

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
    depends_on:
      - meeting-scheduler
```

### Load Balancer Configuration

```nginx
upstream meeting_scheduler {
    server meeting-scheduler_1:8080;
    server meeting-scheduler_2:8080;
    server meeting-scheduler_3:8080;
}

server {
    listen 80;

    location / {
        proxy_pass http://meeting_scheduler;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    }
}
```

## Troubleshooting Deployment

### Common Issues

1. **Port Binding Errors**

   - Check if ports are already in use
   - Verify firewall settings
   - Ensure proper port configuration

2. **Database Connection Issues**

   - Verify connection string format
   - Check database server accessibility
   - Confirm credentials and permissions

3. **SSL Certificate Issues**

   - Verify certificate validity
   - Check certificate path and permissions
   - Ensure proper certificate format

4. **Performance Issues**
   - Monitor resource usage (CPU, memory)
   - Check database query performance
   - Review application logs for bottlenecks

### Deployment Checklist

- [ ] Database server configured and accessible
- [ ] Connection strings properly set
- [ ] SSL certificates installed (if using HTTPS)
- [ ] Firewall rules configured
- [ ] Health checks responding
- [ ] Logging configured and working
- [ ] Monitoring and alerting set up
- [ ] Backup procedures in place
- [ ] Security hardening applied
- [ ] Performance testing completed
