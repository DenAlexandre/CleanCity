import os
from ultralytics import YOLO

if __name__ == '__main__':
    # Récupère le chemin absolu du répertoire où se trouve ce script
    base_path = os.path.dirname(__file__)

    # Construit dynamiquement le chemin vers le fichier YAML
    data_path = os.path.join(base_path, '../../data/detection.yaml')

    # Charger le modèle YOLOv10x pré-entraîné
    model = YOLO('yolov8x.pt')
    model.to('cuda:0')

    # Démarrer l'entraînement avec les paramètres ajustés
    results = model.train(
        data=data_path,           # Chemin vers le fichier YAML contenant les classes 
        epochs=300,               # Nombre d'epochs
        imgsz=640,                # Taille des images
        batch=12,                 # Taille des lots 
        patience=20,              # Arrêt précoce en cas de stagnation  
        lr0=0.001,                 # Taux d'apprentissage
        lrf=0.2,                  # Taux d'apprentissage final
        val=True,                 # Validation à chaque époque
        half=True,                # Précision mixte pour optimiser la mémoire GPU
        name='bottle_cigarette_v10x' # Nom du dossier du modèle sauvegardé
    )

    print("Entraînement terminé. Modèle sauvegardé dans le dossier runs/train/bottle_cigarette_v10x.")
