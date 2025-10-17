"""Tests for the S3Manager module."""

import pytest
from unittest.mock import patch, MagicMock
import sys
import os

# Add the parent directory to sys.path to import the electrostoreIA module
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../..')))

from electrostoreIA.S3Manager import S3Manager


class TestS3Manager:
    """Test S3Manager functionality."""
    
    def test_init_disabled(self):
        """Test S3Manager initialization when disabled."""
        # Test with no config
        manager = S3Manager()
        assert not manager.is_enabled()
        assert manager.client is None
        assert manager.bucket_name is None
        
        # Test with disabled config
        config = {"Enable": "false"}
        manager = S3Manager(config)
        assert not manager.is_enabled()
    
    @patch('electrostoreIA.S3Manager.Minio')
    def test_init_enabled_success(self, mock_minio):
        """Test successful S3Manager initialization when enabled."""
        # Arrange
        mock_client = MagicMock()
        mock_minio.return_value = mock_client
        
        config = {
            "Enable": "true",
            "ServiceUrl": "http://localhost:9000",
            "AccessKey": "test_access",
            "SecretKey": "test_secret",
            "BucketName": "test_bucket"
        }
        
        # Act
        manager = S3Manager(config)
        
        # Assert
        assert manager.is_enabled()
        assert manager.bucket_name == "test_bucket"
        mock_minio.assert_called_once_with(
            endpoint="localhost:9000",
            access_key="test_access",
            secret_key="test_secret",
            secure=False
        )
    
    @patch('electrostoreIA.S3Manager.Minio')
    def test_init_enabled_https(self, mock_minio):
        """Test S3Manager initialization with HTTPS."""
        # Arrange
        mock_client = MagicMock()
        mock_minio.return_value = mock_client
        
        config = {
            "Enable": "true",
            "ServiceUrl": "https://s3.amazonaws.com",
            "AccessKey": "test_access",
            "SecretKey": "test_secret",
            "BucketName": "test_bucket"
        }
        
        # Act
        manager = S3Manager(config)
        
        # Assert
        assert manager.is_enabled()
        mock_minio.assert_called_once_with(
            endpoint="s3.amazonaws.com",
            access_key="test_access",
            secret_key="test_secret",
            secure=True
        )
    
    @patch('electrostoreIA.S3Manager.Minio')
    def test_init_enabled_failure(self, mock_minio):
        """Test S3Manager initialization failure."""
        # Arrange
        mock_minio.side_effect = Exception("Connection failed")
        
        config = {
            "Enable": "true",
            "ServiceUrl": "http://localhost:9000",
            "AccessKey": "test_access",
            "SecretKey": "test_secret",
            "BucketName": "test_bucket"
        }
        
        # Act
        manager = S3Manager(config)
        
        # Assert
        assert not manager.is_enabled()
        assert manager.client is None
        assert manager.bucket_name is None
    
    def test_download_file_disabled(self):
        """Test download_file when S3 is disabled."""
        manager = S3Manager()
        
        with pytest.raises(ValueError, match="S3 is not enabled or configured"):
            manager.download_file("test_key", "/tmp/test_file")
    
    @patch('electrostoreIA.S3Manager.os.makedirs')
    def test_download_file_success(self, mock_makedirs):
        """Test successful file download."""
        # Arrange
        mock_client = MagicMock()
        manager = S3Manager()
        manager.client = mock_client
        manager.bucket_name = "test_bucket"
        manager.enabled = True
        
        # Act
        result = manager.download_file("test_key", "/tmp/test_file")
        
        # Assert
        assert result is True
        mock_makedirs.assert_called_once()
        mock_client.fget_object.assert_called_once_with("test_bucket", "test_key", "/tmp/test_file")
    
    def test_upload_file_disabled(self):
        """Test upload_file when S3 is disabled."""
        manager = S3Manager()
        
        with pytest.raises(ValueError, match="S3 is not enabled or configured"):
            manager.upload_file("/tmp/test_file", "test_key")
    
    def test_upload_file_success(self):
        """Test successful file upload."""
        # Arrange
        mock_client = MagicMock()
        manager = S3Manager()
        manager.client = mock_client
        manager.bucket_name = "test_bucket"
        manager.enabled = True
        
        # Act
        result = manager.upload_file("/tmp/test_file", "test_key")
        
        # Assert
        assert result is True
        mock_client.fput_object.assert_called_once_with("test_bucket", "test_key", "/tmp/test_file")
    
    def test_list_objects_disabled(self):
        """Test list_objects when S3 is disabled."""
        manager = S3Manager()
        result = manager.list_objects("test_prefix")
        assert result == []
    
    def test_list_objects_success(self):
        """Test successful object listing."""
        # Arrange
        mock_client = MagicMock()
        mock_obj1 = MagicMock()
        mock_obj1.object_name = "test_prefix/file1.txt"
        mock_obj2 = MagicMock()
        mock_obj2.object_name = "test_prefix/file2.txt"
        mock_client.list_objects.return_value = [mock_obj1, mock_obj2]
        
        manager = S3Manager()
        manager.client = mock_client
        manager.bucket_name = "test_bucket"
        manager.enabled = True
        
        # Act
        result = manager.list_objects("test_prefix")
        
        # Assert
        assert result == ["test_prefix/file1.txt", "test_prefix/file2.txt"]
        mock_client.list_objects.assert_called_once_with("test_bucket", prefix="test_prefix", recursive=True)
    
    def test_test_connection_disabled(self):
        """Test test_connection when S3 is disabled."""
        manager = S3Manager()
        result = manager.test_connection()
        assert result is False
    
    def test_test_connection_success(self):
        """Test successful connection test."""
        # Arrange
        mock_client = MagicMock()
        mock_client.list_objects.return_value = []
        
        manager = S3Manager()
        manager.client = mock_client
        manager.bucket_name = "test_bucket"
        manager.enabled = True
        
        # Act
        result = manager.test_connection()
        
        # Assert
        assert result is True
        mock_client.list_objects.assert_called_once_with("test_bucket", max_keys=1)
    
    def test_test_connection_failure(self):
        """Test connection test failure."""
        # Arrange
        mock_client = MagicMock()
        mock_client.list_objects.side_effect = Exception("Connection failed")
        
        manager = S3Manager()
        manager.client = mock_client
        manager.bucket_name = "test_bucket"
        manager.enabled = True
        
        # Act
        result = manager.test_connection()
        
        # Assert
        assert result is False