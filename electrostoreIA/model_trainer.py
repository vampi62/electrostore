"""Model training logic and utilities."""

import os
import pathlib
import threading
import tensorflow as tf
from tensorflow.keras import layers, models

from electrostoreIA.config import Status, MODEL_DIR, IMG_HEIGHT, IMG_WIDTH, DEFAULT_BATCH_SIZE, DEFAULT_EPOCHS
from file_manager import get_images_directory

# Dictionnaire partagé pour stocker l'avancement de l'entraînement
training_progress = {}


class TrainingCallback(tf.keras.callbacks.Callback):
    """Callback to track training progress."""
    
    def __init__(self, id_model):
        super().__init__()
        self.id_model = id_model

    def on_epoch_end(self, epoch, logs=None):
        """Met à jour le progrès après chaque époque."""
        if logs is not None:
            accuracy = logs.get('accuracy', 0)
            val_accuracy = logs.get('val_accuracy', 0)
            loss = logs.get('loss', 0)
            val_loss = logs.get('val_loss', 0)

            training_progress[self.id_model] = {
                'status': Status.IN_PROGRESS,
                'message': 'Training in progress',
                'epoch': epoch + 1,
                'accuracy': accuracy,
                'val_accuracy': val_accuracy,
                'loss': loss,
                'val_loss': val_loss
            }


def create_model(num_classes):
    """Create a simple CNN model."""
    model = models.Sequential([
        layers.Rescaling(1./255, input_shape=(IMG_HEIGHT, IMG_WIDTH, 3)),
        layers.Conv2D(16, 3, padding='same', activation='relu'),
        layers.MaxPooling2D(),
        layers.Conv2D(32, 3, padding='same', activation='relu'),
        layers.MaxPooling2D(),
        layers.Conv2D(64, 3, padding='same', activation='relu'),
        layers.MaxPooling2D(),
        layers.Flatten(),
        layers.Dense(128, activation='relu'),
        layers.Dense(num_classes)
    ])

    model.compile(optimizer='adam',
                loss=tf.keras.losses.SparseCategoricalCrossentropy(from_logits=True),
                metrics=['accuracy'])
    
    return model


def train_model(id_model, s3_manager=None, mysql_session=None):
    """Train a model with the given ID."""
    try:
        data_dir = pathlib.Path(get_images_directory(s3_manager))
        batch_size = DEFAULT_BATCH_SIZE

        train_ds = tf.keras.utils.image_dataset_from_directory(
            data_dir,
            validation_split=0.2,
            subset="training",
            seed=123,
            image_size=(IMG_HEIGHT, IMG_WIDTH),
            batch_size=batch_size)

        val_ds = tf.keras.utils.image_dataset_from_directory(
            data_dir,
            validation_split=0.2,
            subset="validation",
            seed=123,
            image_size=(IMG_HEIGHT, IMG_WIDTH),
            batch_size=batch_size)

        class_names = train_ds.class_names
        AUTOTUNE = tf.data.AUTOTUNE
        train_ds = train_ds.cache().shuffle(1000).prefetch(buffer_size=AUTOTUNE)
        val_ds = val_ds.cache().prefetch(buffer_size=AUTOTUNE)

        # Create and compile model
        model = create_model(len(class_names))
        
        epochs = DEFAULT_EPOCHS
        callback = TrainingCallback(id_model)

        # Lancement de l'entraînement avec callback pour suivre le progrès
        model.fit(train_ds, validation_data=val_ds, epochs=epochs, callbacks=[callback])

        # Sauvegarder le modèle localement
        model_path = os.path.join(MODEL_DIR, f'Model{id_model}.keras')
        os.makedirs(MODEL_DIR, exist_ok=True)
        model.save(model_path)

        # Sauvegarder les noms des classes localement
        class_names_path = os.path.join(MODEL_DIR, f'ItemList{id_model}.txt')
        with open(class_names_path, 'w') as f:
            for item in class_names:
                f.write("%s\n" % item)

        # Upload to S3 if enabled
        if s3_manager and s3_manager.is_enabled():
            try:
                # Upload model to S3
                model_s3_key = f'models/Model{id_model}.keras'
                if s3_manager.upload_file(model_path, model_s3_key):
                    print(f"Model {id_model} uploaded to S3")
                
                # Upload class names to S3
                class_names_s3_key = f'models/ItemList{id_model}.txt'
                if s3_manager.upload_file(class_names_path, class_names_s3_key):
                    print(f"Class names for model {id_model} uploaded to S3")
            except Exception as s3_error:
                print(f"Warning: Could not upload model {id_model} to S3: {str(s3_error)}")

        print(f"Model {id_model} trained and saved.")
        # Marquer la fin de l'entraînement dans le dictionnaire de suivi
        training_progress[id_model]['status'] = Status.COMPLETED
        training_progress[id_model]['message'] = 'Training completed successfully.'
        
        # set to true the trained_ia field in the database
        if mysql_session:
            mysql_session.change_train_status(id_model, True)
            
    except Exception as e:
        print(f"Error training model {id_model}: {str(e)}")
        # Marquer l'erreur dans le dictionnaire de suivi
        training_progress[id_model]['status'] = Status.ERROR
        training_progress[id_model]['message'] = str(e)


def async_train_model(id_model, s3_manager=None, mysql_session=None):
    """Lance l'entraînement dans un thread séparé."""
    thread = threading.Thread(target=train_model, args=(id_model, s3_manager, mysql_session))
    thread.start()


def get_training_status(id_model):
    """Get the training status for a model."""
    return training_progress.get(id_model, {"message": "No training in progress for this model."})


def is_training_in_progress():
    """Check if any training is currently in progress."""
    return any(model['status'] == Status.IN_PROGRESS for model in training_progress.values())


def create_demo_training_result(id_model):
    """Create a mock training result for demo mode."""
    return {
        'status': Status.COMPLETED,
        'message': 'Training completed successfully (demo mode).',
        'epoch': 10,
        'accuracy': 0.95,
        'val_accuracy': 0.92,
        'loss': 0.15,
        'val_loss': 0.25
    }