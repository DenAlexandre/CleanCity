import os

# Dossier contenant les fichiers d'annotations (txt)
annotations_dir = ""

# Parcourir chaque fichier .txt
for filename in os.listdir(annotations_dir):
    if filename.endswith(".txt"):
        filepath = os.path.join(annotations_dir, filename)

        # Lire le contenu du fichier
        with open(filepath, "r") as file:
            lines = file.readlines()

        # Modifier les annotations : 
        updated_lines = []
        for line in lines:
            parts = line.strip().split()
            if parts[0] == "0":  # Remplacer 0 par le numéro de la classe à modifier
                parts[0] = "1"   # Remplacer le 1 par le numéro de la nouvelle classe 
            updated_lines.append(" ".join(parts))

        # Écrire les modifications dans le fichier
        with open(filepath, "w") as file:
            file.write("\n".join(updated_lines))

print("Mise à jour des fichiers terminée.")
