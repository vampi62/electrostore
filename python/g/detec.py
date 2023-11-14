import cv2
import numpy as np
import tensorflow as tf

# Chemin vers le modèle custom TensorFlow SavedModel
MODEL_PATH = "chemin/vers/votre/modele"

# Chargement du modèle
model = tf.saved_model.load(MODEL_PATH)

# Créez une fonction pour effectuer la détection
def detect_objects(image):
    # Pré-traitement de l'image
    input_tensor = tf.convert_to_tensor(image)
    detections = model(input_tensor)

    # Post-traitement des détections
    # Vous devrez adapter cette partie en fonction du format de vos détections
    # Par exemple, si vous utilisez TensorFlow Object Detection API, vous pouvez utiliser detections['detection_boxes'], etc.

    return detections

# Charger l'image que vous souhaitez détecter
image_path = "chemin/vers/votre/image.jpg"
image = cv2.imread(image_path)

# Effectuer la détection d'objets
detections = detect_objects(image)

# Dessiner les boîtes de détection sur l'image d'origine
for detection in detections:
    # Récupérez les coordonnées de la boîte
    x, y, w, h = detection  # Vous devez adapter cela en fonction de vos données de détection

    # Dessinez la boîte sur l'image
    cv2.rectangle(image, (int(x), int(y)), (int(x + w), int(y + h)), (0, 255, 0), 2)

# Afficher l'image avec les boîtes de détection
cv2.imshow("Object Detection", image)
cv2.waitKey(0)
cv2.destroyAllWindows()
