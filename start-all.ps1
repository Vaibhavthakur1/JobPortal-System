$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$services = @(
    @{ Name = "IdentityService";     Port = 5001; Path = "src/Services/IdentityService/IdentityService.csproj" },
    @{ Name = "JobCatalogService";   Port = 5002; Path = "src/Services/JobCatalogService/JobCatalogService.csproj" },
    @{ Name = "ApplicationService";  Port = 5003; Path = "src/Services/ApplicationService/ApplicationService.csproj" },
    @{ Name = "ResumeService";       Port = 5004; Path = "src/Services/ResumeService/ResumeService.csproj" },
    @{ Name = "RecruiterService";    Port = 5005; Path = "src/Services/RecruiterService/RecruiterService.csproj" },
    @{ Name = "PaymentService";      Port = 5006; Path = "src/Services/PaymentService/PaymentService.csproj" },
    @{ Name = "NotificationService"; Port = 5007; Path = "src/Services/NotificationService/NotificationService.csproj" },
    @{ Name = "AdminService";        Port = 5008; Path = "src/Services/AdminService/AdminService.csproj" },
    @{ Name = "ApiGateway";          Port = 5000; Path = "src/Gateway/ApiGateway/ApiGateway.csproj" }
)

foreach ($svc in $services) {
    $proj = Join-Path $root $svc.Path
    $cmd = "dotnet run --project `"$proj`" --urls http://localhost:$($svc.Port)"
    Start-Process powershell -ArgumentList "-NoExit", "-Command", $cmd -WindowStyle Normal
    Write-Host "Started $($svc.Name) on port $($svc.Port)"
    Start-Sleep -Seconds 1
}

Write-Host ""
Write-Host "All services started. Swagger URLs:"
Write-Host "  Identity:     http://localhost:5001/swagger"
Write-Host "  JobCatalog:   http://localhost:5002/swagger"
Write-Host "  Application:  http://localhost:5003/swagger"
Write-Host "  Resume:       http://localhost:5004/swagger"
Write-Host "  Recruiter:    http://localhost:5005/swagger"
Write-Host "  Payment:      http://localhost:5006/swagger"
Write-Host "  Notification: http://localhost:5007/swagger"
Write-Host "  Admin:        http://localhost:5008/swagger"
Write-Host "  Gateway:      http://localhost:5000"
