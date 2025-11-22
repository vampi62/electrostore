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
for using a stable version follow the instructions in the [docs/01_installation.md](docs/01_installation.md) file, otherwise you can follow the instructions below to install the development version.

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

#### create and Complete config file
```bash
sudo mkdir /opt/electrostore && cd /opt/electrostore
sudo nano appsettings.json
```
Complete the `appsettings.json` file with the following content, replacing placeholders with your actual values:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=mariadb;Port=3306;Database=electrostore;Uid=electrostore;Pwd=electrostore;"
  },
  "MQTT": {
    "Username": "electrostore",
    "Password": "QHF8Gmq3oa2L117FmLqC",
    "Server": "mqtt",
    "Port": 1883,
    "ClientId": "electroapi"
  },
  "SMTP": {
    "Enable": "false",
    "Host": "<your-smtp-server (optional)>",
    "Port": 587,
    "Username": "<your-email (optional)>",
    "Password": "<your-email-password (optional)>",
    "From": "<your-email (optional)>",
  },
  "Jwt": {
    "Key": "<your-random-key>",
    "Issuer": "<your-issuer>",
    "Audience": "<your-audience>",
    "ExpireDays": 1
  },
  "OAuth": {
    "<method-name>": {
      "ClientId": "<client-id>",
      "ClientSecret": "<client-secret>",
      "Authority": "https://<sso-server-url>/application/o/authorize/",
      "RedirectUri": "https://<frontend-url>/auth/callback",
      "Scope": "openid profile email",
      "DisplayName": "<method-name>",
      "IconUrl": "<method-icon-url>"
    }
  },
  "S3": {
    "Enable": "false",
    "Endpoint": "<minio-url>:9000",
    "AccessKey": "<access-key>",
    "SecretKey": "<secret-key>",
    "BucketName": "electrostore",
    "Region": "garage",
    "Secure": "false"
  },
  "AllowedOrigins": [
    "https://<your-frontend-domain1>",
    "https://<your-frontend-domain2>"
  ],
  "FrontendUrl": "http://<frontend-url>",
  "AllowedHosts": "*"
}
```

#### start the API
```bash
sudo docker build -t ghcr.io/vampi62/electrostore/api:local electrostoreAPI
sudo docker run -d --name electrostoreAPI \
 --restart always \
 --network electrostore \
 -p 5002:8080 \
 -v electrostoreDATADEV:/app/wwwroot \
 -v /opt/electrostore:/app/config:ro \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 --cap-drop ALL \
 --cap-add CHOWN \
 --cpus=2 \
 --memory=1g \
 ghcr.io/vampi62/electrostore/api:local

sudo docker build -t ghcr.io/vampi62/electrostore/ia:local electrostoreIA
sudo docker run -d --name electrostoreIA \
 --restart always \
 --network electrostore \
 -v electrostoreDATADEV:/data \
 -v /opt/electrostore:/app/config:ro \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 --cap-drop ALL \
 --cap-add CHOWN \
 --cpus=2 \
 --memory=2g \
 ghcr.io/vampi62/electrostore/ia:local
```


#### start the web interface
set VUE_API_URL with the complete url of the API (ex: https://api.electrostore.com:443/api)
```bash
sudo docker build -t ghcr.io/vampi62/electrostore/front:local electrostoreFRONT
sudo docker run -d --name electrostoreFRONT \
 --restart always \
 --network electrostore \
 -p 8080:80 \
 -e VUE_API_URL=<VUE_API_URL> \
 --security-opt no-new-privileges=true \
 --cap-drop ALL \
 --cpus=2 \
 --memory=1g \
 ghcr.io/vampi62/electrostore/front:local
```

## login to the web interface
Open your web browser and navigate to `http://<your-server-ip>:8080`. You should see the ElectroStore web interface.
use the default credentials:
- **Username**: admin@localhost.local
- **Password**: Admin@1234

## Documentation
You can find the documentation for the Electrostore API at:

[installation stable version](/docs/01_installation.md)

[esp led and scanbox](/docs/02_storeLed_and_scanner.md)

[web interface](/docs/03_frontend_usage.md)

[api usage](/docs/04_api_usage.md)
