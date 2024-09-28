# Utiliser une image de base officielle Python
FROM python:3.11-slim

# Définir le répertoire de travail dans le conteneur
WORKDIR /app

# Copier le fichier requirements.txt (pour les dépendances)
COPY requirements.txt .

# Installer les dépendances Python
RUN pip install --no-cache-dir -r requirements.txt

# Copier le reste de l'application dans le conteneur
COPY . .

# Exposer le port sur lequel Flask va tourner
EXPOSE 5000

# Démarrer le serveur Flask
CMD ["python", "main.py"]
