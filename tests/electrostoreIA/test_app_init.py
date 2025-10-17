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
    initialize_application
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
        """Test successful load_appsettings."""
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
                "ServiceUrl": "http://localhost:9000",
                "AccessKey": "test",
                "SecretKey": "test",
                "BucketName": "test"
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
