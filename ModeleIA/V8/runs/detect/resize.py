import os
import cv2

def resize_with_padding(input_dir, output_dir, target_size=(1920, 1080)):
    """
    Redimensionne les images avec conservation des proportions et ajoute des bordures.

    Args:
        input_dir (str): Chemin du dossier contenant les images d'origine.
        output_dir (str): Chemin du dossier pour sauvegarder les images redimensionnées.
        target_size (tuple): Taille cible (largeur, hauteur).
    """
    # Crée le dossier de sortie s'il n'existe pas
    os.makedirs(output_dir, exist_ok=True)

    for file_name in os.listdir(input_dir):
        if file_name.lower().endswith((".jpg", ".png", ".jpeg")):
            img_path = os.path.join(input_dir, file_name)
            img = cv2.imread(img_path)

            if img is None:
                print(f"Erreur : Impossible de lire l'image {img_path}")
                continue

            # Calculer le redimensionnement tout en conservant les proportions
            original_size = img.shape[:2]  # (hauteur, largeur)
            ratio = min(target_size[1] / original_size[0], target_size[0] / original_size[1])
            new_size = (int(original_size[1] * ratio), int(original_size[0] * ratio))  # (largeur, hauteur)

            resized_img = cv2.resize(img, new_size)

            # Ajouter des bordures pour atteindre la taille cible
            delta_w = target_size[0] - new_size[0]
            delta_h = target_size[1] - new_size[1]
            top, bottom = delta_h // 2, delta_h - (delta_h // 2)
            left, right = delta_w // 2, delta_w - (delta_w // 2)

            padded_img = cv2.copyMakeBorder(resized_img, top, bottom, left, right, cv2.BORDER_CONSTANT, value=[128, 128, 128])

            # Sauvegarder l'image redimensionnée
            output_path = os.path.join(output_dir, file_name)
            cv2.imwrite(output_path, padded_img)
            print(f"Image redimensionnée et sauvegardée avec bordures : {output_path}")

# Exemple d'utilisation
if __name__ == "__main__":
    # Dossier contenant les images d'origine
    input_directory = "" # Chemin du dossier contenant les images d'origine

    # Dossier pour sauvegarder les images redimensionnées
    output_directory = "" # Chemin du dossier pour sauvegarder les images redimensionnées

    # Taille cible (largeur, hauteur)
    target_resolution = (1920, 1080)

    resize_with_padding(input_directory, output_directory, target_size=target_resolution)
