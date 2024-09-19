# electrostore

## Description
ce projet permet de gérer l'inventaire de vos espaces de rangement, localiser un élément, et plannifier les livraison et consommation de vos produits, pour suivre l'évolution de vos stocks et anticiper les ruptures.


documentation for the electrostore api
https://vampi62.github.io/electrostore/openapi/


## Installation
### scanBox Installation
#### description
dans la section "scanbox" vous trouverez les fichier 3d pour imprimer la boite de scan ainsi que le code arduino pour l'ESP32CAM
imprimez les fichiers (privilégiez la couleur blanche pour une meilleur détection de vos objets)

#### Matériel
- 1 - ESP32CAM
- 1 - WS2812B
- 1 - interrupteur ON/OFF

### LedStorage Installation
#### description
dans la section "scanbox" vous trouverez les fichier 3d pour imprimer le support qui se fixe a l'arrière de votre espace de rangement, vous trouverai aussi le code arduino pour l'ESP01
les fichiers sont conçus pour les casiers à tiroirs de chez Lidl
https://www.lidl.fr/p/parkside-casiers-a-tiroirs/p100377898

#### Matériel
- 1 - ESP-01
- X - WS2812B
- 1 - WS2812 LED controller for esp01

### Server Installation
prerequisites:
- docker + docker-compose
- mariadb server
- mqtt server

```bash
cd ~
git clone https://github.com/vampi62/electrostore

# build api
cd ~/electrostore/electrostoreAPI
# complete the appsettings.json file with your database and mqtt server credentials
# ConnectionStrings --> mariadb
# Mqtt --> mqtt
# SMTP (optional) set Enable to false if you don't want to use the email service
# JWT (key) set a random key for the jwt token
sudo nano appsettings.json

# build dev
docker build -t electrostoreapi:dev -f Dockerfile-debug .

# run dev
docker run -d \
  --name electrostoreAPI \
  --restart always \
  --network dockernet \
  --label "traefik.enable=true" \
  --label "traefik.http.routers.electrostoreAPI.rule=Host(\`yourDNSforAPI.net\`)" \
  --label "traefik.http.routers.electrostoreAPI.entrypoints=websecure" \
  --label "traefik.http.routers.electrostoreAPI.tls.certresolver=myresolver" \
  --label "traefik.http.services.electrostoreAPI.loadbalancer.server.port=80" \
  electrostoreapi:dev

# build frontend
cd ~/electrostore/electrostoreFRONT

# build dev
docker build -t electrostorefront:dev -f Dockerfile-debug .

# run dev
docker run -d \
  --name electrostoreFRONT \
  --restart always \
  --network dockernet \
  --label "traefik.enable=true" \
  --label "traefik.http.routers.electrostoreFRONT.rule=Host(\`yourDNSforFRONT.net\`)" \
  --label "traefik.http.routers.electrostoreFRONT.entrypoints=websecure" \
  --label "traefik.http.routers.electrostoreFRONT.tls.certresolver=myresolver" \
  --label "traefik.http.services.electrostoreFRONT.loadbalancer.server.port=3000" \
  electrostorefront:dev



```