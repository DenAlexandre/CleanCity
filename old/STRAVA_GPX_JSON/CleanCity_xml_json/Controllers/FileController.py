import os
import sys
import zipfile
from os.path import exists, isfile, join

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import const

class FileController:
    def __new__(self, parent):
        self.controler = parent
        print("Creating instance FileController")
        return super(FileController, self).__new__(self)

    def __init__(self, parent):
        self.controler = parent
        print("Init is called")

    def WriteFileByte(self, filetext, datatext):
        try:
            self.controler.mutexReadFile.lock()
            with open(filetext, 'wb') as handler:
                handler.write(datatext)

        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error,
            'Une erreur est survenue dans la fonction ReadFile!\n' + exc)
        finally:
            self.controler.mutexReadFile.unlock()

    def WriteFileAddText(self, filetext, datatext):
        try:
            self.controler.mutexReadFile.lock()
            with open(filetext, 'a') as handler:
                handler.write(str(datatext) + "\n")

        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error,
            'Une erreur est survenue dans la fonction ReadFile!\n' + exc)
        finally:
            self.controler.mutexReadFile.unlock()


    def WriteFileNewText(self, filetext, datatext):
        try:
            self.controler.mutexReadFile.lock()
            with open(filetext, 'w') as handler:
                handler.write(str(datatext) + "\n")

        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error,
            'Une erreur est survenue dans la fonction ReadFile!\n' + exc)
        finally:
            self.controler.mutexReadFile.unlock()



    def RemoveFile(self, file):
        try:
            self.controler.mutexReadFile.lock()
            if (self.file_exist(file)):
                os.remove(file)
            self.controler.mutexReadFile.unlock()
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error,
            'Une erreur est survenue dans la fonction RemoveFile!\n' + exc)


    def ReadFile(self, file):
        try:
            self.controler.mutexReadFile.lock()
            with open(file, 'r') as f:
                fread = f.read()
                readline = fread.splitlines()

            self.controler.mutexReadFile.unlock()
            return readline
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error,
            'Une erreur est survenue dans la fonction ReadFile!\n' + exc)

    def file_exist(self, pathfile):
        try:
            return exists(pathfile)
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error,
            'Une erreur est survenue dans la fonction file_exist!\n' + exc)
    def get_files(self, pathrep, nbr_files):
        try:
            files_order = os.listdir(pathrep)
            files = []
            compteur = 0
            for f in sorted(files_order, reverse=True):
                compteur += 1
                if (compteur <= nbr_files):
                    strfile = join(pathrep, f)
                    if isfile(strfile):
                        files.append(strfile)
            return files
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error,
            'Une erreur est survenue dans la fonction get_files!\n' + exc)
    def concat_files(self, files):
        try:
            concat = ''.join([open(f).read() for f in files])
            return concat
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error,
            'Une erreur est survenue dans la fonction concat_files!\n' + exc)


    def compress_file(self, zipfilepath, textFile):
        try:
            zip = zipfile.ZipFile(zipfilepath, 'w')
            zip.write(textFile)
            zip.close
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error,
            'Une erreur est survenue dans la fonction compress_file!\n' + exc)

    def ReadRegulationFiles(self):
        try:

            self.controler.data_temp = self.ReadFile(const.FILE_COURBE_VENTIL)
            self.controler.data_lumi = self.ReadFile(const.FILE_COURBE_LUMI_ST_CREE)

        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error,
            'Une erreur est survenue dans la fonction ReadRegulationFiles!\n' + exc)

