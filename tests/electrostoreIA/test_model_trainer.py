"""Tests for the model_trainer module."""

import pytest
from unittest.mock import patch, MagicMock
import sys
import os

# Add the parent directory to sys.path to import the electrostoreIA module
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../..')))

from electrostoreIA.model_trainer import (
    TrainingCallback, create_model, get_training_status,
    is_training_in_progress, create_demo_training_result,
    training_progress
)
from electrostoreIA.config import Status


class TestModelTrainer:
    """Test model trainer functions."""
    
    def setup_method(self):
        """Setup for each test method."""
        training_progress.clear()
    
    def test_training_callback_init(self):
        """Test TrainingCallback initialization."""
        # Act
        callback = TrainingCallback(1)
        
        # Assert
        assert callback.id_model == 1
    
    def test_get_training_status_existing_model(self):
        """Test get_training_status for existing model."""
        # Arrange
        training_progress[1] = {
            'status': Status.COMPLETED,
            'message': 'Training completed successfully.'
        }
        
        # Act
        result = get_training_status(1)
        
        # Assert
        assert result['status'] == Status.COMPLETED
        assert result['message'] == 'Training completed successfully.'
    
    def test_get_training_status_nonexistent_model(self):
        """Test get_training_status for non-existent model."""
        # Act
        result = get_training_status(999)
        
        # Assert
        assert "No training in progress for this model" in result["message"]
    
    def test_is_training_in_progress_true(self):
        """Test is_training_in_progress when training is active."""
        # Arrange
        training_progress[1] = {'status': Status.IN_PROGRESS}
        training_progress[2] = {'status': Status.COMPLETED}
        
        # Act
        result = is_training_in_progress()
        
        # Assert
        assert result is True
    
    def test_is_training_in_progress_false(self):
        """Test is_training_in_progress when no training is active."""
        # Arrange
        training_progress[1] = {'status': Status.COMPLETED}
        training_progress[2] = {'status': Status.ERROR}
        
        # Act
        result = is_training_in_progress()
        
        # Assert
        assert result is False
    
    def test_is_training_in_progress_empty(self):
        """Test is_training_in_progress with empty progress."""
        # Act
        result = is_training_in_progress()
        
        # Assert
        assert result is False
    
    def test_create_demo_training_result(self):
        """Test create_demo_training_result function."""
        # Act
        result = create_demo_training_result(1)
        
        # Assert
        assert result['status'] == Status.COMPLETED
        assert result['message'] == 'Training completed successfully (demo mode).'
        assert result['epoch'] == 10
        assert abs(result['accuracy'] - 0.95) < 0.001
        assert abs(result['val_accuracy'] - 0.92) < 0.001
        assert abs(result['loss'] - 0.15) < 0.001
        assert abs(result['val_loss'] - 0.25) < 0.001
    
    @patch('electrostoreIA.model_trainer.threading.Thread')
    @patch('electrostoreIA.model_trainer.train_model')
    def test_async_train_model(self, mock_train_model, mock_thread):
        """Test async_train_model function."""
        # Arrange
        mock_thread_instance = MagicMock()
        mock_thread.return_value = mock_thread_instance
        mock_s3_manager = MagicMock()
        mock_mysql_session = MagicMock()
        
        # Import the function to test
        from electrostoreIA.model_trainer import async_train_model
        
        # Act
        async_train_model(1, mock_s3_manager, mock_mysql_session)
        
        # Assert
        mock_thread.assert_called_once_with(
            target=mock_train_model, 
            args=(1, mock_s3_manager, mock_mysql_session)
        )
        mock_thread_instance.start.assert_called_once()
    