# Utilisez l'image d'ASP.NET 7 SDK comme base
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
# Définissez le répertoire de travail
WORKDIR /app

# Copiez les fichiers de votre projet dans le conteneur
COPY . .

# Restaurez les dépendances et construisez l'application
RUN dotnet restore

RUN dotnet publish -c Release -o out

# Utilisez une image légère ASP.NET 7 pour l'exécution
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime

# Définissez le répertoire de travail pour l'exécution
WORKDIR /app

# Copiez les fichiers publiés à partir de l'étape de construction
COPY --from=build /app/out .

# Exposez le port sur lequel l'application écoute
EXPOSE 80

# Exposez le port sur lequel l'application écoute en HTTPS
#EXPOSE 443

# Définissez la variable d'environnement ASPNETCORE_URLS pour spécifier HTTPS
#ENV ASPNETCORE_URLS=https://+:443
#ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/certificates/fullchain.pem
#ENV ASPNETCORE_Kestrel__Certificates__Default__KeyPath=/certificates/privkey.pem


# Définissez la commande pour lancer l'application au démarrage du conteneur
CMD ["dotnet", "electrostore.dll"]
