"""Main application entry point for ElectrostoreIA."""

from flask import Flask
from app_init import initialize_application
from routes import register_routes

# Initialize Flask app
app = Flask(__name__)

# Initialize application components
appsettings, mysql_session, s3_manager = initialize_application()

# Register all routes
register_routes(app, appsettings, s3_manager, mysql_session)

if __name__ == '__main__':
	app.run(host='0.0.0.0', port=5000)
