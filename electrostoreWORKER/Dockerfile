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

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

WORKDIR /app

COPY --from=build /app/out .

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "electrostoreWORKER.dll"]
