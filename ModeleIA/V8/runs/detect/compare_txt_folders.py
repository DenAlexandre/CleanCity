import os
import json

def detect_non_compliant_files(labels_dir, output_file):
    """
    Détecte les fichiers .txt non conformes (ne contenant pas exactement 5 colonnes par ligne) dans un dossier.

    Args:
        labels_dir (str): Chemin vers le dossier contenant les annotations .txt.
        output_file (str): Chemin du fichier de sortie pour sauvegarder la liste des fichiers concernés.
    """
    non_compliant_files = []

    for file in os.listdir(labels_dir):
        if file.endswith('.txt'):
            file_path = os.path.join(labels_dir, file)
            with open(file_path, 'r') as f:
                lines = f.readlines()

            # Vérifier si une ligne contient plus ou moins de 5 colonnes
            for line in lines:
                columns = line.split()
                if len(columns) != 5:
                    non_compliant_files.append(file)
                    break

    # Sauvegarder les fichiers concernés dans un fichier JSON
    with open(output_file, 'w') as outfile:
        json.dump({"non_compliant_files": non_compliant_files}, outfile, indent=4)

    print(f"Les fichiers non conformes ont été sauvegardés dans {output_file}")

if __name__ == "__main__":
    # Définir le chemin de base dynamique
    base_path = os.path.dirname(__file__)

    # Construire les chemins relatifs dynamiquement
    labels_dir = os.path.join(base_path, '../../datasets/Donnees/images/val/labels')
    output_file = os.path.join(base_path, 'compare.json')

    detect_non_compliant_files(labels_dir, output_file)
