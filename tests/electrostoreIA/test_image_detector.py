"""Tests for the image_detector module."""

import pytest
from unittest.mock import patch, MagicMock, mock_open
import sys
import os
import numpy as np
import io

# Add the parent directory to sys.path to import the electrostoreIA module
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../..')))

from electrostoreIA.image_detector import predict_image, detect_model
from electrostoreIA.config import IMG_WIDTH, IMG_HEIGHT


class TestImageDetector:
    """Test image detector functions."""

    
    @patch('electrostoreIA.image_detector.get_class_names_path')
    @patch('electrostoreIA.image_detector.get_model_path')
    @patch('electrostoreIA.image_detector.tf')
    @patch('electrostoreIA.image_detector.predict_image')
    def test_detect_model_success(self, mock_predict, mock_tf, mock_model_path, mock_class_path):
        """Test successful model detection."""
        # Arrange
        mock_s3_manager = MagicMock()
        mock_image_data = MagicMock()
        mock_image_data.read.return_value = b'fake_image_data'
        
        mock_model_path.return_value = '/path/to/model'
        mock_class_path.return_value = '/path/to/classes.txt'
        
        mock_model = MagicMock()
        mock_tf.keras.models.load_model.return_value = mock_model
        
        mock_img = MagicMock()
        mock_img_array = np.array([[[[1, 2, 3]]]])
        mock_tf.keras.preprocessing.image.load_img.return_value = mock_img
        mock_tf.keras.utils.img_to_array.return_value = mock_img_array
        mock_tf.expand_dims.return_value = mock_img_array
        
        mock_score = np.array([0.1, 0.8, 0.1])
        mock_predict.return_value = mock_score
        
        mock_file_content = "0\n1\n2"
        with patch('builtins.open', mock_open(read_data=mock_file_content)):
            # Act
            result = detect_model(1, mock_image_data, mock_s3_manager)
        
        # Assert
        assert result['predicted_class'] == 1  # Index of max score (0.8)
        assert abs(result['confidence'] - 80.0) < 0.1  # 80% confidence
        mock_model_path.assert_called_once_with(1, mock_s3_manager)
        mock_class_path.assert_called_once_with(1, mock_s3_manager)
    
    @patch('electrostoreIA.image_detector.get_model_path')
    @patch('electrostoreIA.image_detector.tf')
    def test_detect_model_not_found(self, mock_tf, mock_model_path):
        """Test detection when model is not found."""
        # Arrange
        mock_s3_manager = MagicMock()
        mock_image_data = MagicMock()
        
        mock_model_path.return_value = '/path/to/nonexistent/model'
        mock_tf.keras.models.load_model.side_effect = FileNotFoundError("Model not found")
        
        # Act & Assert
        with pytest.raises(FileNotFoundError, match="Model 1 not found or not trained yet"):
            detect_model(1, mock_image_data, mock_s3_manager)
    
    @patch('electrostoreIA.image_detector.get_class_names_path')
    @patch('electrostoreIA.image_detector.get_model_path')
    @patch('electrostoreIA.image_detector.tf')
    def test_detect_model_class_names_not_found(self, mock_tf, mock_model_path, mock_class_path):
        """Test detection when class names file is not found."""
        # Arrange
        mock_s3_manager = MagicMock()
        mock_image_data = MagicMock()
        
        mock_model_path.return_value = '/path/to/model'
        mock_class_path.return_value = '/path/to/nonexistent/classes.txt'
        
        mock_model = MagicMock()
        mock_tf.keras.models.load_model.return_value = mock_model
        
        with patch('builtins.open', side_effect=FileNotFoundError("File not found")):
            # Act & Assert
            with pytest.raises(FileNotFoundError, match="Class names file for model 1 not found"):
                detect_model(1, mock_image_data, mock_s3_manager)
    
    @patch('electrostoreIA.image_detector.get_class_names_path')
    @patch('electrostoreIA.image_detector.get_model_path')
    @patch('electrostoreIA.image_detector.tf')
    def test_detect_model_image_processing_error(self, mock_tf, mock_model_path, mock_class_path):
        """Test detection when image processing fails."""
        # Arrange
        mock_s3_manager = MagicMock()
        mock_image_data = MagicMock()
        mock_image_data.read.side_effect = Exception("Image read error")
        
        mock_model_path.return_value = '/path/to/model'
        mock_class_path.return_value = '/path/to/classes.txt'
        
        mock_model = MagicMock()
        mock_tf.keras.models.load_model.return_value = mock_model
        
        mock_file_content = "0\n1\n2"
        with patch('builtins.open', mock_open(read_data=mock_file_content)):
            # Act & Assert
            with pytest.raises(ValueError, match="Error processing image"):
                detect_model(1, mock_image_data, mock_s3_manager)
    
    @patch('electrostoreIA.image_detector.get_class_names_path')
    @patch('electrostoreIA.image_detector.get_model_path')
    @patch('electrostoreIA.image_detector.tf')
    @patch('electrostoreIA.image_detector.predict_image')
    def test_detect_model_with_numeric_classes(self, mock_predict, mock_tf, mock_model_path, mock_class_path):
        """Test detection with numeric class names."""
        # Arrange
        mock_s3_manager = MagicMock()
        mock_image_data = MagicMock()
        mock_image_data.read.return_value = b'fake_image_data'
        
        mock_model_path.return_value = '/path/to/model'
        mock_class_path.return_value = '/path/to/classes.txt'
        
        mock_model = MagicMock()
        mock_tf.keras.models.load_model.return_value = mock_model
        
        mock_img = MagicMock()
        mock_img_array = np.array([[[[1, 2, 3]]]])
        mock_tf.keras.preprocessing.image.load_img.return_value = mock_img
        mock_tf.keras.utils.img_to_array.return_value = mock_img_array
        mock_tf.expand_dims.return_value = mock_img_array
        
        # Class 2 has highest confidence
        mock_score = np.array([0.05, 0.1, 0.85])
        mock_predict.return_value = mock_score
        
        mock_file_content = "0\n1\n2"
        with patch('builtins.open', mock_open(read_data=mock_file_content)):
            # Act
            result = detect_model(1, mock_image_data, mock_s3_manager)
        
        # Assert
        assert result['predicted_class'] == 2  # Converted "2" to int
        assert abs(result['confidence'] - 85.0) < 0.1  # 85% confidence
    
    @patch('electrostoreIA.image_detector.get_class_names_path')
    @patch('electrostoreIA.image_detector.get_model_path')
    @patch('electrostoreIA.image_detector.tf')
    @patch('electrostoreIA.image_detector.predict_image')
    def test_detect_model_without_s3_manager(self, mock_predict, mock_tf, mock_model_path, mock_class_path):
        """Test detection without S3 manager (local files only)."""
        # Arrange
        mock_image_data = MagicMock()
        mock_image_data.read.return_value = b'fake_image_data'
        
        mock_model_path.return_value = '/local/path/to/model'
        mock_class_path.return_value = '/local/path/to/classes.txt'
        
        mock_model = MagicMock()
        mock_tf.keras.models.load_model.return_value = mock_model
        
        mock_img = MagicMock()
        mock_img_array = np.array([[[[1, 2, 3]]]])
        mock_tf.keras.preprocessing.image.load_img.return_value = mock_img
        mock_tf.keras.utils.img_to_array.return_value = mock_img_array
        mock_tf.expand_dims.return_value = mock_img_array
        
        mock_score = np.array([0.3, 0.7])
        mock_predict.return_value = mock_score
        
        mock_file_content = "0\n1"
        with patch('builtins.open', mock_open(read_data=mock_file_content)):
            # Act
            result = detect_model(1, mock_image_data, s3_manager=None)
        
        # Assert
        assert result['predicted_class'] == 1
        assert abs(result['confidence'] - 70.0) < 0.1
        mock_model_path.assert_called_once_with(1, None)
        mock_class_path.assert_called_once_with(1, None)