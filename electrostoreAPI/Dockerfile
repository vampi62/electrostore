FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build

WORKDIR /app

COPY . .

RUN dotnet restore

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS runtime

WORKDIR /app

COPY --from=build /app/out .

#RUN chown -R 3216:3216 /app

#USER 3216:3216

EXPOSE 80

CMD ["dotnet", "electrostore.dll"]
