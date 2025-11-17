FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /app

COPY ["Controllers/", "DTO/", "Enums/", "Middleware/", "Migrations/", "Models/", "Services/", "Validators/", \
        "ApplicationDbContext.cs", "electrostore.csproj", "Mapper.cs", "Program.cs", "SwaggerClass.cs", "./"]

RUN dotnet restore

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

WORKDIR /app

COPY --from=build /app/out .

#COPY entrypoint.sh /entrypoint.sh
#RUN chmod +x /entrypoint.sh && chown -R 3216:3216 /app

#USER 3216:3216

EXPOSE 80

CMD ["dotnet", "electrostore.dll"]
