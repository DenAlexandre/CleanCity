import os
import json

# Définir le chemin de base dynamique
base_path = os.path.dirname(__file__)

# Chemins relatifs
coco_json_path = os.path.join(base_path, 'temp/coco_annotations.json')  # Chemin vers le fichier COCO JSON
output_dir = os.path.join(base_path, 'tempSortie')  # Dossier de sortie pour les fichiers .txt

# Charger le fichier JSON
with open(coco_json_path, 'r') as f:
    coco_data = json.load(f)

# Créer le dossier de sortie s'il n'existe pas
os.makedirs(output_dir, exist_ok=True)

# Récupérer les infos des images et des annotations
images = {img['id']: img for img in coco_data['images']}
annotations = coco_data['annotations']
categories = {cat['id']: i for i, cat in enumerate(coco_data['categories'])}  # Map COCO ID -> YOLO class ID

# Parcourir les annotations
for image_id, image_info in images.items():
    # Récupérer les annotations pour cette image
    image_annotations = [ann for ann in annotations if ann['image_id'] == image_id]

    # Dimensions de l'image
    img_width = image_info['width']
    img_height = image_info['height']

    # Préparer les annotations pour cette image
    annotations_line = []
    for ann in image_annotations:
        # Coordonnées de la boîte (COCO format : [x, y, width, height])
        x, y, width, height = ann['bbox']

        # Convertir en YOLO format (centre x, centre y, largeur, hauteur, normalisés)
        x_center = (x + width / 2) / img_width
        y_center = (y + height / 2) / img_height
        width /= img_width
        height /= img_height

        # ID de la classe
        category_id = ann['category_id']
        class_id = categories[category_id]

        # Ajouter l'annotation formatée
        annotations_line.append(f"{class_id} {x_center} {y_center} {width} {height}")

    # Créer un fichier .txt pour cette image et écrire toutes les annotations sur une seule ligne
    txt_file_path = os.path.join(output_dir, f"{os.path.splitext(image_info['file_name'])[0]}.txt")
    with open(txt_file_path, 'w') as f:
        f.write(" ".join(annotations_line))
