import cv2
from ultralytics import YOLO
import time

# Charger le modèle YOLO 
model = YOLO('Chemin vers le modèle Yolo')  

# Ouvrir le flux vidéo de la webcam
cap = cv2.VideoCapture(0)

# Configurer la résolution de la webcam
FRAME_WIDTH = 1280
FRAME_HEIGHT = 720
cap.set(cv2.CAP_PROP_FRAME_WIDTH, FRAME_WIDTH)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, FRAME_HEIGHT)

if not cap.isOpened():
    print("Erreur : Impossible d'accéder à la webcam.")
    exit()

# Définir le temps entre chaque frame pour limiter à 10 FPS
FRAME_DELAY = 1 / 100 # 10 FPS
last_frame_time = 0

# Boucle pour traiter chaque frame de la webcam
try:
    while True:
        # Attendre pour limiter à 10 FPS
        current_time = time.time()
        if current_time - last_frame_time < FRAME_DELAY:
            continue
        last_frame_time = current_time

        # Lire une frame de la webcam
        ret, frame = cap.read()
        if not ret:
            print("Erreur : Impossible de lire le flux vidéo.")
            break

        # Redimensionner la frame si nécessaire
        resized_frame = cv2.resize(frame, (640, 640))  # Taille d'entrée pour YOLO (modifiable)

        # Appliquer le modèle YOLO pour détecter les objets
        results = model(resized_frame)

        # Dessiner les boîtes englobantes et les étiquettes sur l'image originale
        for result in results:
            boxes = result.boxes.xyxy  # Coordonnées des boîtes englobantes
            confidences = result.boxes.conf  # Confiance des prédictions
            classes = result.boxes.cls  # Classes détectées
            
            for box, conf, cls in zip(boxes, confidences, classes):
                x1, y1, x2, y2 = map(int, box)  # Convertir les coordonnées en entier
                label = f"{model.names[int(cls)]} ({conf:.2f})"
                cv2.rectangle(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)  # Dessiner la boîte
                cv2.putText(frame, label, (x1, y1 - 10), cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 2)

        # Afficher la frame avec les détections
        cv2.imshow("YOLO - Détection en direct", frame)

        # Quitter la boucle si l'utilisateur appuie sur la touche 'q'
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

except KeyboardInterrupt:
    print("\nArrêt manuel par l'utilisateur.")

# Libérer les ressources
cap.release()
cv2.destroyAllWindows()
