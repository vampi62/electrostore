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
- traefik with docker provider and letsencrypt enabled
- mariadb server
- mqtt server

#### clone the repository
```bash
cd ~
git clone https://github.com/vampi62/electrostore
cd electrostore
```

#### complete confi file
```bash
# API configuration
sudo nano electrostoreAPI/appsettings.json
# complete the appsettings.json file with your database and mqtt server credentials
# ConnectionStrings --> mariadb
# Mqtt --> mqtt
# SMTP (optional) set Enable to false if you don't want to use the email service
# JWT (key) set a random key for the jwt token

# FRONT configuration
sudo nano electrostoreFRONT/.env
# complete the .env file with your api server url

# DOCKER-COMPOSE configuration
sudo nano docker-compose.yml
# change the traefik labels with your domain name for the api and front
```

#### install the database
for install the database execute the file "electrostore.sql" in your mariadb server

#### build and run the server
```bash
docker-compose up -d --build
```
