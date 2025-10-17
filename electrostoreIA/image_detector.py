"""Image detection and prediction utilities."""

import io
import numpy as np
import tensorflow as tf
from PIL import Image

from electrostoreIA.config import IMG_HEIGHT, IMG_WIDTH
from file_manager import get_model_path, get_class_names_path


@tf.function(reduce_retracing=True)
def predict_image(model, img_array):
    """Predict image using the model."""
    predictions = model(img_array, training=False)
    score = tf.nn.softmax(predictions[0])
    return score


def detect_model(id_model, image_data, s3_manager=None):
    """Detect/classify an image using the specified model."""
    # Charger le modèle (télécharger depuis S3 si nécessaire)
    try:
        model = tf.keras.models.load_model(get_model_path(id_model, s3_manager))
    except FileNotFoundError as e:
        raise FileNotFoundError(f"Model {id_model} not found or not trained yet.")

    # Charger les noms des classes (télécharger depuis S3 si nécessaire)
    try:
        class_names = []
        with open(get_class_names_path(id_model, s3_manager), 'r') as f:
            for line in f:
                class_names.append(line.strip())
    except FileNotFoundError as e:
        raise FileNotFoundError(f"Class names file for model {id_model} not found.")

    try:
        img = tf.keras.preprocessing.image.load_img(
            io.BytesIO(image_data.read()), 
            target_size=(IMG_HEIGHT, IMG_WIDTH)
        )
        img_array = tf.keras.utils.img_to_array(img)
        img_array = tf.expand_dims(img_array, 0)  # Créer un batch

        score = predict_image(model, img_array)

        predicted_class = class_names[np.argmax(score)]
        confidence = float(100 * np.max(score))

        return {
            "predicted_class": int(predicted_class),
            "confidence": confidence
        }
    except Exception as e:
        raise ValueError(f"Error processing image: {str(e)}")