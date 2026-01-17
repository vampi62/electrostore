"""Database and configuration initialization utilities."""

import json
import os
import re
import hvac
import db_query
from S3Manager import S3Manager


def connect_to_vault(vault_config):
    """Connect to HashiCorp Vault and return client."""
    try:
        client = hvac.Client(
            url=vault_config.get("Addr", "http://vault:8200"),
            token=vault_config.get("Token", None),
            namespace=vault_config.get("Path", None)
        )
        
        # Verify connection
        if not client.is_authenticated():
            raise ConnectionError("Failed to authenticate with Vault")
        
        return client
    except Exception as e:
        raise ConnectionError(f"Failed to connect to Vault: {str(e)}")


def get_secret_from_vault(vault_client, secret_path, mount_point="secret"):
    """Retrieve a secret from Vault."""
    try:
        # For KV v2, use read_secret_version
        secret_response = vault_client.secrets.kv.v2.read_secret_version(
            path=secret_path,
            mount_point=mount_point
        )
        return secret_response['data']['data']
    except Exception as e:
        # Fallback to KV v1 if v2 fails
        try:
            secret_response = vault_client.secrets.kv.v1.read_secret(
                path=secret_path,
                mount_point=mount_point
            )
            return secret_response['data']
        except Exception as e2:
            raise ValueError(f"Failed to retrieve secret '{secret_path}': {str(e2)}")


def process_vault_secrets(config, vault_client, vault_config):
    """Recursively process configuration and replace vault placeholders with actual secrets."""
    vault_pattern = re.compile(r'\{{vault:([^}]+)\}}')
    mount_point = vault_config.get("MountPoint", "secret")
    
    def replace_vault_placeholder(value):
        """Replace vault placeholder with actual secret value."""
        if not isinstance(value, str):
            return value
        
        match = vault_pattern.match(value)
        if match:
            secret_key = match.group(1)
            # Parse secret key: can be "path" or "path:field"
            if ':' in secret_key:
                secret_path, field = secret_key.rsplit(':', 1)
            else:
                secret_path = secret_key
                field = 'value'  # Default field name
            
            try:
                secret_data = get_secret_from_vault(vault_client, secret_path, mount_point)
                return secret_data.get(field, value)
            except Exception as e:
                print(f"Warning: Could not retrieve secret '{secret_key}': {str(e)}")
                return value
        
        return value
    
    def process_dict(d):
        """Recursively process dictionary."""
        for key, value in d.items():
            if isinstance(value, dict):
                process_dict(value)
            elif isinstance(value, list):
                d[key] = [replace_vault_placeholder(item) if isinstance(item, str) else item for item in value]
            elif isinstance(value, str):
                d[key] = replace_vault_placeholder(value)
    
    process_dict(config)
    return config


def load_appsettings(config_path='/app/config/appsettings.json'):
    """Load application settings from JSON file."""
    appsettings = {}
    
    if os.path.exists(config_path):
        with open(config_path) as f:
            appsettings = json.load(f)
    
    # Check if Vault is enabled and process secrets
    if appsettings.get("Vault", {}).get("Enable") == True or \
       str(appsettings.get("Vault", {}).get("Enable", "")).lower() == "true":
        try:
            vault_config = appsettings.get("Vault", {})
            vault_client = connect_to_vault(vault_config)
            appsettings = process_vault_secrets(appsettings, vault_client, vault_config)
            print("Successfully retrieved secrets from Vault")
        except Exception as e:
            print(f"Error processing Vault secrets: {str(e)}")
            raise
    
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
        raise ConnectionError(f"Could not initialize application: {str(e)}")
    
    return appsettings, mysql_session, s3_manager
