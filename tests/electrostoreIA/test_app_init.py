"""Tests for the app_init module."""

import pytest
from unittest.mock import patch, MagicMock, mock_open
import sys
import os
import json

# Add the parent directory to sys.path to import the electrostoreIA module
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../..')))

from electrostoreIA.app_init import (
    load_appsettings, initialize_database, initialize_s3_manager,
    initialize_application, connect_to_vault, get_secret_from_vault,
    process_vault_secrets
)


class TestAppInit:
    """Test application initialization functions."""
    
    @patch('electrostoreIA.app_init.os.path.exists')
    def test_load_appsettings_file_not_exists(self, mock_exists):
        """Test load_appsettings when config file doesn't exist."""
        # Arrange
        mock_exists.return_value = False
        
        # Act
        result = load_appsettings()
        
        # Assert
        assert result == {}
    
    @patch('builtins.open', new_callable=mock_open, read_data='{"test": "value"}')
    @patch('electrostoreIA.app_init.os.path.exists')
    def test_load_appsettings_success(self, mock_exists, mock_file):
        """Test successful load_appsettings without Vault."""
        # Arrange
        mock_exists.return_value = True
        
        # Act
        result = load_appsettings()
        
        # Assert
        assert result == {"test": "value"}
        mock_file.assert_called_once_with('/app/config/appsettings.json')
    
    @patch('electrostoreIA.app_init.db_query.MySQLConnection')
    def test_initialize_database_success(self, mock_mysql_connection):
        """Test successful database initialization."""
        # Arrange
        mock_session = MagicMock()
        mock_mysql_connection.return_value = mock_session
        
        appsettings = {
            "ConnectionStrings": {
                "DefaultConnection": "Server=localhost;Port=3306;Database=test;Uid=user;Pwd=pass;"
            }
        }
        
        # Act
        result = initialize_database(appsettings)
        
        # Assert
        assert result == mock_session
        mock_mysql_connection.assert_called_once_with({
            'Server': 'localhost',
            'Port': '3306', 
            'Database': 'test',
            'Uid': 'user',
            'Pwd': 'pass'
        })
        mock_session.connect.assert_called_once()
    
    def test_initialize_database_no_connection_string(self):
        """Test database initialization with no connection string."""
        # Arrange
        appsettings = {}
        
        # Act
        result = initialize_database(appsettings)
        
        # Assert
        assert result is None
    
    @patch('electrostoreIA.app_init.S3Manager')
    def test_initialize_s3_manager_enabled(self, mock_s3_manager_class):
        """Test S3 manager initialization when enabled."""
        # Arrange
        mock_s3_manager = MagicMock()
        mock_s3_manager_class.return_value = mock_s3_manager
        
        appsettings = {
            "S3": {
                "Enable": "true",
                "Endpoint": "localhost:9000",
                "AccessKey": "test",
                "SecretKey": "test",
                "BucketName": "test",
                "Secure": "false"
            }
        }
        
        # Act
        result = initialize_s3_manager(appsettings)
        
        # Assert
        assert result == mock_s3_manager
        mock_s3_manager_class.assert_called_once_with(appsettings["S3"])
    
    @patch('electrostoreIA.app_init.S3Manager')
    def test_initialize_s3_manager_disabled(self, mock_s3_manager_class):
        """Test S3 manager initialization when disabled."""
        # Arrange
        mock_s3_manager = MagicMock()
        mock_s3_manager_class.return_value = mock_s3_manager
        
        appsettings = {
            "S3": {
                "Enable": "false"
            }
        }
        
        # Act
        result = initialize_s3_manager(appsettings)
        
        # Assert
        assert result == mock_s3_manager
        mock_s3_manager_class.assert_called_once_with()
    
    @patch('electrostoreIA.app_init.S3Manager')
    def test_initialize_s3_manager_no_config(self, mock_s3_manager_class):
        """Test S3 manager initialization with no S3 config."""
        # Arrange
        mock_s3_manager = MagicMock()
        mock_s3_manager_class.return_value = mock_s3_manager
        
        appsettings = {}
        
        # Act
        result = initialize_s3_manager(appsettings)
        
        # Assert
        assert result == mock_s3_manager
        mock_s3_manager_class.assert_called_once_with()
    
    @patch('electrostoreIA.app_init.initialize_s3_manager')
    @patch('electrostoreIA.app_init.initialize_database')
    @patch('electrostoreIA.app_init.load_appsettings')
    def test_initialize_application_success(self, mock_load_settings, mock_init_db, mock_init_s3):
        """Test successful application initialization."""
        # Arrange
        mock_appsettings = {"test": "value"}
        mock_mysql_session = MagicMock()
        mock_s3_manager = MagicMock()
        
        mock_load_settings.return_value = mock_appsettings
        mock_init_db.return_value = mock_mysql_session
        mock_init_s3.return_value = mock_s3_manager
        
        # Act
        appsettings, mysql_session, s3_manager = initialize_application()
        
        # Assert
        assert appsettings == mock_appsettings
        assert mysql_session == mock_mysql_session
        assert s3_manager == mock_s3_manager
        mock_load_settings.assert_called_once()
        mock_init_db.assert_called_once_with(mock_appsettings)
        mock_init_s3.assert_called_once_with(mock_appsettings)

    @patch('electrostoreIA.app_init.initialize_s3_manager')
    @patch('electrostoreIA.app_init.initialize_database')
    @patch('electrostoreIA.app_init.load_appsettings')
    def test_initialize_application_with_error(self, mock_load_settings, mock_init_db, mock_init_s3):
        """Test application initialization with error in non-testing mode."""
        # Arrange
        mock_appsettings = {"test": "value"}
        mock_s3_manager = MagicMock()
        
        mock_load_settings.return_value = mock_appsettings
        mock_init_db.side_effect = Exception("Database error")
        mock_init_s3.return_value = mock_s3_manager
        
        # Mock the globals check for non-testing mode
        with patch('builtins.globals', return_value={}):
            # Act & Assert
            with pytest.raises(ConnectionError, match="Could not initialize application: Database error"):
                initialize_application()


