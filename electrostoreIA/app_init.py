"""Database and configuration initialization utilities."""

import json
import os
import db_query
from S3Manager import S3Manager


def load_appsettings(config_path='/app/config/appsettings.json'):
    """Load application settings from JSON file."""
    appsettings = {}
    
    if os.path.exists(config_path):
        with open(config_path) as f:
            appsettings = json.load(f)
    
    return appsettings


def initialize_database(appsettings):
    """Initialize database connection from settings."""
    mysql_session = None
    
    if ("ConnectionStrings" in appsettings) and ("DefaultConnection" in appsettings["ConnectionStrings"]):
        appsettings_string = appsettings["ConnectionStrings"]["DefaultConnection"]
        db_settings = {}
        
        for setting in appsettings_string.split(';'):
            if setting == '':
                continue
            key, value = setting.split('=')
            db_settings[key] = value

        mysql_session = db_query.MySQLConnection(db_settings)
        mysql_session.connect()
    
    return mysql_session


def initialize_s3_manager(appsettings):
    """Initialize S3 manager from settings."""
    if "S3" in appsettings and appsettings["S3"].get("Enable", "false").lower() == "true":
        return S3Manager(appsettings["S3"])
    else:
        return S3Manager()


def create_mock_database_session():
    """Create a mock database session for testing."""
    class MockMySQLConnection:
        def change_train_status(self, id_model, status):
            return True
        
        def get_ia(self, id_model):
            return {"id": id_model, "name": "Test Model"}
        
        def is_connected(self):
            return True
    
    return MockMySQLConnection()


def initialize_application():
    """Initialize the complete application configuration."""
    appsettings = load_appsettings()
    mysql_session = None
    s3_manager = None
    
    try:
        # Initialize database
        mysql_session = initialize_database(appsettings)
        
        # Initialize S3 manager
        s3_manager = initialize_s3_manager(appsettings)
        
    except Exception as e:
        print(f"Error initializing application: {str(e)}")
        
        # Check if in testing mode
        if 'app.config' in globals() and getattr(globals().get('app', {}), 'config', {}).get('TESTING', False):
            print(f"Warning: Could not initialize database connection: {str(e)}")
            mysql_session = create_mock_database_session()
        else:
            raise ConnectionError(f"Could not initialize database connection: {str(e)}")
    
    return appsettings, mysql_session, s3_manager