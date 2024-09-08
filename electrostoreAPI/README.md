# electrostore

commande pour lancer le projet : 
```bash
# build dev
docker build -t electrostoreapi:dev -f Dockerfile-debug .

# run dev
docker run -d \
  --name electrostoreAPI \
  --restart always \
  --network dockernet \
  --label "traefik.enable=true" \
  --label "traefik.http.routers.electrostoreAPI.rule=Host(\`store.raspberrycloudav.fr\`)" \
  --label "traefik.http.routers.electrostoreAPI.entrypoints=websecure" \
  --label "traefik.http.routers.electrostoreAPI.tls.certresolver=myresolver" \
  --label "traefik.http.services.electrostoreAPI.loadbalancer.server.port=80" \
  electrostoreapi:dev



# build release
docker build -t electrostoreapi:release -f Dockerfile-release .
# run release
docker run -d \
--name electrostoreAPI \
--restart always \
--network dockernet \
-v electrostoreAPI:/app/wwwroot \
--label "traefik.enable=true" \
--label "traefik.http.routers.electrostoreAPI.rule=Host(\`store.raspberrycloudav.fr\`)" \
--label "traefik.http.routers.electrostoreAPI.entrypoints=websecure" \
--label "traefik.http.routers.electrostoreAPI.tls.certresolver=myresolver" \
--label "traefik.http.services.electrostoreAPI.loadbalancer.server.port=80" \
electrostoreapi:dev
```
