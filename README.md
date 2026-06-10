# The Gatekeeper API

Một ASP.NET Core Web API nhỏ để học deploy, CI/CD, API Gateway, log và debug theo hướng đơn giản, dễ hiểu.

## Project Goal

Luồng học chính của repo:

```text
Developer push code
-> GitHub Actions build/test
-> Deploy lên AWS
-> Request đi qua API Gateway
-> Backend xử lý request
-> Lỗi có requestId để truy vết trong log
```

## Current Status

Day 2 đã có:

- Repo structure chuẩn cho .NET project.
- ASP.NET Core Web API trong `src/Gatekeeper.Api`.
- xUnit test project trong `tests/Gatekeeper.Api.Tests`.
- 5 endpoint demo cho gateway, protected gate, version, error và latency.
- Acceptance tests chạy HTTP thật qua Kestrel local.

## Repo Structure

```text
the-gatekeeper-api/
  src/
    Gatekeeper.Api/
      Controllers/
      GatekeeperApplication.cs
      Program.cs
  tests/
    Gatekeeper.Api.Tests/
      EndpointAcceptanceTests.cs
      GatekeeperControllerTests.cs
  postman/
  docs/
    assets/
  the-gatekeeper-api.sln
  README.md
```

## Main Projects

- `src/Gatekeeper.Api`: ASP.NET Core Web API.
- `tests/Gatekeeper.Api.Tests`: xUnit tests cho route contract và acceptance criteria.
- `postman`: nơi lưu Postman collections/environments sau này.
- `docs`: ghi chú học tập, triển khai, debug và runbook.
- `docs/assets`: ảnh minh họa cho tài liệu.

## Quick Start

Yêu cầu local:

- .NET SDK 8 trở lên.

Chạy build và test:

```bash
dotnet restore the-gatekeeper-api.sln
dotnet build the-gatekeeper-api.sln
dotnet test the-gatekeeper-api.sln
```

Chạy API local:

```bash
dotnet run --project src/Gatekeeper.Api/Gatekeeper.Api.csproj
```

Khi chạy bằng profile mặc định, API thường listen ở:

```text
http://localhost:5244
https://localhost:7239
```

Swagger bật trong môi trường Development.

## Endpoint Contract

| Endpoint | Mục đích | Acceptance evidence |
| --- | --- | --- |
| `GET /public/ping` | Health check/public gate | API sống, trả `requestId` |
| `GET /version` | Deployment fingerprint | Biết `version`, `environment`, `commit`, `run` |
| `GET /secure/treasure` | Protected gate | Thiếu key thì bị chặn |
| `GET /chaos/error` | Intentional failure | Tạo lỗi `500` có `requestId` |
| `GET /chaos/slow?ms=1500` | Latency demo | Request chậm có kiểm soát |

## Local API Examples

Set base URL theo port local của bạn:

```bash
BASE_URL=http://localhost:5244
```

Public ping:

```bash
curl "$BASE_URL/public/ping"
```

Expected:

```json
{
  "status": "ok",
  "gate": "public",
  "requestId": "..."
}
```

Version fingerprint:

```bash
curl "$BASE_URL/version"
```

Expected:

```json
{
  "service": "Gatekeeper.Api",
  "version": "1.0.0.0",
  "environment": "Development",
  "commit": "local",
  "run": "local",
  "requestId": "..."
}
```

Protected gate without key:

```bash
curl -i "$BASE_URL/secure/treasure"
```

Expected: `401 Unauthorized`.

Protected gate with key:

```bash
curl -H "X-Gatekeeper-Key: local-dev-key" "$BASE_URL/secure/treasure"
```

Intentional error:

```bash
curl -i "$BASE_URL/chaos/error"
```

Expected: `500 Internal Server Error` with `requestId`.

Controlled latency:

```bash
curl "$BASE_URL/chaos/slow?ms=1500"
```

Expected:

```json
{
  "status": "completed",
  "requestedDelayMs": 1500,
  "requestId": "..."
}
```

## Configuration

Local protected endpoint dùng key demo:

```json
{
  "Gatekeeper": {
    "ApiKey": "local-dev-key"
  }
}
```

Header cần gửi:

```text
X-Gatekeeper-Key: local-dev-key
```

`GET /version` đọc thông tin deploy từ environment variables nếu có:

- `GITHUB_SHA`, `COMMIT_SHA`, hoặc `APP_COMMIT`
- `GITHUB_RUN_ID` hoặc `APP_RUN_ID`

Nếu không có, API trả `local`.

## Tests

Test suite hiện có:

- `GatekeeperControllerTests`: kiểm tra controller expose đúng 5 route.
- `EndpointAcceptanceTests`: chạy HTTP thật qua Kestrel local và kiểm tra response/status code/requestId.

Chạy test:

```bash
dotnet test the-gatekeeper-api.sln
```

Acceptance criteria đang được test:

- `/public/ping` trả `200` và có `requestId`.
- `/version` trả deployment fingerprint.
- `/secure/treasure` thiếu key trả `401`.
- `/secure/treasure` đúng key trả `200`.
- `/chaos/error` trả `500` và có `requestId`.
- `/chaos/slow?ms=150` có delay có kiểm soát và trả `requestedDelayMs`.

## Tạo Structure Bằng .NET Template

Các lệnh tương đương để tạo layout này từ đầu:

```bash
dotnet new sln -n the-gatekeeper-api
dotnet new webapi -n Gatekeeper.Api -o src/Gatekeeper.Api --framework net8.0
dotnet new xunit -n Gatekeeper.Api.Tests -o tests/Gatekeeper.Api.Tests --framework net8.0
dotnet sln the-gatekeeper-api.sln add src/Gatekeeper.Api/Gatekeeper.Api.csproj
dotnet sln the-gatekeeper-api.sln add tests/Gatekeeper.Api.Tests/Gatekeeper.Api.Tests.csproj
dotnet add tests/Gatekeeper.Api.Tests/Gatekeeper.Api.Tests.csproj reference src/Gatekeeper.Api/Gatekeeper.Api.csproj
```

## Next Steps

- Thêm GitHub Actions build/test.
- Thêm Dockerfile.
- Thêm deploy lên AWS.
- Đặt API Gateway trước backend.
- Gửi log/error có `requestId` lên CloudWatch.
- Viết troubleshooting runbook.
