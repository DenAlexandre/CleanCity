from ultralytics import YOLO
import os
import json
import cv2
import time
import threading

class YOLOTest:
    def __init__(self, model_path, directory_img, directory_save_results, confidence_threshold=0.25, class_filter=None):
        """
        Initialisation de la classe pour le test du modèle YOLO.
        """
        self.model = YOLO(model_path)
        self.class_names = {0: 'bottle', 1: 'cigarette', 39: 'bottle'}  # Mapping des classes
        print("Classes du modèle :", self.class_names)

        self.directory_img = directory_img
        self.directory_save_results = directory_save_results
        self.confidence_threshold = confidence_threshold
        self.class_filter = class_filter

        # Créer le dossier de résultats s'il n'existe pas
        os.makedirs(self.directory_save_results, exist_ok=True)

        # Stockage des fichiers déjà analysés
        self.processed_files = set()

    def run_detection(self):
        """
        Exécute la détection en continu sur toutes les nouvelles images ajoutées au dossier spécifié.
        """
        json_results = {}

        while True:
            for file_name in os.listdir(self.directory_img):
                if file_name.lower().endswith((".jpg", ".png")) and file_name not in self.processed_files:
                    image_path = os.path.join(self.directory_img, file_name)

                    # Lire l'image avec OpenCV
                    image = cv2.imread(image_path)
                    if image is None:
                        print(f"Erreur : Impossible de lire l'image {image_path}")
                        continue

                    # Obtenir la résolution originale de l'image
                    original_height, original_width = image.shape[:2]

                    try:
                        # Effectuer l'inférence avec la résolution originale
                        result = self.model.predict(image, conf=self.confidence_threshold, imgsz=(original_width, original_height))

                        # Traiter les résultats
                        detections = []
                        for r in result[0].boxes:
                            box = r.xyxy.tolist()[0]
                            confidence = float(r.conf)
                            class_id = int(r.cls)

                            # Filtrer les classes si un filtre est défini
                            if self.class_filter is None or class_id in self.class_filter:
                                class_name = self.class_names.get(class_id, f"class_{class_id}")

                                x1, y1, x2, y2 = map(int, box)

                                detection = {
                                    "class": class_name,
                                    "confidence": confidence,
                                    "bounding_box": {
                                        "x1": x1,
                                        "y1": y1,
                                        "x2": x2,
                                        "y2": y2
                                    }
                                }
                                detections.append(detection)

                                # Dessiner les boîtes englobantes sur l'image
                                cv2.rectangle(image, (x1, y1), (x2, y2), (0, 255, 0), 2)
                                cv2.putText(
                                    image,
                                    f"{class_name} ({confidence:.2f})",
                                    (x1, y1 - 10),
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

                        # Ajouter le fichier à la liste des fichiers traités
                        self.processed_files.add(file_name)

                    except Exception as e:
                        print(f"Erreur lors du traitement de l'image {file_name}: {e}")

            # Sauvegarder les résultats en JSON
            json_path = os.path.join(self.directory_save_results, "detections_results.json")
            with open(json_path, 'w') as json_file:
                json.dump(json_results, json_file, indent=2)

            # Attendre avant de vérifier à nouveau les nouveaux fichiers
            time.sleep(0.1)

        print("Détection terminée et résultats enregistrés.")

# Utilisation

# Chemin de base dynamique
base_path = os.path.dirname(__file__)

# Modèle et configuration pour la classe 39 uniquement
def run_model_1():
    test_model_path = 'C:/Users/Leroy/Documents/GitHub/Yolo_waste/yolov10x.pt'
    test_directory_img = os.path.join(base_path, '../../datasets/Donnees/images/test')
    test_directory_save_results = os.path.join(base_path, '../../Sortie')

    yolo_test_1 = YOLOTest(test_model_path, test_directory_img, test_directory_save_results, confidence_threshold=0.2, class_filter=[39])
    yolo_test_1.run_detection()

# Modèle et configuration pour les classes 0 et 1
def run_model_2():
    testb_model_path = 'C:/Users/Leroy/Documents/GitHub/Yolo_waste/runs/detect/YoloBouteilleEtCigarette/weights/best.pt'
    testb_directory_img = os.path.join(base_path, '../../datasets/Donnees/images/testb')
    testb_directory_save_results = os.path.join(base_path, '../../Sortie')

    yolo_test_2 = YOLOTest(testb_model_path, testb_directory_img, testb_directory_save_results, confidence_threshold=0.2, class_filter=[0, 1])
    yolo_test_2.run_detection()

# Lancement des deux modèles en parallèle
thread_1 = threading.Thread(target=run_model_1)
thread_2 = threading.Thread(target=run_model_2)

thread_1.start()
thread_2.start()

thread_1.join()
thread_2.join()
