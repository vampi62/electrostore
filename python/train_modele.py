import tensorflow as tf
import os
import cv2
import numpy as np

# Définir les classes d'objet
class_names = ['objet1', 'objet2', 'objet3']

# Définir les répertoires d'entraînement et de validation
train_dir = 'chemin/vers/repertoire/dentrainement'
val_dir = 'chemin/vers/repertoire/devalidation'

# Définir les paramètres d'entraînement
batch_size = 32
epochs = 10

# Définir la fonction pour prétraiter les images
def preprocess_image(image):
    image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
    image = image / 255.0
    return image

# Définir la fonction pour charger les images et les étiquettes
def load_data(data_dir):
    images = []
    labels = []
    for class_name in class_names:
        dir_path = os.path.join(data_dir, class_name)
        for img_name in os.listdir(dir_path):
            img_path = os.path.join(dir_path, img_name)
            image = cv2.imread(img_path)
            image = preprocess_image(image)
            label = class_names.index(class_name)
            images.append(image)
            labels.append(label)
    return np.array(images), np.array(labels)

# Charger les données d'entraînement et de validation
train_images, train_labels = load_data(train_dir)
val_images, val_labels = load_data(val_dir)

# Définir le modèle
model = tf.keras.models.Sequential([
    tf.keras.layers.Conv2D(32, (3, 3), activation='relu', input_shape=(224, 224, 3)),
    tf.keras.layers.MaxPooling2D((2, 2)),
    tf.keras.layers.Conv2D(64, (3, 3), activation='relu'),
    tf.keras.layers.MaxPooling2D((2, 2)),
    tf.keras.layers.Conv2D(128, (3, 3), activation='relu'),
    tf.keras.layers.MaxPooling2D((2, 2)),
    tf.keras.layers.Flatten(),
    tf.keras.layers.Dense(64, activation='relu'),
    tf.keras.layers.Dense(len(class_names), activation='softmax')
])

# Compiler le modèle
model.compile(optimizer='adam',
              loss='sparse_categorical_crossentropy',
              metrics=['accuracy'])

# Entraîner le modèle
model.fit(train_images, train_labels, epochs=epochs, batch_size=batch_size, validation_data=(val_images, val_labels))

# Sauvegarder le modèle
model.save('mon_model.h5')
