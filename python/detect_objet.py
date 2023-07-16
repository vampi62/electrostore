import cv2
import numpy as np
import tensorflow as tf
from object_detection.utils import visualization_utils as vis_util

# Charger le modèle de détection d'objets pré-entraîné
model_path = 'D:/GIT/electrostore/python/mon_model.h5'
detection_graph = tf.Graph()
with detection_graph.as_default():
    od_graph_def = tf.GraphDef()
    with tf.io.gfile.GFile(model_path, 'rb') as fid:
        serialized_graph = fid.read()
        od_graph_def.ParseFromString(serialized_graph)
        tf.import_graph_def(od_graph_def, name='')

# Charger les labels des classes
label_map = 'D:/GIT/electrostore/python/label_map.pbtxt'
categories = []
with open(label_map, 'r') as f:
    for line in f:
        if 'name' in line:
            name = line.split(':')[-1].strip().replace("'", "")
            categories.append(name)

# Charger l'image depuis le disque
image_path = 'D:/GIT/electrostore/python/image.jpg'
image = cv2.imread(image_path)
image_expanded = np.expand_dims(image, axis=0)

# Démarrer une session TensorFlow
with tf.Session(graph=detection_graph) as sess:
    # Obtenir les opérations nécessaires du modèle
    image_tensor = detection_graph.get_tensor_by_name('image_tensor:0')
    detection_boxes = detection_graph.get_tensor_by_name('detection_boxes:0')
    detection_scores = detection_graph.get_tensor_by_name('detection_scores:0')
    detection_classes = detection_graph.get_tensor_by_name('detection_classes:0')
    num_detections = detection_graph.get_tensor_by_name('num_detections:0')

    # Effectuer la détection d'objets
    (boxes, scores, classes, num) = sess.run(
        [detection_boxes, detection_scores, detection_classes, num_detections],
        feed_dict={image_tensor: image_expanded})

    # Visualiser les résultats de détection
    vis_util.visualize_boxes_and_labels_on_image_array(
        image,
        np.squeeze(boxes),
        np.squeeze(classes).astype(np.int32),
        np.squeeze(scores),
        categories,
        use_normalized_coordinates=True,
        line_thickness=8)

# Afficher l'image avec les résultats de détection
cv2.imshow('Object Detection', image)
cv2.waitKey(0)
cv2.destroyAllWindows()
