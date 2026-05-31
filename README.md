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
This section describes how to install the development version of Electrostore.  
For using a stable version follow the instructions in the [docs/01_installation.md](docs/01_installation.md) file,
and use the configuration generator page to create the configuration files and setup script:  
[generator page](https://vampi62.github.io/electrostore/docs/generator/index.html)

otherwise you can follow the instructions below to install the development version.

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

#### start Kafka
```bash
sudo docker run -d --name kafka \
 --restart always \
 -p 9092:9092 \
 -e KAFKA_NODE_ID=1 \
 -e KAFKA_PROCESS_ROLES=broker,controller \
 -e KAFKA_LISTENERS=PLAINTEXT://:9092,CONTROLLER://:9093 \
 -e KAFKA_ADVERTISED_LISTENERS=PLAINTEXT://kafka:9092 \
 -e KAFKA_CONTROLLER_LISTENER_NAMES=CONTROLLER \
 -e KAFKA_LISTENER_SECURITY_PROTOCOL_MAP=CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT \
 -e KAFKA_CONTROLLER_QUORUM_VOTERS=1@kafka:9093 \
 -e KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR=1 \
 -e KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR=1 \
 -e KAFKA_TRANSACTION_STATE_LOG_MIN_ISR=1 \
 -e KAFKA_GROUP_INITIAL_REBALANCE_DELAY_MS=0 \
 -e KAFKA_NUM_PARTITIONS=3 \
 -v electrostore-kafka-data:/var/lib/kafka/data \
 -v electrostore-kafka-config:/mnt/shared/config \
 -v electrostore-kafka-secrets:/etc/kafka/secrets \
 apache/kafka:4.2.1
```

#### create network
```bash
sudo docker network create electrostore

sudo docker network connect electrostore mariadb

sudo docker network connect electrostore mqtt

sudo docker network connect electrostore kafka
```

#### create and Complete config file
```bash
sudo mkdir -p /opt/electrostore/api /opt/electrostore/ia /opt/electrostore/notif /opt/electrostore/cron /opt/electrostore/worker
```

##### API configuration
```bash
sudo nano /opt/electrostore/api/appsettings.json
```
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Grpc": {
        "Url": "http://0.0.0.0:5001",
        "Protocols": "Http2"
      },
      "Http": {
        "Url": "http://0.0.0.0:5000",
        "Protocols": "Http1"
      }
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
      "GroupMapping": {
        "User": "electrostore Users",
        "Moderator": "electrostore Moderators",
        "Admin": "electrostore Admins"
      },
      "DisplayName": "<method-name>",
      "IconUrl": "<method-icon-url>"
    }
  },
  "S3": {
    "Enable": false,
    "Endpoint": "<minio-url>:9000",
    "AccessKey": "<access-key>",
    "SecretKey": "<secret-key>",
    "BucketName": "electrostore",
    "Region": "garage",
    "Secure": false
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092"
  },
  "Vault": {
    "Enable": false,
    "Address": "http://<vault-server-url>:8200",
    "Token": "<vault-access-token>",
    "Path": "electrostore",
    "MountPoint": "secret"
  },
  "IAServiceGrpcUrl": "http://electrostoreIA:5001",
  "IAServiceHealthUrl": "http://electrostoreIA:5000/health",
  "NotifServiceHealthUrl": "http://electrostoreNOTIF:5000/health",
  "CRONServiceHealthUrl": "http://electrostoreCRON:5000/health",
  "WORKERServiceHealthUrl": "http://electrostoreWORKER:5000/health",
  "AllowedOrigins": [
    "https://<your-frontend-domain1>",
    "https://<your-frontend-domain2>"
  ],
  "FrontendUrl": "http://<frontend-url>",
  "AllowedHosts": "*",
  "DemoMode": false
}
```

##### IA service configuration
```bash
sudo nano /opt/electrostore/ia/appsettings.json
```
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.ML": "Warning"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Grpc": {
        "Url": "http://0.0.0.0:5001",
        "Protocols": "Http2"
      },
      "Http": {
        "Url": "http://0.0.0.0:5000",
        "Protocols": "Http1"
      }
    }
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "ConsumerGroupId": "ia-service"
  },
  "ApiServiceGrpcUrl": "http://electrostoreAPI:5001",
  "Vault": {
    "Enable": false,
    "Addr": "http://vault:8200",
    "Token": "",
    "Path": "",
    "MountPoint": "secret"
  },
  "DefaultEpochs": 10,
  "DefaultBatchSize": 32
}
```

