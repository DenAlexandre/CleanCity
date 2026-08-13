Pour tous les modules de cet hyperviseur (traitement des données, serveur, site), il faut avoir MongoDB ouvert.

## Installation
Les fichiers de traitement et le site ont des dépendences à installer.

```
$ npm i --force
```
Cette commande installe les dépendences du **client**, du **server**, et du **traitement**.

## Traitement des données
Dans le dossier **traitement** se trouve les fichiers pythons permettant le traitement des données dans les fichiers JSON.

Démarrez les scripts de traitement dans le dossier traitement, où tapez `cd traitement/` pour y naviguer à partir de l'invite de commande.

Avant de lancer le traitment, il faut lancer l'API StreetFinder qui permet de retrouver la rue associée à des coordonnées géographiques. Pour cela, il faut lancer le controlleur de l'API, qui est `StreetFinderAPIController.py`.

```
$ python StreetFinderAPIController.py
```
Cette commande permet de lancer l'API.

Il faut ensuite lancer `Traitement.py` dans un autre invite de commande pour faire le traitement.

```
$ python StreetFinderAPIController.py
```
Cette commande lance le script effectuant le traitement des données, qui sont ensuite envoyées sur MongoDB.

## Lancement du site
Une fois le traitement des données terminé, il faut lancer le site. La commande permettant cela se fait à la racine du projet.

```
$ npm run dev
```
Cette commande va lancer le server et le site, qui sera sur [localhost:5173](http://localhost:5173/).
