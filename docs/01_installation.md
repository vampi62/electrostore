
# Installation

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
sudo mkdir /opt/mqtt && cd /opt/mqtt

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

## create network
```bash
sudo docker network create electrostore

sudo docker network connect electrostore mariadb

sudo docker network connect electrostore mqtt
```

## create and Complete config file
```bash
sudo mkdir /opt/electrostore && cd /opt/electrostore
sudo nano config.json
```
Complete the `config.json` file with the following content, replacing placeholders with your actual values:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=mariadb;Port=3306;Database=electrostore;Uid=electrostore;Pwd=password;"
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
  "FrontendUrl": "http://<frontend-url>",
  "AllowedHosts": "*"
}
```

## start the API
```bash
sudo docker run -d --name electrostoreAPI \
 --restart always \
 --network electrostore \
 -p 5002:80 \
 -v electrostoreDATA:/app/wwwroot \
 -v /opt/electrostore:/app/config:ro \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 --cap-add NET_RAW \
 --cap-drop ALL \
 ghcr.io/vampi62/electrostore/api:v1.0

sudo docker run -d --name electrostoreIA \
 --restart always \
 --network electrostore \
 -v electrostoreDATA:/data \
 -v /opt/electrostore:/app/config:ro \
 --tmpfs /tmp \
 --security-opt no-new-privileges=true \
 --read-only=true \
 --cap-drop ALL \
 --cpus=2 \
 ghcr.io/vampi62/electrostore/ia:v1.0
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
 ghcr.io/vampi62/electrostore/front:v1.0
```

## login to the web interface
Open your web browser and navigate to `http://<your-server-ip>:8080`. You should see the ElectroStore web interface.
use the default credentials:
- **Username**: admin@localhost.local
- **Password**: Admin@1234