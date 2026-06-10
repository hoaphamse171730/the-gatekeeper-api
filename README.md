# The Gatekeeper API

Một API nhỏ để học deploy, CI/CD, API Gateway, log và debug theo hướng đơn giản cho fresher dev.

## Mục tiêu

Developer push code
-> GitHub Actions build/test
-> Deploy lên AWS
-> Request đi qua API Gateway
-> Backend xử lý request
-> Lỗi có `requestId` để truy vết trong log.

## Repo Structure

```text
the-gatekeeper-api/
  src/
    Gatekeeper.Api/
  tests/
    Gatekeeper.Api.Tests/
  postman/
  docs/
    assets/
  the-gatekeeper-api.sln
  README.md
```

## Project

- `src/Gatekeeper.Api`: ASP.NET Core Web API project.
- `tests/Gatekeeper.Api.Tests`: xUnit test project.
- `postman`: nơi lưu Postman collections/environments sau này.
- `docs`: ghi chú học tập, triển khai, debug và runbook.
- `docs/assets`: ảnh minh họa cho tài liệu.

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

## Chạy Local

```bash
dotnet restore the-gatekeeper-api.sln
dotnet build the-gatekeeper-api.sln
dotnet test the-gatekeeper-api.sln
dotnet run --project src/Gatekeeper.Api/Gatekeeper.Api.csproj
```

Swagger chạy ở môi trường Development theo cấu hình mặc định của template ASP.NET Core.

## Endpoint Cơ Bản

| Endpoint | Mục đích | Evidence |
| --- | --- | --- |
| `GET /public/ping` | Health check/public gate | API sống, trả `requestId` |
| `GET /version` | Deployment fingerprint | Biết version/env/commit/run |
| `GET /secure/treasure` | Protected gate | Thiếu key thì bị chặn |
| `GET /chaos/error` | Intentional failure | Tạo lỗi 500 có `requestId` |
| `GET /chaos/slow?ms=1500` | Latency demo | Request chậm có kiểm soát |

Protected endpoint dùng header demo:

```bash
X-Gatekeeper-Key: local-dev-key
```

## Next Steps

- Thêm GitHub Actions build/test.
- Thêm deploy lên AWS và API Gateway.
