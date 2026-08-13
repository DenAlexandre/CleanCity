
## **Guide pour ajouter une nouvelle classe d'entraînement**

### **1. Préparation des données**

1. **Rassemblez vos images :**
   - Prenez des images contenant des exemples de la classe que vous souhaitez détecter.
   - Organisez les images dans le dossier dédié : `datasets/Donnees/images/`.
   - Je vous conseille une répartition 80 - 20 entre le dossier train et le dossier val

2. **Étiquetage des données :**
   - Labélisez les images puis mettez les dans les dossiers appropriés (labels de train et val)

3. **Vérifiez la cohérence des fichiers :**
   - Les fichiers d’annotation `.txt` doivent avoir le même nom que leurs images correspondantes (par exemple : `image1.jpg` et `image1.txt`).
   - Chaque ligne d’un fichier `.txt` doit contenir le format suivant :  
     ```text
     <class_id> <x_center> <y_center> <width> <height>
     ```
     où :
     - `<class_id>` est l’identifiant numérique de votre nouvelle classe.
     - Les autres valeurs sont normalisées (entre 0 et 1).

---

### **2. Configuration des fichiers**

1. **Mettez à jour votre fichier de configuration YOLO :**
   - Allez dans `data/detection.yaml`.
   - Ajoutez le nom de la nouvelle classe à la liste `names`. Exemple :
     ```yaml
     names:
       - bottle
       - cigarette
       - <new_class>  # Ajoutez ici le nom de votre nouvelle classe
     ```
   - Mettez à jour dans ce même fichier le `nc` il correspond au nombre de classes total

2. **Convertissez vos annotations si nécessaire :**
   - Si vos données ne sont pas déjà au format YOLO, utilisez le script `convert_coco_to_yolo.py` ou `convert_xml_to_yolo.py` pour les convertir.
   
---

### **3. Prétraitement des images**

1. **Redimensionnez vos images :**
   - Pour garantir un bon fonctionnement du modèle, uniformisez les dimensions des images avec le script `resize.py`
   - N'oubliez pas de mettre à jour les paths dans le fichier.

2. **Filtrez les images inutiles :**
   - Utilisez `filtre.py` pour éliminer les images non pertinentes ou mal étiquetées.
   - N'oubliez pas de mettre à jour le path dans le fichier.

---

### **4. Entraînement du modèle**

1. **Lancez l’entraînement :**
   - Exécutez le script `Run_Entrainement.py` pour entraîner le modèle avec vos nouvelles données.
    

2. **Surveillez les métriques :**
   - Le script génère un fichier `results.png` avec les métriques d’entraînement (losses, précision, etc.).

---

### **5. Testez le modèle**

1. **Effectuez des détections :**
   - Utilisez `Run_Detection.py` pour tester le modèle sur un ensemble d’images de validation.
    

2. **Analysez les résultats :**
   - Vérifiez les boîtes de détection sur les images annotées.
   - Les résultats détaillés (classes détectées, confiance, etc.) sont sauvegardés dans un fichier JSON.

---

### **6. Mise à jour des labels si nécessaire**

1. Si vous devez modifier les annotations après coup, utilisez le script `update_labels_yolo.py`.

- Ce programme sert si vous utilisez un datset déjà constitué et que vous souhaitez changer la `class_id`

2. Vérifiez les mises à jour pour éviter les erreurs de cohérence.

---

### **7. Vérification GPU**

1. Avant de lancer un entraînement intensif, vérifiez les capacités de votre GPU avec `testgpu.py` :
   ```bash
   python testgpu.py
   ```

