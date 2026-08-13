#!/usr/bin/sudo python
import sys
import os
import const
from Controllers.mainController import MainController

def main():
    try:

        mainCtrl = MainController()
        mainCtrl.CreateFileCompar()

    except Exception as exc:
        raise RuntimeError from exc

if __name__ == '__main__':
    sys.exit(main())


