import tensorflow as tf
import os
import numpy as np

# Définition des paramètres d'entraînement
num_epochs = 10
batch_size = 32
learning_rate = 0.001

# Définition du répertoire contenant les images
data_dir = '/chemin/vers/le/repertoire/contenant/les/images'

# Chargement des noms des fichiers d'images et de leurs étiquettes
image_paths = []
labels = []
class_names = os.listdir(data_dir)
for class_name in class_names:
    class_dir = os.path.join(data_dir, class_name)
    if os.path.isdir(class_dir):
        for image_name in os.listdir(class_dir):
            image_path = os.path.join(class_dir, image_name)
            image_paths.append(image_path)
            labels.append(class_name)
num_images = len(image_paths)
num_classes = len(class_names)

# Mélange des données
random_indices = np.random.permutation(num_images)
image_paths = [image_paths[i] for i in random_indices]
labels = [labels[i] for i in random_indices]

# Création d'un générateur de données
def data_generator():
    for i in range(0, num_images, batch_size):
        batch_images = []
        batch_labels = []
        for j in range(i, min(i + batch_size, num_images)):
            image = tf.keras.preprocessing.image.load_img(image_paths[j], target_size=(224, 224))
            image = tf.keras.preprocessing.image.img_to_array(image)
            image = tf.keras.applications.resnet50.preprocess_input(image)
            label = tf.one_hot(class_names.index(labels[j]), num_classes)
            batch_images.append(image)
            batch_labels.append(label)
        yield np.array(batch_images), np.array(batch_labels)

# Création du modèle
base_model = tf.keras.applications.ResNet50(weights='imagenet', include_top=False, input_shape=(224, 224, 3))
flatten = tf.keras.layers.Flatten()(base_model.output)
output = tf.keras.layers.Dense(num_classes, activation='softmax')(flatten)
model = tf.keras.models.Model(inputs=base_model.input, outputs=output)

# Compilation du modèle
model.compile(optimizer=tf.keras.optimizers.Adam(learning_rate),
              loss=tf.keras.losses.CategoricalCrossentropy(),
              metrics=['accuracy'])

# Entraînement du modèle
model.fit(data_generator(), steps_per_epoch=num_images // batch_size, epochs=num_epochs)
# Sauvegarder le modèle
model.save('mon_model.h5')
