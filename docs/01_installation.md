
# Installation

## configuration page
A page to generate the configuration files and the setup script is available here: [generator page](https://vampi62.github.io/electrostore/docs/generator/index.html)

## start database server
if you have already a mariadb server and a mqtt server, you can skip this step, just make sure that the database is named `electrostore` with a user with the permissions to access it.
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
```

create MQTT configuration
```bash
sudo mkdir /opt/mqtt && sudo mkdir /opt/mqtt/config && cd /opt/mqtt/config

sudo nano mosquitto.conf

sudo nano mosquitto.passwd
```
Add the following content to the `mosquitto.conf` file, replacing `electrostore` with your desired username and password:
```plaintext
listener 1883
persistence true
persistence_location /mosquitto/data/
password_file /mosquitto/config/mosquitto.passwd
allow_anonymous false
```
Add the user to the password file: `mosquitto.passwd`
```plaintext
electrostore:$7$101$C/k6gESY+L+YDyuA$XCUrPtlix5a0mlq/f6JGklsaMbzL9wekwZ2udKjkpZNK0S8ix50vUbumpTBUqGacMd1HeCInrZstzhrw+Upe5g==
```
```bash
sudo docker run -d --name mqtt \
 --restart always \
 -p 1883:1883 \
 -v electrostoreMQTT:/mosquitto/data \
 -v /opt/mqtt/config:/mosquitto/config \
 -v /opt/mqtt/log:/mosquitto/log \
 eclipse-mosquitto:2.0.20
```
the user created in the `mosquitto.passwd` is `electrostore` with the password `QHF8Gmq3oa2L117FmLqC`
if you want to change the password, you can use the `mosquitto_passwd` command:
```bash
sudo docker attach mqtt
mosquitto_passwd -b /mosquitto/config/mosquitto.passwd electrostore <new-password>
```

## start Kafka
```bash
sudo docker run -d --name kafka \
 --restart always \
 -p 9092:9092 \
 -e KAFKA_CFG_NODE_ID=1 \
 -e KAFKA_CFG_PROCESS_ROLES=broker,controller \
 -e KAFKA_CFG_CONTROLLER_QUORUM_VOTERS=1@kafka:9093 \
 -e KAFKA_CFG_LISTENERS=PLAINTEXT://:9092,CONTROLLER://:9093 \
 -e KAFKA_CFG_ADVERTISED_LISTENERS=PLAINTEXT://kafka:9092 \
 -e KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP=CONTROLLER:PLAINTEXT,PLAINTEXT:PLAINTEXT \
 -e KAFKA_CFG_CONTROLLER_LISTENER_NAMES=CONTROLLER \
 -v electrostoreKAFKA:/bitnami/kafka \
 bitnami/kafka:3.9
```

## create network
```bash
sudo docker network create electrostore

sudo docker network connect electrostore mariadb

sudo docker network connect electrostore mqtt

sudo docker network connect electrostore kafka
```

## create and Complete config file
```bash
sudo mkdir -p /opt/electrostore/api /opt/electrostore/ia /opt/electrostore/notif /opt/electrostore/cron /opt/electrostore/worker
```

### API configuration
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

### IA service configuration
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

### Notification service configuration
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

### CRON service configuration
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

### WORKER service configuration
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

## start the API
```bash
sudo docker run -d --name electrostoreAPI \
 --restart always \
 --network electrostore \
 -p 5002:8080 \
 -v electrostoreDATA:/app/wwwroot \
 -v /opt/electrostore/api:/app/config:ro \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 --cap-drop ALL \
 --cap-add CHOWN \
 --cpus=2 \
 --memory=1g \
 ghcr.io/vampi62/electrostore/api:v1.0

sudo docker run -d --name electrostoreIA \
 --restart always \
 --network electrostore \
 -v electrostoreDATA:/data \
 -v /opt/electrostore/ia:/app/config:ro \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 --cap-drop ALL \
 --cap-add CHOWN \
 --cpus=2 \
 --memory=2g \
 ghcr.io/vampi62/electrostore/ia:v1.0

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
 ghcr.io/vampi62/electrostore/notif:v1.0

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
 ghcr.io/vampi62/electrostore/cron:v1.0

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
 ghcr.io/vampi62/electrostore/worker:v1.0
```

## start the web interface
set `VUE_API_URL` with the complete url of the API (ex: https://api.electrostore.com:443/api)
```bash
sudo docker run -d --name electrostoreFRONT \
 --restart always \
 -p 8080:80 \
 -e VUE_API_URL=http://<your-api-url>/api \
 --security-opt no-new-privileges=true \
 --cap-drop ALL \
 --cpus=2 \
 --memory=1g \
 ghcr.io/vampi62/electrostore/front:v1.0
```

## login to the web interface
Open your web browser and navigate to `http://<your-server-ip>:8080`. You should see the ElectroStore web interface.
use the default credentials:
- **Username**: admin@localhost.local
- **Password**: Admin@1234