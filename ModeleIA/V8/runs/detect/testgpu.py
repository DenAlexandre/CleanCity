import torch
# Ce programme vérifie si un GPU compatible CUDA est disponible sur la machine.
# Il utilise la bibliothèque PyTorch pour effectuer cette vérification.
# Si un GPU compatible CUDA est disponible, il affichera 'True', sinon 'False'.
print(torch.cuda.is_available()) 
