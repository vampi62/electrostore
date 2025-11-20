FROM node:lts-alpine AS build

WORKDIR /app

COPY package*.json ./

RUN npm install --ignore-scripts

COPY src/ ./src/
COPY public/ ./public/
COPY [ ".env", ".eslintrc.cjs", "index.html", "postcss.config.js", "tailwind.config.js", "vite.config.js", "./" ]

RUN npm run build

FROM nginx:alpine

COPY --from=build /app/dist /usr/share/nginx/html

COPY nginx.conf /etc/nginx/nginx.conf

COPY entrypoint.sh /entrypoint.sh

RUN chmod +x /entrypoint.sh && \
    chown -R 3216:3216 /usr/share/nginx/html && \
    chown -R 3216:3216 /var/cache/nginx && \
    chown -R 3216:3216 /var/log/nginx && \
    chown -R 3216:3216 /etc/nginx/conf.d && \
    touch /var/run/nginx.pid && \
    chown 3216:3216 /var/run/nginx.pid

USER 3216:3216

EXPOSE 80

ENTRYPOINT ["/entrypoint.sh"]

CMD ["nginx", "-g", "daemon off;"]