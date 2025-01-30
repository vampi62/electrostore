from flask import Flask, request, jsonify
import numpy as np
import tensorflow as tf
from keras import layers, models
import pathlib
import os
import threading
from PIL import Image
import io

app = Flask(__name__)

# Chemins pour stocker les modèles et les classes
MODEL_DIR = '/data/models/'
IMAGE_DIR = '/data/images/'

# Dictionnaire partagé pour stocker l'avancement de l'entraînement
training_progress = {}

img_height = 180
img_width = 180

class TrainingCallback(tf.keras.callbacks.Callback):
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
                'status': 'in progress',
                'epoch': epoch + 1,
                'accuracy': accuracy,
                'val_accuracy': val_accuracy,
                'loss': loss,
                'val_loss': val_loss
            }

def train_model(id_model):
    try:
        data_dir = pathlib.Path(IMAGE_DIR)
        batch_size = 32

        train_ds = tf.keras.utils.image_dataset_from_directory(
            data_dir,
            validation_split=0.2,
            subset="training",
            seed=123,
            image_size=(img_height, img_width),
            batch_size=batch_size)

        val_ds = tf.keras.utils.image_dataset_from_directory(
            data_dir,
            validation_split=0.2,
            subset="validation",
            seed=123,
            image_size=(img_height, img_width),
            batch_size=batch_size)

        class_names = train_ds.class_names
        AUTOTUNE = tf.data.AUTOTUNE
        train_ds = train_ds.cache().shuffle(1000).prefetch(buffer_size=AUTOTUNE)
        val_ds = val_ds.cache().prefetch(buffer_size=AUTOTUNE)

        # Modèle simple CNN
        model = models.Sequential([
            layers.Rescaling(1./255, input_shape=(img_height, img_width, 3)),
            layers.Conv2D(16, 3, padding='same', activation='relu'),
            layers.MaxPooling2D(),
            layers.Conv2D(32, 3, padding='same', activation='relu'),
            layers.MaxPooling2D(),
            layers.Conv2D(64, 3, padding='same', activation='relu'),
            layers.MaxPooling2D(),
            layers.Flatten(),
            layers.Dense(128, activation='relu'),
            layers.Dense(len(class_names))
        ])

        model.compile(optimizer='adam',
                    loss=tf.keras.losses.SparseCategoricalCrossentropy(from_logits=True),
                    metrics=['accuracy'])

        epochs = 10
        callback = TrainingCallback(id_model)

        # Lancement de l'entraînement avec callback pour suivre le progrès
        model.fit(train_ds, validation_data=val_ds, epochs=epochs, callbacks=[callback])

        # Sauvegarder le modèle
        model_path = os.path.join(MODEL_DIR, f'Model{id_model}.keras')
        model.save(model_path)

        # Sauvegarder les noms des classes
        class_names_path = os.path.join(MODEL_DIR, f'ItemList{id_model}.txt')
        with open(class_names_path, 'w') as f:
            for item in class_names:
                f.write("%s\n" % item)

        print(f"Model {id_model} trained and saved.")
        # Marquer la fin de l'entraînement dans le dictionnaire de suivi
        training_progress[id_model]['status'] = 'completed'
    except Exception as e:
        print(f"Error training model {id_model}: {str(e)}")
        # Marquer l'erreur dans le dictionnaire de suivi
        training_progress[id_model]['status'] = 'error'
        training_progress[id_model]['message'] = str(e)

def async_train_model(id_model):
    """Lance l'entraînement dans un thread séparé."""
    thread = threading.Thread(target=train_model, args=(id_model,))
    thread.start()

def detect_model(id_model, imageData):
    # Charger le modèle
    model_path = os.path.join(MODEL_DIR, f'Model{id_model}.keras')
    if not os.path.exists(model_path):
        raise Exception(f"Model {id_model} not found or not trained yet.")
    model = tf.keras.models.load_model(model_path)

    # Charger les noms des classes
    class_names_path = os.path.join(MODEL_DIR, f'ItemList{id_model}.txt')
    class_names = []
    with open(class_names_path, 'r') as f:
        for line in f:
            class_names.append(line.strip())

    try:
        #img = tf.keras.utils.load_img(sunflower_path, target_size=(img_height, img_width))
        img = tf.keras.preprocessing.image.load_img(io.BytesIO(imageData.read()), target_size=(img_height, img_width))
        img_array = tf.keras.utils.img_to_array(img)
        img_array = tf.expand_dims(img_array, 0)  # Créer un batch

        predictions = model.predict(img_array)
        score = tf.nn.softmax(predictions[0])

        predicted_class = class_names[np.argmax(score)]
        confidence = 100 * np.max(score)

        return {
            "predicted_class": predicted_class,
            "confidence": confidence
        }
    except Exception as e:
        raise Exception(f"Error detecting image: {str(e)}")

@app.route('/train/<int:id_model>', methods=['POST'])
def train(id_model):
    try:
        # check if a training is already in progress
        for model in training_progress:
            if training_progress[model]['status'] == 'in progress':
                return jsonify({"error": "Training already in progress for a model."}), 400
        
        # Initialiser le progrès dans le dictionnaire
        training_progress[id_model] = {'status': 'in progress'}

        # Lancer la tâche d'entraînement en arrière-plan
        async_train_model(id_model)
        return jsonify({"message": f"Training for model {id_model} started."}), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

@app.route('/status/<int:id_model>', methods=['GET'])
def status(id_model):
    """Route pour récupérer l'avancement de l'entraînement."""
    if id_model in training_progress:
        return jsonify(training_progress[id_model]), 200
    else:
        return jsonify({"error": "No training in progress for this model."}), 404

@app.route('/detect/<int:id_model>', methods=['POST'])
def detect(id_model):
    try:
        img_file = request.files.get('img_file')
        if img_file is None:
            return jsonify({"error": "No image file provided"}), 400
        result = detect_model(id_model, img_file)
        return jsonify(result), 200
    except Exception as e:
        return jsonify({"error": str(e)}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000)