#!/bin/sh
# search and replace the placeholder with the actual API URL
for file in /usr/share/nginx/html/assets/js/*.js; do
    sed -i "s|http://localhost/vite_api_url_placeholder/api|"${VUE_API_URL}"|g" $file
    sed -i "s|demo_mode_placeholder|"${APP_DEMO_MODE}"|g" $file
done
for file in /usr/share/nginx/html/assets/*.js; do
    sed -i "s|http://localhost/vite_api_url_placeholder/api|"${VUE_API_URL}"|g" $file
    sed -i "s|demo_mode_placeholder|"${APP_DEMO_MODE}"|g" $file
done
exec "$@"
