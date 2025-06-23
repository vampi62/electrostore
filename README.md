# electrostore
## Description
This project allows you to manage the inventory of your storage spaces, locate an item, and plan the delivery and consumption of your products, in order to track stock levels and anticipate shortages.

Documentation for the Electrostore API:  
[https://vampi62.github.io/electrostore/openapi/](https://vampi62.github.io/electrostore/openapi/)

## Installation
### ScanBox Installation
#### Description
In the "scanbox" section, you will find the 3D files to print the scan box, as well as the Arduino code for the ESP32CAM.  
Print the files (preferably in white for better object detection).

#### Materials
- 1 - ESP32CAM
- 1 - WS2812B
- 1 - ON/OFF switch

### LedStorage Installation
#### Description
In the "scanbox" section, you will find the 3D files to print the mount that attaches to the back of your storage space, as well as the Arduino code for the ESP01.  
The files are designed for drawer cabinets from Lidl.
https://www.lidl.fr/p/parkside-casiers-a-tiroirs/p100377898

#### Matériel
- 1 - ESP-01
- X - WS2812B
- 1 - WS2812 LED controller for esp01

### Server Installation
prerequisites:
- docker + docker-compose
- (recommended) traefik with docker provider and letsencrypt enabled
- mariadb server
- mqtt server

#### clone the repository
```bash
git clone https://github.com/vampi62/electrostore
cd electrostore
```

#### complete config file
```bash
# API configuration
sudo nano electrostoreAPI/config/appsettings.json
# complete the appsettings.json file with your database and mqtt server credentials
# ConnectionStrings --> mariadb
# Mqtt --> mqtt
# SMTP (optional) set Enable to false if you don't want to use the email service
# JWT (key) set a random key for the jwt token
```
note: after the first lauch, if you want update the config file, you need to edit the config file in the volume "electrostoreCONFIG" and restart the container

#### start database server
if you have already a mariadb server and a mqtt server, you can skip this step
```bash
sudo docker run -d --name mariadb \
 --restart always \
 -p 3306:3306 \
 -e MYSQL_ROOT_PASSWORD=electrostore \
 -e MYSQL_DATABASE=electrostore \
 -e MYSQL_USER=electrostore \
 -e MYSQL_PASSWORD=electrostore \
 -v electrostoreDB:/var/lib/mysql \
 mariadb:11.7.2


sudo docker run -d --name mqtt \
 --restart always \
 -p 1883:1883 \
 -v electrostoreMQTT:/mosquitto/data \
 -v $(pwd)/MQTTCONF:/mosquitto/config \
 -v electrostoreLOG:/mosquitto/log \
 eclipse-mosquitto:2.0.20
```

#### create network
```bash
sudo docker network create electrostore

sudo docker network connect electrostore mariadb

sudo docker network connect electrostore mqtt
```

#### start the API
```bash
sudo docker build -t electrostore/api:latest electrostoreAPI
sudo docker run -d --name electrostoreAPI \
 --restart always \
 --network electrostore \
 -p 5002:80 \
 -v electrostoreDATA:/app/wwwroot \
 -v electrostoreCONFIG:/app/config \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 electrostore/api:latest

sudo docker build -t electrostore/ia:latest electrostoreIA
sudo docker run -d --name electrostoreIA \
 --restart always \
 --network electrostore \
 -v electrostoreDATA:/data \
 -v electrostoreCONFIG:/app/config:ro \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 --cap-drop ALL \
 electrostore/ia:latest
```


#### start the web interface
set VUE_API_URL with the complete url of the API (ex: https://api.electrostore.com:443/api)
```bash
sudo docker build -t electrostore/front:latest electrostoreFRONT
sudo docker run -d --name electrostoreFRONT \
 --restart always \
 -p 8080:80 \
 -e VUE_API_URL=<VUE_API_URL> \
 --security-opt no-new-privileges=true \
 --cap-drop ALL \
 electrostore/front:latest
```

## login to the web interface
Open your web browser and navigate to `http://<your-server-ip>:8080`. You should see the ElectroStore web interface.
use the default credentials:
- **Username**: admin@localhost.local
- **Password**: Admin@1234

## Documentation
You can find the documentation for the Electrostore API at:
[installation](/docs/01_installation.md)
[esp led and scanbox](/docs/02_storeLed_and_scanner.md)
[web interface](/docs/03_frontend_usage.md)
[api usage](/docs/04_api_usage.md)