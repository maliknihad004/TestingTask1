
# Vehicle Management API

A backend REST API for managing vehicles in a Palestinian automotive showroom.

## Technologies

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core
- PostgreSQL
- JWT Authentication
- xUnit
- Moq
- WebApplicationFactory
- In-Memory Database

## Features

- Add, update, and delete vehicles
- Get vehicles by ID
- Get available vehicles
- Filter vehicles by brand and model
- Update vehicle availability
- User registration and login
- Role-based authorization

## Database Setup

Create the PostgreSQL database:

```sql
CREATE DATABASE "ShowroomDB";
```

Create a `.env` file inside `TestingTask1`:

```env
DB_CONNECTION_STRING=Host=localhost;Port=5432;Database=ShowroomDB;Username=postgres;Password=YOUR_PASSWORD
JWT_KEY=YOUR_RANDOM_SECRET_KEY_AT_LEAST_32_CHARACTERS
```

Replace the example values with your own.

## Build and Run

Open the terminal inside `TestingTask1`.

Build the project:

```bash
dotnet restore
dotnet build
```

Apply database migrations:

```bash
dotnet ef database update
```

Run the API:

```bash
dotnet run --urls http://localhost:5001
```

## API Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | /api/auth/register | Register user |
| POST | /api/auth/login | Login |
| GET | /api/vehicles | Get available vehicles |
| GET | /api/vehicles/{id} | Get vehicle by ID |
| POST | /api/vehicles | Add vehicle |
| PUT | /api/vehicles/{id} | Update vehicle |
| DELETE | /api/vehicles/{id} | Delete vehicle |
| PATCH | /api/vehicles/{id}/availability | Update availability |

## Testing

The project includes unit tests using xUnit and Moq, and integration tests using WebApplicationFactory with an In-Memory database.

Run these commands from the `TestingTask1` folder.

### Unit Tests

```bash
dotnet test ../TestingTask1.Tests/TestingTask1.Tests.csproj --filter FullyQualifiedName~VehicleServiceTests
```

### Integration Tests

```bash
dotnet test ../TestingTask1.Tests/TestingTask1.Tests.csproj --filter FullyQualifiedName~VehiclesApiTests
```

### Run All Tests

```bash
dotnet test ../TestingTask1.Tests/TestingTask1.Tests.csproj
```

### Test Coverage

```bash
dotnet test ../TestingTask1.Tests/TestingTask1.Tests.csproj --collect:"XPlat Code Coverage"
```

Coverage results are generated inside the `TestResults` folder.

## Test Results

- Total Tests: 31
- Passed: 31
- Failed: 0
- Skipped: 0
- Vehicle Module Line Coverage: 96.03%
- Vehicle Module Branch Coverage: 87.04%

See `COVERAGE_REPORT.md` for more details.
