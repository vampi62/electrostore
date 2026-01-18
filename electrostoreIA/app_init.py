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
        if not client.is_authenticated():
            raise ConnectionError("Failed to authenticate with Vault")
        return client
    except Exception as e:
        raise ConnectionError(f"Failed to connect to Vault: {str(e)}")


def get_secret_from_vault(vault_client, secret_path, mount_point="secret"):
    """Retrieve a secret from Vault."""
    try:
        secret_response = vault_client.secrets.kv.v2.read_secret_version(
            path=secret_path,
            mount_point=mount_point
        )
        return secret_response['data']['data']
    except Exception as e:
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
    secret_path = vault_config.get("Path", "")
    def replace_vault_placeholder(value):
        """Replace vault placeholder with actual secret value."""
        if not isinstance(value, str):
            return value
        for match in vault_pattern.finditer(value):
            secret_key = match.group(1)
            secret_data = get_secret_from_vault(vault_client, secret_path, mount_point)
            if secret_key in secret_data:
                value = value.replace(f'{{{{vault:{secret_key}}}}}', str(secret_data[secret_key]))
            else:
                raise KeyError(f"Secret field '{secret_key}' not found in Vault path '{secret_path}'")
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
        mysql_session = initialize_database(appsettings)
        s3_manager = initialize_s3_manager(appsettings)
    except Exception as e:
        raise ConnectionError(f"Could not initialize application: {str(e)}")
    return appsettings, mysql_session, s3_manager
