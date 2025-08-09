#!/bin/sh
# search and replace the placeholder with the actual API URL
for file in /usr/local/apache2/htdocs/assets/*.js; do
    sed -i "s|http://localhost/placeholder/api|"${VUE_API_URL}"|g" $file
done
exec "$@"