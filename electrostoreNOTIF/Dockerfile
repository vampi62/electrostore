FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /app

COPY electrostoreNOTIF.csproj .
RUN dotnet restore

COPY ["DTO/", "Extensions/", "Grpc/", "Kafka/", "Services/", \
        "electrostoreNOTIF.csproj", "Program.cs", "./"]

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

WORKDIR /app

COPY --from=build /app/out .

#COPY entrypoint.sh /entrypoint.sh
#RUN chmod +x /entrypoint.sh && chown -R 3216:3216 /app

#USER 3216:3216

EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "electrostoreNOTIF.dll"]
