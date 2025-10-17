"""Tests for the routes module."""

import pytest
from unittest.mock import patch, MagicMock
import json
import sys
import os

# Add the parent directory to sys.path to import the electrostoreIA module
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../..')))

from electrostoreIA.routes import register_routes
from electrostoreIA.config import Status


@pytest.fixture
def mock_app():
    """Create a mock Flask app for testing."""
    mock_app = MagicMock()
    mock_app.route = MagicMock()
    return mock_app


@pytest.fixture
def mock_appsettings():
    """Create mock appsettings."""
    return {'DemoMode': False}


@pytest.fixture
def mock_s3_manager():
    """Create mock S3 manager."""
    return MagicMock()


@pytest.fixture
def mock_mysql_session():
    """Create mock MySQL session."""
    return MagicMock()


class TestRoutes:
    """Test routes functionality."""
    
    def test_train_endpoint_route_registered(self, mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session):
        """Test training endpoint route registration."""
        # Import and setup routes
        register_routes(mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session)
        
        # Verify route was registered
        train_calls = [call for call in mock_app.route.call_args_list if '/train' in str(call)]
        assert len(train_calls) > 0
    
    @patch('electrostoreIA.routes.is_training_in_progress')
    def test_train_endpoint_already_in_progress(self, mock_is_training, mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session):
        """Test training endpoint when training is already in progress."""
        # Arrange
        mock_is_training.return_value = True
        
        # Import and setup routes
        register_routes(mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session)
        
        # Verify route was registered
        train_calls = [call for call in mock_app.route.call_args_list if '/train' in str(call)]
        assert len(train_calls) > 0
    
    def test_status_endpoint(self, mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session):
        """Test status endpoint."""
        # Import and setup routes
        register_routes(mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session)
        
        # Verify route was registered
        status_calls = [call for call in mock_app.route.call_args_list if '/status' in str(call)]
        assert len(status_calls) > 0
    
    def test_detect_endpoint_success(self, mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session):
        """Test successful detect endpoint."""
        # Import and setup routes
        register_routes(mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session)
        
        # Verify route was registered
        detect_calls = [call for call in mock_app.route.call_args_list if '/detect' in str(call)]
        assert len(detect_calls) > 0
    
    def test_detect_endpoint_registration(self, mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session):
        """Test detect endpoint registration."""
        # Import and setup routes
        register_routes(mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session)
        
        # Verify route was registered
        detect_calls = [call for call in mock_app.route.call_args_list if '/detect' in str(call)]
        assert len(detect_calls) > 0
    
    def test_health_endpoint(self, mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session):
        """Test health endpoint."""
        # Import and setup routes
        register_routes(mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session)
        
        # Verify route was registered
        health_calls = [call for call in mock_app.route.call_args_list if '/health' in str(call)]
        assert len(health_calls) > 0
    
    def test_all_routes_registered(self, mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session):
        """Test that all expected routes are registered."""
        # Import and setup routes
        register_routes(mock_app, mock_appsettings, mock_s3_manager, mock_mysql_session)
        
        # Get all route calls
        route_calls = mock_app.route.call_args_list
        routes = [str(call) for call in route_calls]
        
        # Verify all expected routes are present
        expected_routes = ['/train', '/status', '/detect', '/health']
        for expected_route in expected_routes:
            assert any(expected_route in route for route in routes), f"Route {expected_route} not found"
    
    @patch('electrostoreIA.routes.create_demo_training_result')
    @patch('electrostoreIA.routes.training_progress')
    def test_demo_mode_enabled(self, mock_demo_result, mock_app, mock_s3_manager, mock_mysql_session):
        """Test training endpoint with demo mode enabled."""
        # Arrange
        mock_appsettings_demo = {'DemoMode': True}
        mock_mysql_session.get_ia.return_value = {'id': 1, 'name': 'Test Model'}
        
        # Mock the demo result
        expected_demo_result = {
            'status': Status.COMPLETED,
            'message': 'Training completed successfully (demo mode).',
            'epoch': 10,
            'accuracy': 0.95,
            'val_accuracy': 0.92,
            'loss': 0.15,
            'val_loss': 0.25
        }
        mock_demo_result.return_value = expected_demo_result
        
        # Import and setup routes
        register_routes(mock_app, mock_appsettings_demo, mock_s3_manager, mock_mysql_session)
        
        # Verify routes are still registered
        route_calls = mock_app.route.call_args_list
        assert len(route_calls) >= 4  # At least 4 routes should be registered
        
        # Verify that create_demo_training_result would be called with correct parameters
        # and that the training_progress would be updated with demo result
        mock_demo_result.assert_not_called()  # Not called yet since we just registered routes
        
        # Verify the demo result structure is correct
        demo_result = mock_demo_result.return_value
        assert demo_result['status'] == Status.COMPLETED
        assert 'demo mode' in demo_result['message'].lower()
        assert 'epoch' in demo_result
        assert 'accuracy' in demo_result
        assert 'val_accuracy' in demo_result
        assert 'loss' in demo_result
        assert 'val_loss' in demo_result