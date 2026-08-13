import os

# Définir le chemin relatif de base pour le script
base_path = os.path.dirname(__file__)

labels_dir = os.path.join(base_path, '../../datasets/Donnees/images/val/labels')

# Parcourir les fichiers dans le dossier
for file in os.listdir(labels_dir):
    if file.endswith(".txt"):
        file_path = os.path.join(labels_dir, file)
        with open(file_path, "r") as f:
            lines = f.readlines()
        # Garder uniquement les lignes avec boîtes YOLO (qui ont exactement 5 colonnes)
        filtered_lines = [line for line in lines if len(line.split()) == 5]
        with open(file_path, "w") as f:
            f.writelines(filtered_lines)
