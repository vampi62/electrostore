"""Configuration constants and enums for the ElectrostoreIA application."""

# Chemins pour stocker les modèles et les classes
MODEL_DIR = '/data/models/'
IMAGE_DIR = '/data/images/'

# Dimensions des images pour l'entraînement
IMG_HEIGHT = 180
IMG_WIDTH = 180

# Configuration par défaut pour l'entraînement
DEFAULT_BATCH_SIZE = 32
DEFAULT_EPOCHS = 10
DEFAULT_VALIDATION_SPLIT = 0.2
DEFAULT_SEED = 123


class Status:
    """Enum for training status."""
    IN_PROGRESS = 'in progress'
    COMPLETED = 'completed'
    ERROR = 'error'