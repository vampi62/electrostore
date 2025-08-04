FROM node:lts-alpine AS build

WORKDIR /app

COPY package*.json ./

RUN npm install

COPY . .

RUN npm run build

FROM httpd:alpine

COPY --from=build /app/dist /usr/local/apache2/htdocs/

COPY .htaccess /usr/local/apache2/htdocs/

COPY httpd.conf /usr/local/apache2/conf/httpd.conf

COPY entrypoint.sh /usr/local/apache2/

RUN chmod +x /usr/local/apache2/entrypoint.sh && \
    chown -R 3216:3216 /usr/local/apache2/htdocs && \
    chown -R 3216:3216 /usr/local/apache2/conf && \
    chown -R 3216:3216 /usr/local/apache2/entrypoint.sh

USER 3216:3216

ENTRYPOINT ["/usr/local/apache2/entrypoint.sh"]

CMD ["httpd-foreground"]
