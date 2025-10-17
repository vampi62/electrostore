"""Tests for the config module."""

import pytest
import sys
import os

# Add the parent directory to sys.path to import the electrostoreIA module
sys.path.insert(0, os.path.abspath(os.path.join(os.path.dirname(__file__), '../..')))

from electrostoreIA.config import (
    Status, MODEL_DIR, IMAGE_DIR, IMG_HEIGHT, IMG_WIDTH,
    DEFAULT_BATCH_SIZE, DEFAULT_EPOCHS, DEFAULT_VALIDATION_SPLIT, DEFAULT_SEED
)


class TestConfig:
    """Test configuration constants and enums."""
    
    def test_status_enum_values(self):
        """Test Status enum has correct values."""
        assert Status.IN_PROGRESS == 'in progress'
        assert Status.COMPLETED == 'completed'
        assert Status.ERROR == 'error'
    
    def test_directory_paths(self):
        """Test directory path constants."""
        assert MODEL_DIR == '/data/models/'
        assert IMAGE_DIR == '/data/images/'
        assert MODEL_DIR.endswith('/')
        assert IMAGE_DIR.endswith('/')
    
    def test_image_dimensions(self):
        """Test image dimension constants."""
        assert IMG_HEIGHT == 180
        assert IMG_WIDTH == 180
        assert isinstance(IMG_HEIGHT, int)
        assert isinstance(IMG_WIDTH, int)
        assert IMG_HEIGHT > 0
        assert IMG_WIDTH > 0
    
    def test_training_defaults(self):
        """Test default training parameters."""
        assert DEFAULT_BATCH_SIZE == 32
        assert DEFAULT_EPOCHS == 10
        assert abs(DEFAULT_VALIDATION_SPLIT - 0.2) < 1e-9
        assert DEFAULT_SEED == 123
        
        # Test types
        assert isinstance(DEFAULT_BATCH_SIZE, int)
        assert isinstance(DEFAULT_EPOCHS, int)
        assert isinstance(DEFAULT_VALIDATION_SPLIT, float)
        assert isinstance(DEFAULT_SEED, int)
        
        # Test reasonable values
        assert DEFAULT_BATCH_SIZE > 0
        assert DEFAULT_EPOCHS > 0
        assert 0 < DEFAULT_VALIDATION_SPLIT < 1
        assert DEFAULT_SEED >= 0
    
    def test_status_enum_immutable(self):
        """Test that Status enum values are strings (immutable)."""
        assert isinstance(Status.IN_PROGRESS, str)
        assert isinstance(Status.COMPLETED, str)
        assert isinstance(Status.ERROR, str)