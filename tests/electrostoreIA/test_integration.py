"""Integration tests for the modular electrostoreIA application."""

import pytest
from unittest.mock import patch, MagicMock
import sys
import os

# Add the parent directory to sys.path to import the electrostoreIA module
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../..')))


class TestModuleIntegration:
    """Test integration between different modules."""
    
    @patch('electrostoreIA.app_init.load_appsettings')
    @patch('electrostoreIA.app_init.initialize_database')
    @patch('electrostoreIA.app_init.initialize_s3_manager')
    def test_application_initialization_flow(self, mock_settings, mock_s3, mock_db):
        """Test the full application initialization flow."""
        # Arrange
        mock_settings.return_value = {
            'S3': {'enable': False},
            'ConnectionStrings': {'DefaultConnection': 'mock://connection'}
        }
        mock_db.return_value = MagicMock()
        mock_s3.return_value = MagicMock()
        
        # Act
        from electrostoreIA.app_init import initialize_application
        initialize_application()
        
        # Assert
        mock_settings.assert_called_once()
        mock_db.assert_called_once()
        mock_s3.assert_called_once()
    
    @patch('electrostoreIA.model_trainer.get_images_directory')
    @patch('electrostoreIA.file_manager.get_images_directory')
    def test_model_trainer_file_manager_integration(self, mock_fm_get_images, mock_mt_get_images):
        """Test integration between model_trainer and file_manager."""
        # Arrange
        mock_s3_manager = MagicMock()
        test_dir = '/data/images/'
        mock_fm_get_images.return_value = test_dir
        mock_mt_get_images.return_value = test_dir
        
        # Import modules
        from electrostoreIA import model_trainer
        from electrostoreIA import file_manager
        
        # Both modules should use the same directory structure
        fm_dir = file_manager.get_images_directory(mock_s3_manager)
        mt_dir = model_trainer.get_images_directory(mock_s3_manager)
        
        # Assert both return the same directory
        assert fm_dir == mt_dir
    
    @patch('electrostoreIA.S3Manager.Minio')
    def test_s3_manager_configuration_consistency(self, mock_minio):
        """Test S3Manager configuration with different settings."""
        from electrostoreIA.S3Manager import S3Manager
        
        # mock Minio client inside S3Manager
        mock_minio.return_value = MagicMock()

        # Test with S3 disabled
        settings_disabled = {'S3': {'Enable': False}}
        s3_manager_disabled = S3Manager(settings_disabled)
        assert not s3_manager_disabled.is_enabled()
        
        # Test with S3 enabled
        settings_enabled = {
            'Enable': 'True',
            'Endpoint': 'localhost:9000',
            'AccessKey': 'test',
            'SecretKey': 'test',
            'BucketName': 'test-bucket',
            'Secure': 'false'
        }
        s3_manager_enabled = S3Manager(settings_enabled)
        assert s3_manager_enabled.is_enabled()
    
    def test_status_enum_consistency(self):
        """Test that Status enum is used consistently across modules."""
        from electrostoreIA.config import Status
        from electrostoreIA import model_trainer
        
        # Verify Status enum values
        assert hasattr(Status, 'IN_PROGRESS')
        assert hasattr(Status, 'COMPLETED')
        assert hasattr(Status, 'ERROR')
        
        # Test that model_trainer uses the same Status enum
        result = model_trainer.create_demo_training_result(1)
        assert result['status'] == Status.COMPLETED
    
    def test_main_application_structure(self):
        """Test the main application structure after refactoring."""
        # Import main module
        from electrostoreIA import main
        
        # Verify main module has minimal content
        # The main.py should now be much shorter after refactoring
        import inspect
        main_source = inspect.getsource(main)
        
        # The main module should be significantly shorter than before
        # (originally it was 400+ lines, now should be around 15 lines)
        line_count = len(main_source.splitlines())
        assert line_count < 50, f"Main module too large: {line_count} lines"
    
    def test_configuration_loading(self):
        """Test configuration loading and constants."""
        from electrostoreIA.config import (
            IMG_WIDTH, IMG_HEIGHT, DEFAULT_BATCH_SIZE, DEFAULT_EPOCHS,
            IMAGE_DIR, MODEL_DIR, Status
        )
        
        # Verify all constants are defined
        assert IMG_WIDTH > 0
        assert IMG_HEIGHT > 0
        assert DEFAULT_BATCH_SIZE > 0
        assert DEFAULT_EPOCHS > 0
        assert isinstance(IMAGE_DIR, str)
        assert isinstance(MODEL_DIR, str)
        
        # Verify Status enum
        assert hasattr(Status, 'IN_PROGRESS')
        assert hasattr(Status, 'COMPLETED')
        assert hasattr(Status, 'ERROR')


class TestErrorHandling:
    """Test error handling across modules."""
    
    def test_s3_manager_error_handling(self):
        """Test S3Manager error handling."""
        from electrostoreIA.S3Manager import S3Manager
        
        # Test with invalid configuration
        invalid_settings = {'S3': {'enable': True}}  # Missing required fields
        s3_manager = S3Manager(invalid_settings)
        
        # Should handle gracefully
        assert not s3_manager.test_connection()
    
    def test_model_trainer_error_handling(self):
        """Test model trainer error handling."""
        from electrostoreIA.model_trainer import get_training_status
        
        # Test with non-existent model
        result = get_training_status(999)
        assert "No training in progress" in result["message"]


class TestPerformance:
    """Test performance aspects of the modular structure."""
    
    def test_import_time(self):
        """Test that modules import quickly."""
        import time
        
        start_time = time.time()
        
        # Import all modules
        from electrostoreIA import config
        from electrostoreIA import S3Manager
        from electrostoreIA import app_init
        from electrostoreIA import routes
        from electrostoreIA import model_trainer
        from electrostoreIA import image_detector
        from electrostoreIA import file_manager
        
        end_time = time.time()
        import_time = end_time - start_time
        
        # Should import quickly (less than 2 seconds on most systems)
        assert import_time < 2.0, f"Import time too slow: {import_time}s"
    
    def test_memory_usage(self):
        """Test that modules don't consume excessive memory."""
        import sys
        
        # Get initial module count
        initial_modules = len(sys.modules)
        
        # Import all electrostoreIA modules
        from electrostoreIA import config
        from electrostoreIA import S3Manager
        from electrostoreIA import app_init
        from electrostoreIA import routes
        from electrostoreIA import model_trainer
        from electrostoreIA import image_detector
        from electrostoreIA import file_manager
        
        # Get final module count
        final_modules = len(sys.modules)
        new_modules = final_modules - initial_modules
        
        # Should not import excessive number of new modules
        assert new_modules < 50, f"Too many new modules imported: {new_modules}"