class TestVaultIntegration:
    """Test Vault integration functions."""
    
    @patch('electrostoreIA.app_init.hvac.Client')
    def test_connect_to_vault_success(self, mock_hvac_client):
        """Test successful connection to Vault."""
        # Arrange
        mock_client = MagicMock()
        mock_client.is_authenticated.return_value = True
        mock_hvac_client.return_value = mock_client
        
        vault_config = {
            "Addr": "http://vault:8200",
            "Token": "test-token",
            "Path": "test-namespace"
        }
        
        # Act
        result = connect_to_vault(vault_config)
        
        # Assert
        assert result == mock_client
        mock_hvac_client.assert_called_once_with(
            url="http://vault:8200",
            token="test-token",
            namespace="test-namespace"
        )
        mock_client.is_authenticated.assert_called_once()
    
    @patch('electrostoreIA.app_init.hvac.Client')
    def test_connect_to_vault_not_authenticated(self, mock_hvac_client):
        """Test connection to Vault with failed authentication."""
        # Arrange
        mock_client = MagicMock()
        mock_client.is_authenticated.return_value = False
        mock_hvac_client.return_value = mock_client
        
        vault_config = {
            "Addr": "http://vault:8200",
            "Token": "bad-token"
        }
        
        # Act & Assert
        with pytest.raises(ConnectionError, match="Failed to authenticate with Vault"):
            connect_to_vault(vault_config)
    
    @patch('electrostoreIA.app_init.hvac.Client')
    def test_connect_to_vault_connection_error(self, mock_hvac_client):
        """Test connection to Vault with connection error."""
        # Arrange
        mock_hvac_client.side_effect = Exception("Connection failed")
        
        vault_config = {
            "Addr": "http://vault:8200",
            "Token": "test-token"
        }
        
        # Act & Assert
        with pytest.raises(ConnectionError, match="Failed to connect to Vault: Connection failed"):
            connect_to_vault(vault_config)
    
    def test_get_secret_from_vault_kv_v2_success(self):
        """Test retrieving secret from Vault KV v2."""
        # Arrange
        mock_vault_client = MagicMock()
        mock_vault_client.secrets.kv.v2.read_secret_version.return_value = {
            'data': {
                'data': {
                    'username': 'test-user',
                    'password': 'test-pass'
                }
            }
        }
        
        # Act
        result = get_secret_from_vault(mock_vault_client, "database/credentials", "secret")
        
        # Assert
        assert result == {'username': 'test-user', 'password': 'test-pass'}
        mock_vault_client.secrets.kv.v2.read_secret_version.assert_called_once_with(
            path="database/credentials",
            mount_point="secret"
        )
    
    def test_get_secret_from_vault_kv_v1_fallback(self):
        """Test retrieving secret from Vault KV v1 (fallback)."""
        # Arrange
        mock_vault_client = MagicMock()
        mock_vault_client.secrets.kv.v2.read_secret_version.side_effect = Exception("KV v2 not available")
        mock_vault_client.secrets.kv.v1.read_secret.return_value = {
            'data': {
                'username': 'test-user',
                'password': 'test-pass'
            }
        }
        
        # Act
        result = get_secret_from_vault(mock_vault_client, "database/credentials", "secret")
        
        # Assert
        assert result == {'username': 'test-user', 'password': 'test-pass'}
        mock_vault_client.secrets.kv.v1.read_secret.assert_called_once_with(
            path="database/credentials",
            mount_point="secret"
        )
    
    def test_get_secret_from_vault_both_versions_fail(self):
        """Test retrieving secret when both KV v1 and v2 fail."""
        # Arrange
        mock_vault_client = MagicMock()
        mock_vault_client.secrets.kv.v2.read_secret_version.side_effect = Exception("KV v2 not available")
        mock_vault_client.secrets.kv.v1.read_secret.side_effect = Exception("KV v1 failed")
        
        # Act & Assert
        with pytest.raises(ValueError, match="Failed to retrieve secret 'database/credentials'"):
            get_secret_from_vault(mock_vault_client, "database/credentials", "secret")
    
    @patch('electrostoreIA.app_init.get_secret_from_vault')
    def test_process_vault_secrets_simple_placeholder(self, mock_get_secret):
        """Test processing simple vault placeholder."""
        # Arrange
        mock_vault_client = MagicMock()
        vault_config = {"MountPoint": "secret"}
        
        mock_get_secret.return_value = {'value': 'secret-value'}
        
        config = {
            "ApiKey": "{{vault:api/key}}"
        }
        
        # Act
        result = process_vault_secrets(config, mock_vault_client, vault_config)
        
        # Assert
        assert result["ApiKey"] == "secret-value"
        mock_get_secret.assert_called_once_with(mock_vault_client, "api/key", "secret")
    
    @patch('electrostoreIA.app_init.get_secret_from_vault')
    def test_process_vault_secrets_with_field(self, mock_get_secret):
        """Test processing vault placeholder with specific field."""
        # Arrange
        mock_vault_client = MagicMock()
        vault_config = {"MountPoint": "secret"}
        
        mock_get_secret.return_value = {
            'username': 'test-user',
            'password': 'test-pass'
        }
        
        config = {
            "Database": {
                "Username": "{{vault:database/credentials:username}}",
                "Password": "{{vault:database/credentials:password}}"
            }
        }
        
        # Act
        result = process_vault_secrets(config, mock_vault_client, vault_config)
        
        # Assert
        assert result["Database"]["Username"] == "test-user"
        assert result["Database"]["Password"] == "test-pass"
        assert mock_get_secret.call_count == 2
    
    @patch('electrostoreIA.app_init.get_secret_from_vault')
    def test_process_vault_secrets_in_connection_string(self, mock_get_secret):
        """Test processing vault placeholders in connection string."""
        # Arrange
        mock_vault_client = MagicMock()
        vault_config = {"MountPoint": "secret"}
        
        def get_secret_side_effect(client, path, mount):
            if path == "database/credentials":
                return {'username': 'dbuser', 'password': 'dbpass'}
            return {}
        
        mock_get_secret.side_effect = get_secret_side_effect
        
        config = {
            "ConnectionStrings": {
                "DefaultConnection": "Server=localhost;Uid={{vault:database/credentials:username}};Pwd={{vault:database/credentials:password}};"
            }
        }
        
        # Act
        result = process_vault_secrets(config, mock_vault_client, vault_config)
        
        # Assert
        assert result["ConnectionStrings"]["DefaultConnection"] == "Server=localhost;Uid=dbuser;Pwd=dbpass;"
    
    @patch('electrostoreIA.app_init.get_secret_from_vault')
    def test_process_vault_secrets_nested_dict(self, mock_get_secret):
        """Test processing vault placeholders in nested dictionaries."""
        # Arrange
        mock_vault_client = MagicMock()
        vault_config = {"MountPoint": "secret"}
        
        mock_get_secret.return_value = {'value': 'secret-key'}
        
        config = {
            "Level1": {
                "Level2": {
                    "Level3": {
                        "ApiKey": "{{vault:api/key}}"
                    }
                }
            }
        }
        
        # Act
        result = process_vault_secrets(config, mock_vault_client, vault_config)
        
        # Assert
        assert result["Level1"]["Level2"]["Level3"]["ApiKey"] == "secret-key"
    
    @patch('electrostoreIA.app_init.get_secret_from_vault')
    def test_process_vault_secrets_list(self, mock_get_secret):
        """Test processing vault placeholders in lists."""
        # Arrange
        mock_vault_client = MagicMock()
        vault_config = {"MountPoint": "secret"}
        
        mock_get_secret.return_value = {'value': 'secret-token'}
        
        config = {
            "Tokens": ["{{vault:token1}}", "{{vault:token2}}", "plain-token"]
        }
        
        # Act
        result = process_vault_secrets(config, mock_vault_client, vault_config)
        
        # Assert
        assert result["Tokens"] == ["secret-token", "secret-token", "plain-token"]
    
    @patch('electrostoreIA.app_init.get_secret_from_vault')
    def test_process_vault_secrets_error_handling(self, mock_get_secret):
        """Test error handling when secret retrieval fails."""
        # Arrange
        mock_vault_client = MagicMock()
        vault_config = {"MountPoint": "secret"}
        
        mock_get_secret.side_effect = ValueError("Secret not found")
        
        config = {
            "ApiKey": "{{vault:api/key}}"
        }
        
        # Act
        result = process_vault_secrets(config, mock_vault_client, vault_config)
        
        # Assert - original placeholder should be retained
        assert result["ApiKey"] == "{{vault:api/key}}"
    
    @patch('electrostoreIA.app_init.get_secret_from_vault')
    def test_process_vault_secrets_non_string_values(self, mock_get_secret):
        """Test processing config with non-string values."""
        # Arrange
        mock_vault_client = MagicMock()
        vault_config = {"MountPoint": "secret"}
        
        config = {
            "Port": 8080,
            "Enabled": True,
            "Ratio": 3.14,
            "Items": None
        }
        
        # Act
        result = process_vault_secrets(config, mock_vault_client, vault_config)
        
        # Assert - non-string values should remain unchanged
        assert result["Port"] == 8080
        assert result["Enabled"] is True
        assert result["Ratio"] == 3.14
        assert result["Items"] is None
        mock_get_secret.assert_not_called()
    
    @patch('electrostoreIA.app_init.get_secret_from_vault')
    def test_process_vault_secrets_custom_mount_point(self, mock_get_secret):
        """Test processing vault placeholders with custom mount point."""
        # Arrange
        mock_vault_client = MagicMock()
        vault_config = {"MountPoint": "custom-kv"}
        
        mock_get_secret.return_value = {'value': 'secret-value'}
        
        config = {
            "ApiKey": "{{vault:api/key}}"
        }
        
        # Act
        result = process_vault_secrets(config, mock_vault_client, vault_config)
        
        # Assert
        assert result["ApiKey"] == "secret-value"
        mock_get_secret.assert_called_once_with(mock_vault_client, "api/key", "custom-kv")
