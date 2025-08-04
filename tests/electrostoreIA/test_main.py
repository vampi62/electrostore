import pytest
import json
import io
from unittest.mock import patch, MagicMock
import sys
import os

# Add the parent directory to sys.path to import the electrostoreIA module
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../..')))

from electrostoreIA.main import train, status, detect

@pytest.fixture
def client():
    from electrostoreIA.main import app
    app.config['TESTING'] = True
    
    # Set up training_progress variable for tests
    from electrostoreIA.main import training_progress
    training_progress.clear()  # Clear any existing data
    training_progress[1] = {
        'status': 'completed',
        'message': 'Training completed successfully.',
        'epoch': 5,
        'accuracy': 0.85,
        'val_accuracy': 0.82,
        'loss': 0.25,
        'val_loss': 0.35
    }
    
    with app.test_client() as client:
        yield client

class TestFlaskAPI:
    @patch('electrostoreIA.main.async_train_model')
    @patch('electrostoreIA.main.mysql_session')
    def test_train_endpoint(self, mock_mysql_session, mock_async_train, client):
        # Arrange
        mock_async_train.return_value = None
        mock_mysql_session.get_ia.return_value = {"id": 1, "name": "Test Model"}
        mock_mysql_session.change_train_status.return_value = True
        test_data = {'id_model': 1}
        
        # Act
        response = client.post('/train/' + str(test_data['id_model']))
        response_data = json.loads(response.data)
        
        # Assert
        assert response.status_code == 200
        assert "message" in response_data
        assert "Training for model 1 started" in response_data["message"]
        mock_async_train.assert_called_once_with(1)
        mock_mysql_session.get_ia.assert_called_once_with(1)
        mock_mysql_session.change_train_status.assert_called_once_with(1, False)

    def test_status_endpoint(self, client):
        # Arrange
        test_data = {'id_model': 1}
        
        # Act
        response = client.get('/status/' + str(test_data['id_model']))
        response_data = json.loads(response.data)
        
        # Assert
        assert response.status_code == 200
        assert response_data['status'] == 'completed'
        assert response_data['message'] == 'Training completed successfully.'
        assert response_data['epoch'] == 5
        assert response_data['accuracy'] == 0.85

    @patch('electrostoreIA.main.detect_model')
    def test_detect_endpoint(self, mock_detect_model, client):
        # Arrange
        mock_detect_model.return_value = {'predicted_class': 1, 'confidence': 95.0}
        test_data = {'id_model': 1}
        
        # Create a mock file
        mock_file = io.BytesIO(b'test image data')
        
        # Act
        response = client.post(
            '/detect/' + str(test_data['id_model']),
            data={'img_file': (mock_file, 'test.jpg')}
        )
        response_data = json.loads(response.data)
        
        # Assert
        assert response.status_code == 200
        assert response_data['predicted_class'] == 1
        assert response_data['confidence'] == 95.0
        mock_detect_model.assert_called_once()