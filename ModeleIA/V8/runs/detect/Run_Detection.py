from ultralytics import YOLO
import os
import json
import cv2

class YOLOTest:
    def __init__(self, model_path, directory_img, directory_save_results, confidence_threshold=0.25):
        """
        Initialisation de la classe pour le test du modèle YOLO.
        """
        self.model = YOLO(model_path)
        self.class_names = {0: "bottle"}  # mettre les classes avec les numéros de classe du modèle
        print("Classes du modèle :", self.class_names)

        self.directory_img = directory_img
        self.directory_save_results = directory_save_results
        self.confidence_threshold = confidence_threshold

        # Créer le dossier de résultats s'il n'existe pas
        os.makedirs(self.directory_save_results, exist_ok=True)

    def run_detection(self):
        """
        Exécute la détection sur toutes les images du dossier spécifié.
        """
        json_results = {}

        for file_name in os.listdir(self.directory_img):
            if file_name.lower().endswith((".jpg", ".png")):
                image_path = os.path.join(self.directory_img, file_name)

                # Lire l'image avec OpenCV
                image = cv2.imread(image_path)
                if image is None:
                    print(f"Erreur : Impossible de lire l'image {image_path}")
                    continue

                try:
                    # Effectuer l'inférence
                    result = self.model.predict(image, conf=self.confidence_threshold)

                    # Traiter les résultats
                    detections = []
                    for r in result[0].boxes:
                        box = r.xyxy.tolist()[0]
                        confidence = float(r.conf)
                        class_id = int(r.cls)

                        if class_id in self.class_names:
                            class_name = self.class_names[class_id]

                            detection = {
                                "class": class_name,
                                "confidence": confidence,
                                "bounding_box": {
                                    "x1": int(box[0]),
                                    "y1": int(box[1]),
                                    "x2": int(box[2]),
                                    "y2": int(box[3])
                                }
                            }
                            detections.append(detection)

                            # Dessiner les boîtes englobantes sur l'image
                            cv2.rectangle(image, (int(box[0]), int(box[1])), (int(box[2]), int(box[3])), (0, 255, 0), 2)
                            cv2.putText(
                                image,
                                f"{class_name} ({confidence:.2f})",
                                (int(box[0]), int(box[1]) - 10),
                                cv2.FONT_HERSHEY_SIMPLEX,
                                0.5,
                                (0, 255, 0),
                                2
                            )

                    # Sauvegarder les résultats pour cette image
                    json_results[file_name] = detections

                    # Sauvegarder l'image annotée
                    annotated_path = os.path.join(self.directory_save_results, f"annotated_{file_name}")
                    cv2.imwrite(annotated_path, image)

                except Exception as e:
                    print(f"Erreur lors du traitement de l'image {file_name}: {e}")

        # Sauvegarder les résultats en JSON
        json_path = os.path.join(self.directory_save_results, "detections_results.json")
        with open(json_path, 'w') as json_file:
            json.dump(json_results, json_file, indent=2)

        print("Détection terminée et résultats enregistrés.")

# Utilisation

# Chemin de base dynamique
base_path = os.path.dirname(__file__)

# Chemins relatifs pour les dossiers
model_path = 'Chemin vers le modèle YOLO'  # Chemin vers le modèle YOLO
directory_img = os.path.join(base_path, '../../datasets/Donnees/images/test')
directory_save_results = os.path.join(base_path, '../../Sortie')

confidence_threshold = 0.5  

yolo_test = YOLOTest(model_path, directory_img, directory_save_results, confidence_threshold)
yolo_test.run_detection()
