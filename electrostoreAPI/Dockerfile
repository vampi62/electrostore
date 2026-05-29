FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /app

COPY ["Controllers/", "DTO/", "Enums/", "Extensions/", "Middleware/", "Migrations/", "Models/", "Grpc/", "Kafka/", "Services/", "Validators/", \
        "ApplicationDbContext.cs", "electrostoreAPI.csproj", "Mapper.cs", "Program.cs", "SwaggerClass.cs", "./"]

RUN dotnet restore

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

WORKDIR /app

COPY --from=build /app/out .

#COPY entrypoint.sh /entrypoint.sh
#RUN chmod +x /entrypoint.sh && chown -R 3216:3216 /app

#USER 3216:3216

EXPOSE 80
EXPOSE 5001

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:5234/health || exit 1

CMD ["dotnet", "electrostoreAPI.dll"]
