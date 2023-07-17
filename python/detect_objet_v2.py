import cv2
import numpy as np
from tensorflow import keras

# Charger le modèle pré-entraîné
model = keras.models.load_model('D:/git/electrostore/python/mon_model.h5')

# Classement des étiquettes des objets
labels = ['class', 'object1', 'object2', '...']  # Remplacez par vos étiquettes réelles

def detect_object(image_path):
    # Charger l'image
    image = cv2.imread(image_path)
    image_rgb = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)

    # Redimensionner l'image à la taille d'entrée du modèle
    target_size = (150, 150)  # Remplacez par la taille d'entrée du modèle
    resized_image = cv2.resize(image_rgb, target_size)

    # Prétraiter l'image
    preprocessed_image = resized_image / 255.0  # Normalisation

    # Effectuer la prédiction d'objet
    prediction = model.predict(np.expand_dims(preprocessed_image, axis=0))
    predicted_class = np.argmax(prediction)

    # Afficher le résultat de la prédiction
    label = labels[predicted_class]
    cv2.putText(image, label, (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 1.0, (0, 255, 0), 2)
    cv2.imshow('Object Detection', image)
    cv2.waitKey(0)
    cv2.destroyAllWindows()

# Exemple d'utilisation
detect_object('D:/git/electrostore/python/image.jpg')