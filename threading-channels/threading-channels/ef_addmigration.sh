dotnet build
dotnet tool restore
dotnet ef migrations add $(date +%s) --context UserActionContext
