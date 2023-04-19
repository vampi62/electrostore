import cv2
import numpy as np
import requests
import tensorflow as tf

# Charger le modèle TensorFlow
model = tf.keras.models.load_model("chemin/vers/le/modele")

# Définir les classes d'objets détectables
classes = ["objet1", "objet2", "objet3", ...]

# Fonction pour détecter les objets dans une image
def detect_objects(image):
    # Prétraiter l'image
    image = cv2.resize(image, (224, 224))
    image = image.astype("float32") / 255.0
    image = tf.expand_dims(image, axis=0)

    # Faire la prédiction avec le modèle
    predictions = model.predict(image)[0]
    class_index = np.argmax(predictions)
    class_label = classes[class_index]

    # Dessiner la boîte autour de l'objet détecté
    if class_label == "objet1":
        color = (0, 255, 0)
    elif class_label == "objet2":
        color = (0, 0, 255)
    elif class_label == "objet3":
        color = (255, 0, 0)
    else:
        color = (255, 255, 255)

    cv2.rectangle(image, (x1, y1), (x2, y2), color, 2)
    cv2.putText(image, class_label, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, color, 2)

    return image

# Définir l'URL du flux vidéo
url = "http://adresse/du/flux/video"

# Ouvrir le flux vidéo
stream = requests.get(url, stream=True).raw

# Boucle pour lire chaque image du flux
while True:
    # Lire l'image du flux
    image_bytes = bytes()
    while True:
        chunk = stream.read(1024)
        if not chunk:
            break
        image_bytes += chunk
        if b'\xff\xd9' in image_bytes:
            break
    image = cv2.imdecode(np.frombuffer(image_bytes, np.uint8), -1)

    # Détecter les objets dans l'image
    image = detect_objects(image)

    # Afficher l'image avec les objets détectés
    cv2.imshow("Objet détecté", image)

    # Attendre l'appui sur la touche "q" pour quitter
    if cv2.waitKey(1) & 0xFF == ord("q"):
        break

# Fermer les fenêtres
cv2.destroyAllWindows()
