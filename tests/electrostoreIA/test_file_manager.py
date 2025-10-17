"""Tests for the file_manager module."""

import pytest
from unittest.mock import patch, MagicMock
import sys
import os

# Add the parent directory to sys.path to import the electrostoreIA module
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../..')))

from electrostoreIA.file_manager import (
    get_model_path, get_class_names_path, get_images_directory,
    _download_images_from_s3, _get_local_relative_paths,
    _delete_obsolete_files, _clean_empty_directories
)


class TestFileManager:
    """Test file manager functions."""
    
    def test_get_model_path_no_s3(self):
        """Test get_model_path without S3."""
        # Act
        result = get_model_path(1, None)
        
        # Assert
        expected = "/data/models/Model1.keras"
        assert result == expected
    
    @patch('electrostoreIA.file_manager.os.makedirs')
    @patch('electrostoreIA.file_manager.os.path.exists')
    def test_get_model_path_with_s3_exists_locally(self, mock_exists, mock_makedirs):
        """Test get_model_path with S3 when file exists locally."""
        # Arrange
        mock_exists.return_value = True
        mock_s3_manager = MagicMock()
        mock_s3_manager.is_enabled.return_value = True
        
        # Act
        result = get_model_path(1, mock_s3_manager)
        
        # Assert
        expected = "/data/models/Model1.keras"
        assert result == expected
        mock_makedirs.assert_called_once()
        mock_s3_manager.download_file.assert_not_called()
    
    @patch('electrostoreIA.file_manager.os.makedirs')
    @patch('electrostoreIA.file_manager.os.path.exists')
    def test_get_model_path_with_s3_download_success(self, mock_exists, mock_makedirs):
        """Test get_model_path with S3 download success."""
        # Arrange
        mock_exists.return_value = False
        mock_s3_manager = MagicMock()
        mock_s3_manager.is_enabled.return_value = True
        mock_s3_manager.download_file.return_value = True
        
        # Act
        result = get_model_path(1, mock_s3_manager)
        
        # Assert
        expected = "/data/models/Model1.keras"
        assert result == expected
        mock_s3_manager.download_file.assert_called_once_with(
            'models/Model1.keras', expected
        )
    
    @patch('electrostoreIA.file_manager.os.makedirs')
    @patch('electrostoreIA.file_manager.os.path.exists')
    def test_get_model_path_with_s3_download_failure(self, mock_exists, mock_makedirs):
        """Test get_model_path with S3 download failure."""
        # Arrange
        mock_exists.return_value = False
        mock_s3_manager = MagicMock()
        mock_s3_manager.is_enabled.return_value = True
        mock_s3_manager.download_file.return_value = False
        
        # Act & Assert
        with pytest.raises(FileNotFoundError, match="Model 1 not found in S3 or local storage"):
            get_model_path(1, mock_s3_manager)
    
    def test_get_class_names_path_no_s3(self):
        """Test get_class_names_path without S3."""
        # Act
        result = get_class_names_path(1, None)
        
        # Assert
        expected = "/data/models/ItemList1.txt"
        assert result == expected
    
    @patch('electrostoreIA.file_manager.os.walk')
    def test_get_local_relative_paths(self, mock_walk):
        """Test _get_local_relative_paths function."""
        # Arrange
        mock_walk.return_value = [
            ('/data/images', ['class1'], []),
            ('/data/images/class1', [], ['image1.jpg', 'image2.jpg'])
        ]
        
        # Act
        result = _get_local_relative_paths('/data/images')
        
        # Assert
        expected = {'class1/image1.jpg', 'class1/image2.jpg'}
        assert result == expected
    
    def test_download_images_from_s3(self):
        """Test _download_images_from_s3 function."""
        # Arrange
        mock_s3_manager = MagicMock()
        mock_s3_manager.download_file.return_value = True
        
        image_objects = [
            'images/class1/image1.jpg',
            'images/class1/image2.jpg',
            'images/class2/image3.jpg'
        ]
        
        with patch('electrostoreIA.file_manager.os.makedirs'), \
             patch('electrostoreIA.file_manager.os.path.exists', return_value=False):
            
            # Act
            result = _download_images_from_s3('/data/images', image_objects, mock_s3_manager)
            
            # Assert
            expected = {'class1/image1.jpg', 'class1/image2.jpg', 'class2/image3.jpg'}
            assert result == expected
            assert mock_s3_manager.download_file.call_count == 3
    
    @patch('electrostoreIA.file_manager.os.remove')
    def test_delete_obsolete_files(self, mock_remove):
        """Test _delete_obsolete_files function."""
        # Arrange
        files_to_delete = {'class1/old_image.jpg', 'class2/old_image.jpg'}
        
        # Act
        _delete_obsolete_files('/data/images', files_to_delete)
        
        # Assert
        assert mock_remove.call_count == 2
    
    @patch('electrostoreIA.file_manager.os.rmdir')
    @patch('electrostoreIA.file_manager.os.listdir')
    @patch('electrostoreIA.file_manager.os.walk')
    def test_clean_empty_directories(self, mock_walk, mock_listdir, mock_rmdir):
        """Test _clean_empty_directories function."""
        # Arrange
        mock_walk.return_value = [
            ('/data/images', ['empty_class'], []),
            ('/data/images/empty_class', [], [])
        ]
        mock_listdir.return_value = []  # Empty directory
        
        # Act
        _clean_empty_directories('/data/images')
        
        # Assert
        mock_rmdir.assert_called()
    
    @patch('electrostoreIA.file_manager._clean_empty_directories')
    @patch('electrostoreIA.file_manager._delete_obsolete_files')
    @patch('electrostoreIA.file_manager._get_local_relative_paths')
    @patch('electrostoreIA.file_manager._download_images_from_s3')
    @patch('electrostoreIA.file_manager.os.makedirs')
    def test_get_images_directory_with_s3(self, mock_makedirs, mock_download, 
                                        mock_local_paths, mock_delete, mock_clean):
        """Test get_images_directory with S3 enabled."""
        # Arrange
        mock_s3_manager = MagicMock()
        mock_s3_manager.is_enabled.return_value = True
        mock_s3_manager.list_objects.return_value = ['images/class1/image1.jpg']
        
        mock_download.return_value = {'class1/image1.jpg'}
        mock_local_paths.return_value = {'class1/old_image.jpg', 'class1/image1.jpg'}
        
        # Act
        result = get_images_directory(mock_s3_manager)
        
        # Assert
        assert result == '/data/images/'
        mock_makedirs.assert_called_once_with('/data/images/', exist_ok=True)
        mock_download.assert_called_once()
        mock_local_paths.assert_called_once()
        mock_delete.assert_called_once_with('/data/images/', {'class1/old_image.jpg'})
        mock_clean.assert_called_once()
    
    def test_get_images_directory_no_s3(self):
        """Test get_images_directory without S3."""
        # Act
        result = get_images_directory(None)
        
        # Assert
        assert result == '/data/images/'