##### Notification service configuration
```bash
sudo nano /opt/electrostore/notif/appsettings.json
```
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000",
        "Protocols": "Http1"
      }
    }
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "ConsumerGroupId": "notif-service"
  },
  "ApiServiceGrpcUrl": "http://electrostoreAPI:5001",
  "Vault": {
    "Enable": false,
    "Addr": "http://vault:8200",
    "Token": "",
    "Path": "",
    "MountPoint": "secret"
  },
  "SMTP": {
    "Enable": false,
    "Host": "<your-smtp-host>",
    "Port": "587",
    "Username": "<your-smtp-username>",
    "Password": "<your-smtp-password>",
    "From": "noreply@electrostore.local"
  },
  "VAPID": {
    "Subject": "mailto:admin@electrostore.local",
    "PublicKey": "<your-vapid-public-key>",
    "PrivateKey": "<your-vapid-private-key>"
  }
}
```

> Generate VAPID keys with: `npx web-push generate-vapid-keys`  
> The **Public Key** must also be set as `VITE_VAPID_PUBLIC_KEY` in the frontend environment.

##### CRON service configuration
```bash
sudo nano /opt/electrostore/cron/appsettings.json
```
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Quartz": "Warning"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000",
        "Protocols": "Http1"
      }
    }
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "CronConsumerGroupId": "cron-service-events"
  },
  "Track17": {
    "ApiKey": "<your-17track-api-key>"
  },
  "ApiServiceGrpcUrl": "http://electrostoreAPI:5001",
  "CronRefreshIntervalMinutes": 60,
  "Vault": {
    "Enable": false,
    "Addr": "http://vault:8200",
    "Token": "",
    "Path": "",
    "MountPoint": "secret"
  }
}
```

##### WORKER service configuration
```bash
sudo nano /opt/electrostore/worker/appsettings.json
```
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000",
        "Protocols": "Http1"
      }
    }
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "ConsumerGroupId": "worker-service"
  },
  "Mqtt": {
    "Host": "mqtt",
    "Port": "1883",
    "Username": "electrostore",
    "Password": "QHF8Gmq3oa2L117FmLqC",
    "ClientId": "electrostore-worker"
  },
  "ApiServiceGrpcUrl": "http://electrostoreAPI:5001",
  "Vault": {
    "Enable": false,
    "Addr": "http://vault:8200",
    "Token": "",
    "Path": "",
    "MountPoint": "secret"
  }
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
 -v /opt/electrostore/api:/app/config:ro \
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
 -v /opt/electrostore/ia:/app/config:ro \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 --cap-drop ALL \
 --cap-add CHOWN \
 --cpus=2 \
 --memory=2g \
 ghcr.io/vampi62/electrostore/ia:local

sudo docker build -t ghcr.io/vampi62/electrostore/notif:local electrostoreNOTIF
sudo docker run -d --name electrostoreNOTIF \
 --restart always \
 --network electrostore \
 -v /opt/electrostore/notif:/app/config:ro \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 --cap-drop ALL \
 --cap-add CHOWN \
 --cpus=1 \
 --memory=512m \
 ghcr.io/vampi62/electrostore/notif:local

sudo docker build -t ghcr.io/vampi62/electrostore/cron:local electrostoreCRON
sudo docker run -d --name electrostoreCRON \
 --restart always \
 --network electrostore \
 -v /opt/electrostore/cron:/app/config:ro \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 --cap-drop ALL \
 --cap-add CHOWN \
 --cpus=1 \
 --memory=256m \
 ghcr.io/vampi62/electrostore/cron:local

sudo docker build -t ghcr.io/vampi62/electrostore/worker:local electrostoreWORKER
sudo docker run -d --name electrostoreWORKER \
 --restart always \
 --network electrostore \
 -v /opt/electrostore/worker:/app/config:ro \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 --cap-drop ALL \
 --cap-add CHOWN \
 --cpus=1 \
 --memory=256m \
 ghcr.io/vampi62/electrostore/worker:local
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
