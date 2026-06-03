FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY electrostoreWORKER.csproj .
RUN dotnet restore

COPY DTO/ DTO/
COPY Extensions/ Extensions/
COPY Grpc/ Grpc/
COPY Kafka/ Kafka/
COPY Mqtt/ Mqtt/
COPY Services/ Services/
COPY Program.cs ./

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://127.0.0.1:5000/health || exit 1

ENTRYPOINT ["dotnet", "electrostoreWORKER.dll"]
