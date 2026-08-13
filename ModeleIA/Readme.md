# **Documentation pour le projet YOLO**

## **Introduction**
Ce projet utilise le modèle YOLO (You Only Look Once) pour détecter des objets spécifiques dans des images, avec un pipeline complet pour préparer, entraîner, et évaluer un modèle de détection d'objets. Cette documentation vous guidera à chaque étape, même si vous êtes débutant.

---

## **Structure du projet**

Voici une description de l'arborescence du projet et du rôle de chaque dossier/fichier :

### **Dossiers principaux :**
- **`data/`** :
  - Contient le fichier `detection.yaml` qui configure les chemins des données pour l'entraînement et la validation.

- **`datasets/`** :
  - Contient les données d'entraînement, validation, et test.
  - Sous-dossiers importants :
    - **`Donnees/images/train/`** :
      - **`images/`** : Placez ici les images pour l'entraînement.
      - **`labels/`** : Placez ici les fichiers d'annotations `.txt` pour les images d'entraînement.
    - **`Donnees/images/val/`** :
      - **`images/`** : Placez ici les images pour la validation.
      - **`labels/`** : Placez ici les fichiers d'annotations `.txt` pour les images de validation.
    - **`Donnees/images/test/`** :
      - Contient les images que vous voulez tester après l'entraînement.

- **`runs/detect/`** :
  - Contient les scripts Python pour préparer et analyser les données.
  - Sous-dossiers importants :
    - **`temp/`** : Contient les fichiers intermédiaires(temporaires) comme `coco_annotations.json`.
    - **`tempSortie/`** : Contient les fichiers de sortie transformés, comme les annotations converties. (Exemple json convertie en txt)
    - **`Sortie/`** : Contient les résultats finaux après détection.

### **Fichiers importants :**
- **`detection.yaml`** :
  - Configure les chemins des images et annotations pour l'entraînement et la validation.
- **Scripts Python dans `runs/detect/`** :
  - **`Run_Entrainement.py`** : Lance l'entraînement du modèle YOLO.
  - **`compare_txt_folders.py`** : Identifie les fichiers d'annotations non conformes.
  - **`convert_xml_to_yolo.py`** : Convertit les annotations des fichiers xml en annotations YOLO (txt).
  - **`convert_coco_to_yolo.py`** : Convertit les annotations COCO en annotations YOLO.
  - **`delete_img_and_label.py`** : Supprime les images et annotations non conformes.
  - **`filtre.py`** : Filtre les annotations pour ne garder que celles valides.
  - **`Run_Detection.py`** : Permet de tester le modèle sur des images.
  - **`Resize.py`** : Permet de changer la taille en pixel des images.
  - **`Update_labels_yolo.py`** : Permet de changer le numéro d'une classe à partir d'un dossier.
  - **`Webcam.py`** : Permet de faire fonctionner le modèle à partir d'une caméra (ici webcam).

---

## **Guide d'utilisation**

### **1. Préparation des données**
Vous devez organiser vos données avant de lancer l'entraînement.

1. **Structure des dossiers :**
   - Placez vos images et annotations dans les dossiers correspondants :
     - **`datasets/Donnees/images/train/images/`** : Images pour l'entraînement.
     - **`datasets/Donnees/images/train/labels/`** : Annotations YOLO (`.txt`) pour ces images.
     - **`datasets/Donnees/images/val/images/`** : Images pour la validation.
     - **`datasets/Donnees/images/val/labels/`** : Annotations YOLO (`.txt`) pour ces images.
     - **`datasets/Donnees/images/test/`** : Images pour tester le modèle.

2. **Format des annotations YOLO :**
   - Chaque ligne d'un fichier `.txt` correspond à une annotation d'objet, au format suivant :
     ```
     <class_id> <x_center> <y_center> <width> <height>
     ```
   - Les valeurs `x_center`, `y_center`, `width`, et `height` doivent être normalisées entre 0 et 1.

---

### **2. Lancer l'entraînement**
Pour entraîner le modèle YOLO :

1. Ouvrez un terminal dans le répertoire `YOLO_WASTE`.
2. Exécutez le script `Run_Entrainement.py` :
   ```bash
   python runs/detect/Run_Entrainement.py
   ```
3. **Personnaliser les paramètres d'entraînement :**
   - Vous pouvez modifier les paramètres comme le nombre d'époques (`epochs`), la taille des images (`imgsz`), et la taille des lots (`batch`) dans le script `Run_Entrainement.py`.

---

### **3. Valider les annotations**
Avant l'entraînement, vérifiez que toutes les annotations sont conformes.

1. Exécutez le script `compare_txt_folders.py` :
   ```bash
   python runs/detect/compare_txt_folders.py
   ```
2. Les fichiers non conformes seront listés dans `runs/detect/compare.json`.
3. Pour supprimer les fichiers non conformes, exécutez :
   ```bash
   python runs/detect/delete_img_and_label.py
   ```

---

### **4. Convertir des annotations COCO en YOLO**
Si vous avez des annotations au format COCO, utilisez le script `convert_coco_to_yolo.py` :
```bash
python runs/detect/convert_coco_to_yolo.py
```
Les fichiers YOLO générés seront placés dans `runs/detect/tempSortie/`.

---

### **5. Tester le modèle**
Après l'entraînement, vous pouvez tester le modèle sur de nouvelles images.

1. Placez vos images dans `datasets/Donnees/images/test/`.
2. Exécutez le script `Run_Detection.py` :
   ```bash
   python runs/detect/Run_Detection.py
   ```
3. Les résultats seront enregistrés dans `runs/detect/Sortie/`.

---

### **6. Nettoyer les annotations**
Pour filtrer les annotations incorrectes dans un dossier, à savoir que le script parcours tous les `.txt` et supprime toutes les lignes ne faisant pas 5 colonnes, utilisez le script `filtre.py` :
```bash
python runs/detect/filtre.py
```

---

## **Configuration de `detection.yaml`**
Le fichier `detection.yaml` configure les chemins des données. Voici un exemple de contenu :

```yaml
train: datasets/Donnees/images/train/images
val: datasets/Donnees/images/val/images

nc: 1  # Nombre de classes (ici, 1 pour les cigarettes)
names: ['cigarette']
```

- **`train`** : Chemin vers les images d'entraînement.
- **`val`** : Chemin vers les images de validation.
- **`nc`** : Nombre de classes (1 pour détecter uniquement les cigarettes).
- **`names`** : Liste des noms de classes.

---

## **Dépendances nécessaires**
1. Installez les bibliothèques Python requises :
   ```bash
   pip install ultralytics opencv-python-headless
   ```
2. Assurez-vous d'avoir un GPU compatible avec CUDA pour accélérer l'entraînement.

---

## **FAQ**

### **1. Comment ajouter une nouvelle classe à détecter ?**
- Modifiez le fichier `detection.yaml` :
  - Changez le nombre de classes (`nc`) et ajoutez les noms des nouvelles classes dans `names`.
- Ajoutez des images annotées pour chaque nouvelle classe.

### **2. Comment savoir si le modèle est bien entraîné ?**
- Vérifiez les métriques affichées pendant l'entraînement, comme `mAP50` et `mAP50-95`. Une valeur élevée (proche de 1.0) indique de bonnes performances.

---

## **Contact**
Si vous avez des questions ou rencontrez des problèmes, n'hésitez pas à demander de l'aide. 

