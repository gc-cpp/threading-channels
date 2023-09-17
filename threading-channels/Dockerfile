# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /source
COPY *.sln .
COPY threading-channels/*.csproj ./threading-channels/
COPY threading-channels-tests/*.csproj ./threading-channels-tests/
RUN dotnet restore

# copy everything else and build app
COPY threading-channels/. ./threading-channels/
WORKDIR /source/threading-channels
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /perf
RUN apt-get update && apt-get install -y curl
RUN curl -OL https://aka.ms/perfcollect
RUN chmod +x perfcollect
RUN ./perfcollect install
WORKDIR /app
COPY --from=build /app ./
ENTRYPOINT ["dotnet", "threading-channels.dll"]