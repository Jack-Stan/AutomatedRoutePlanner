# Hoppy Route Manager

Smart route management system for battery swapping services in Europe.

## 🚀 Project Overview

Hoppy Route Manager is a comprehensive solution for managing battery swapping operations across multiple European regions. The system provides role-based access control and real-time tracking for efficient fleet management.

## 🏗️ Architecture

- **Backend**: .NET Core API with Entity Framework
- **Frontend**: React Native mobile application with TypeScript
- **Database**: SQL Server with seeded data for 23 European regions
- **Authentication**: JWT-based role-based access control

## 👥 User Roles

- **Admin**: Complete system management and user administration
- **Fleet Manager**: Zone management and route optimization
- **Battery Swapper**: Route execution and vehicle status updates

## 🗺️ Supported Regions

### Belgium
- Cadzand, Kortrijk, Kempen, Sint-Niklaas, Genk, Mechelen, Blankenberge, Dender, Vlaamse Ardennen

### Spain
- Torrevieja, Orihuela, Albir, Lanzarote, Altea, La Nucia, Fuerteventura, Tenerife

### Greece
- Rhodos, Kos, Corfu, Rethymnon

### Gibraltar
- Gibraltar

## 🛠️ Features

- **Real-time Vehicle Tracking**: Monitor 50+ vehicles per zone
- **Route Optimization**: Intelligent route planning for battery swappers
- **Battery Monitoring**: Track battery levels and identify low-battery vehicles
- **Zone Management**: Manage operational zones across multiple countries
- **Dashboard Analytics**: Comprehensive statistics and reporting
- **Out-of-Zone Detection**: Identify vehicles outside operational zones

## 📱 Getting Started

### Prerequisites
- .NET 9.0 SDK
- Node.js 18+
- SQL Server or SQL Server Express
- Expo CLI (for React Native)

### Backend Setup
```bash
cd Backend/HoppyRouteApi
dotnet restore
dotnet run
```

### Frontend Setup
```bash
cd Frontend
npm install
npm start
```

### Database Setup
The application will automatically create and seed the database with:
- 23 regions across Europe
- 10 zones per region (200m x 200m coverage)
- 50 vehicles per zone (45 in-zone, 5 out-of-zone)
- An initial `admin` user (and a test `swapper1` user), only if their seed passwords are configured (see Configuration)

## 🧪 Testing

### Backend Tests
```bash
cd Backend/HoppyRoute.Tests
dotnet test
```

### Frontend Tests
```bash
cd Frontend
npm test
```

## 🔧 Configuration

### Secrets and configuration
No secrets are stored in `appsettings*.json`. Supply them through configuration overrides; ASP.NET Core
maps environment variables with `__` onto the `:` sections automatically.

| Setting | Environment variable | Notes |
|---|---|---|
| `Jwt:Secret` | `Jwt__Secret` | JWT signing key, at least 32 characters. Required; the API refuses to start without it. |
| `EmailSettings:SmtpPassword` | `EmailSettings__SmtpPassword` | SMTP password (for Gmail: an app password). Needed only for sending mail. |
| `Seed:AdminPassword` | `Seed__AdminPassword` | Password for the initial `admin` user, used only when the Users table is empty. If unset, no admin is seeded. |
| `Seed:SwapperPassword` | `Seed__SwapperPassword` | Password for the test `swapper1` user, same rule. Optional. |

Example (bash, local dev):

```bash
export Jwt__Secret="$(openssl rand -base64 48)"
export Seed__AdminPassword="<choose-a-strong-password>"
cd Backend/HoppyRouteApi && dotnet run
```

Or keep them in user-secrets, outside the repo:

```bash
cd Backend/HoppyRouteApi
dotnet user-secrets init
dotnet user-secrets set "Jwt:Secret" "<random key, 32+ chars>"
dotnet user-secrets set "Seed:AdminPassword" "<choose-a-strong-password>"
```

There are no default credentials: log in as `admin` with the password you set in `Seed:AdminPassword`.

### API Endpoints
- Base URL: `https://localhost:7001/api`
- Authentication: JWT Bearer Token
- Documentation: Available via Swagger UI

## 📊 Database Schema

- **Users**: Authentication and role management
- **Zones**: Operational areas with geographic boundaries
- **Vehicles**: Fleet inventory with real-time status
- **Routes**: Optimized paths for battery swapping
- **RouteStops**: Individual stops within routes

## 🚀 Deployment

The application is containerized and ready for deployment to:
- Azure App Service (Backend)
- Expo Application Services (Frontend)
- Azure SQL Database (Database)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## 📄 License

This project is licensed under the MIT License.

## 📞 Support

For support and questions, please create an issue in the GitHub repository.
