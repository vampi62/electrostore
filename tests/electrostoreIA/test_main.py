"""Tests for the main Flask application and routes."""

import pytest
import json
import io
from unittest.mock import patch, MagicMock
import sys
import os

# Add the parent directory to sys.path to import the electrostoreIA module
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../..')))

@pytest.fixture
def app():
    """Create and configure a test Flask app."""
    from flask import Flask
    from electrostoreIA.routes import register_routes
    from electrostoreIA.model_trainer import training_progress
    
    # Create test app
    test_app = Flask(__name__)
    test_app.config['TESTING'] = True
    
    # Create mock dependencies
    mock_appsettings = {'DemoMode': False}
    mock_mysql_session = MagicMock()
    mock_mysql_session.get_ia.return_value = {"id": 1, "name": "Test Model"}
    mock_mysql_session.change_train_status.return_value = True
    mock_mysql_session.is_connected.return_value = True
    
    mock_s3_manager = MagicMock()
    mock_s3_manager.is_enabled.return_value = True
    mock_s3_manager.test_connection.return_value = True
    
    # Register routes with mocked dependencies
    register_routes(test_app, mock_appsettings, mock_s3_manager, mock_mysql_session)
    
    # Set up training_progress for tests
    training_progress.clear()
    training_progress[10] = {
        'status': 'completed',
        'message': 'Training completed successfully.',
        'epoch': 5,
        'accuracy': 0.85,
        'val_accuracy': 0.82,
        'loss': 0.25,
        'val_loss': 0.35
    }
    
    return test_app

@pytest.fixture
def client(app):
    """Create a test client."""
    return app.test_client()

class TestFlaskAPI:
    """Test Flask API endpoints."""

    @patch('electrostoreIA.model_trainer.async_train_model')
    @patch('electrostoreIA.model_trainer.is_training_in_progress')
    def test_train_endpoint_success(self, mock_is_training, mock_async_train, client):
        """Test successful training start."""
        # Arrange
        mock_is_training.return_value = False
        mock_async_train.return_value = None
        
        # Act
        response = client.post('/train/1')
        response_data = json.loads(response.data)
        
        # Assert
        assert response.status_code == 200
        assert "message" in response_data
        assert "Training for model 1 started" in response_data["message"]

    def test_status_endpoint_nonexistent_model(self, client):
        """Test status endpoint for non-existent model."""
        # Act
        response = client.get('/status/999')
        response_data = json.loads(response.data)
        
        # Assert
        assert response.status_code == 404
        assert "No training in progress" in response_data["message"]

    def test_detect_endpoint_no_file(self, client):
        """Test detection endpoint without file."""
        # Act
        response = client.post('/detect/1')
        response_data = json.loads(response.data)
        
        # Assert
        assert response.status_code == 400
        assert "No image file provided" in response_data["error"]

    def test_health_endpoint(self, client):
        """Test health check endpoint."""
        # Act
        response = client.get('/health')
        response_data = json.loads(response.data)
        
        # Assert
        assert response.status_code == 200
        assert response_data['status'] == 'healthy'
        assert 'training_in_progress' in response_data
        assert 'db_connected' in response_data
        assert 's3_status' in response_data
        assert response_data['db_connected'] is True