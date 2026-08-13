import os
import xml.etree.ElementTree as ET

def convert_to_yolo_format(xml_file, output_dir, classes):
    # Lire et parser le fichier XML
    tree = ET.parse(xml_file)
    root = tree.getroot()

    # Taille de l'image
    size = root.find("size")
    width = int(size.find("width").text)
    height = int(size.find("height").text)

    # Création du fichier .txt de sortie
    txt_file = os.path.join(output_dir, os.path.splitext(os.path.basename(xml_file))[0] + ".txt")

    with open(txt_file, "w") as f:
        for obj in root.findall("object"):
            class_name = obj.find("name").text
            if class_name not in classes:
                continue
            class_id = classes.index(class_name)

            # Récupérer les coordonnées du bounding box (utilisation de robndbox au lieu de bndbox)
            robndbox = obj.find("robndbox")
            if robndbox is None:
                print(f"Aucune balise <robndbox> trouvée dans {xml_file}")
                continue  # Passer à l'objet suivant si robndbox est absent

            # Conversion des coordonnées
            cx = float(robndbox.find("cx").text)
            cy = float(robndbox.find("cy").text)
            w = float(robndbox.find("w").text)
            h = float(robndbox.find("h").text)

            # Conversion des coordonnées en format YOLO (normalisé)
            x_center = cx / width
            y_center = cy / height
            bbox_width = w / width
            bbox_height = h / height

            # Écrire les annotations dans le fichier .txt
            f.write(f"{class_id} {x_center} {y_center} {bbox_width} {bbox_height}\n")

# Dossiers
xml_folder = ""  # Dossier contenant les fichiers .xml
output_folder = ""  # Dossier pour les fichiers .txt convertis
os.makedirs(output_folder, exist_ok=True)

# Liste des classes du dataset
classes = ["bottle"]  # Remplace par les classes du dataset

# Conversion de tous les fichiers XML dans le dossier
for xml_file in os.listdir(xml_folder):
    if xml_file.endswith(".xml"):
        convert_to_yolo_format(os.path.join(xml_folder, xml_file), output_folder, classes)

print("Conversion terminée !")
