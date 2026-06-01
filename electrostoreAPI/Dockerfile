FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app

COPY electrostoreAPI.csproj .
RUN dotnet restore

COPY Controllers/ Controllers/
COPY DTO/ DTO/
COPY Enums/ Enums/
COPY Extensions/ Extensions/
COPY Middleware/ Middleware/
COPY Migrations/ Migrations/
COPY Models/ Models/
COPY Grpc/ Grpc/
COPY Kafka/ Kafka/
COPY Services/ Services/
COPY Validators/ Validators/
COPY ApplicationDbContext.cs Mapper.cs Program.cs SwaggerClass.cs ./

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime

WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

#COPY entrypoint.sh /entrypoint.sh
#RUN chmod +x /entrypoint.sh && chown -R 3216:3216 /app

#USER 3216:3216

EXPOSE 5000
EXPOSE 5001

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://127.0.0.1:5000/health || exit 1

CMD ["dotnet", "electrostoreAPI.dll"]
