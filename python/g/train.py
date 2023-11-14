import os
import numpy as np
import tensorflow as tf
from tensorflow import keras
from tensorflow.keras import layers
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import Input
from tensorflow.keras.layers import Flatten, Dense
from tensorflow.keras.layers import Conv2D, MaxPooling2D
from tensorflow.keras.optimizers import Adam
from tensorflow.keras.losses import SparseCategoricalCrossentropy
from tensorflow.keras.metrics import MeanIoU
from tensorflow.keras.utils import to_categorical
from PIL import Image

# Charger les images et extraire les annotations à partir de la structure du dossier
image_dir = "chemin_vers_le_dossier_images"
images = []
annotations = []

# Définir les classes d'objets (ajustez selon vos besoins)
class_names = os.listdir(image_dir)

for class_name in class_names:
    class_dir = os.path.join(image_dir, class_name)
    for image_file in os.listdir(class_dir):
        image_path = os.path.join(class_dir, image_file)
        image = np.array(Image.open(image_path))
        class_id = class_names.index(class_name)
        images.append(image)
        annotations.append(class_id)

images = np.array(images)
annotations = np.array(annotations)

# Créer un modèle de détection d'objet
model = Sequential()

# Ajouter des couches Conv2D et MaxPooling2D (ajustez selon vos besoins)
model.add(Conv2D(32, (3, 3), activation='relu', input_shape=(image_height, image_width, 3)))
model.add(MaxPooling2D((2, 2))
model.add(Conv2D(64, (3, 3), activation='relu'))
model.add(MaxPooling2D((2, 2))
model.add(Conv2D(64, (3, 3), activation='relu'))

# Ajouter des couches de classification pour chaque classe d'objet
model.add(Flatten())
model.add(Dense(128, activation='relu'))
model.add(Dense(len(class_names), activation='softmax'))

# Compiler le modèle
model.compile(optimizer='adam', loss='sparse_categorical_crossentropy', metrics=['accuracy'])

# Entraîner le modèle
model.fit(images, annotations, epochs=10, batch_size=32)

# Sauvegarder le modèle
model.save('mon_modele_detection_objet.h5')
