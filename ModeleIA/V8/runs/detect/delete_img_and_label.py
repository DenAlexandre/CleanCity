import os
import json

def delete_non_compliant_files(json_file, labels_dir, images_dir):
    """
    Supprime les fichiers .txt non conformes et leurs images correspondantes.

    Args:
        json_file (str): Chemin vers le fichier JSON contenant la liste des fichiers non conformes.
        labels_dir (str): Chemin vers le dossier contenant les fichiers .txt.
        images_dir (str): Chemin vers le dossier contenant les fichiers d'images .jpg.
    """
    # Charger la liste des fichiers non conformes depuis le JSON
    with open(json_file, 'r') as f:
        data = json.load(f)

    non_compliant_files = data.get("non_compliant_files", [])

    for file in non_compliant_files:
        # Chemin complet pour le fichier .txt
        txt_path = os.path.join(labels_dir, file)

        # Chemin complet pour le fichier image correspondant (.jpg)
        image_name = os.path.splitext(file)[0] + ".jpg"
        image_path = os.path.join(images_dir, image_name)

        # Supprimer le fichier .txt
        if os.path.exists(txt_path):
            os.remove(txt_path)
            print(f"Supprimé: {txt_path}")
        else:
            print(f"Fichier .txt introuvable: {txt_path}")

        # Supprimer le fichier image
        if os.path.exists(image_path):
            os.remove(image_path)
            print(f"Supprimé: {image_path}")
        else:
            print(f"Fichier image introuvable: {image_path}")

if __name__ == "__main__":
    # Définir le chemin de base dynamique
    base_path = os.path.dirname(__file__)

    json_file = os.path.join(base_path, '../compare.json')
    labels_dir = os.path.join(base_path, '../../datasets/Donnees/images/val/labels')
    images_dir = os.path.join(base_path, '../../datasets/Donnees/images/val/images')

    delete_non_compliant_files(json_file, labels_dir, images_dir)
