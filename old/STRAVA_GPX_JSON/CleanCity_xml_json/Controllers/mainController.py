import json
import os
import sys
import time

from PyQt6.QtCore import QTimer, QThreadPool, QMutex, pyqtSignal, QObject
import Controllers.FileController as FileController
import Controllers.LoggerController as LoggerController
import Controllers.xmlController as xmlController

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import const

class MainController:

    def __new__(self):
        print("Creating instance MainController")
        return super(MainController, self).__new__(self)

    def __init__(self):
        try:

            self.mutexLogger = QMutex()
            self.mutexJson = QMutex()
            self.mutexReadFile = QMutex()
            
            self.loggerCtrl = LoggerController.LoggerController(self)
            self.xmlCtrl = xmlController.xmlController(self)
            self.FileCtrl = FileController.FileController(self)

            print("Exiting Init")
        except Exception as exc:
            self.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, "MainController - __init__ : " + str(exc))


    def CreateFileCompar(self):
        try:

            tree_strava = self.xmlCtrl.get_xml_lxml_in_dico(const.DIRECTORY_ORIGIN + const.FILE_STRAVA_XML)
            list_reel = self.xmlCtrl.get_item_reel_in_section(tree_strava,"/gpx/trk/trkseg/trkpt")


            self.CreateFileRealEqt(list_reel)



        except Exception as exc:
            self.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, "MainController - CreateFileCompar : " + str(exc))



    def CreateFileRealEqt(self, list_reel):
        try:
            strfile = "{"+ "\n"
            strfile = strfile + '   "client": "StravaGPX Android",'+ "\n"
            strfile = strfile + '	"trk": {'+ "\n"
            strfile = strfile + '       "name": "Tournée du 15/11 Cleancity",'+ "\n"
            strfile = strfile + '       "type": "cycling",'+ "\n"	
            strfile = strfile + '       "trkseg": {'+ "\n"
            strfile = strfile + '           "trkpt": ['+ "\n"         
            
            #Detection des entites absentes dans le fichier reel
            for key, data in list_reel.items():
                print("key:" + key)
                strfile = strfile + '				{'+ "\n"    
                strfile = strfile + '				"lat": "' + data.lat + '",'+ "\n"    
                strfile = strfile + '				"lon": "' + data.lon + '",'+ "\n"    
                strfile = strfile + '				"ele": "' + data.element + '",'+ "\n"    
                strfile = strfile + '				"time": "' + data.dt + '"'+ "\n"    
                strfile = strfile + '				},'+ "\n" 
                
                
            
            strfile = strfile + '			]'+ "\n"      
            strfile = strfile + '		}'+ "\n" 
            strfile = strfile + '	}'+ "\n" 
            strfile = strfile + '}'+ "\n" 
            
            
            self.FileCtrl.WriteFileNewText(const.DIRECTORY_ORIGIN + const.FILE_OUT_STRAVA_JSON, strfile)

        except Exception as exc:
            self.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, "MainController - CreateFileReelEqt : " + str(exc))



    def FindValueReal(self, data_key_eqt, data_model_real):
        try:
            list_eqt = []
            for data_eqt in data_key_eqt:
                if data_model_real.section == data_eqt.section:
                    tab_eqt = data_eqt.value.split(';')
                    bln_found = False
                    for data_tab_eqt in tab_eqt:
                        if data_tab_eqt == "/GTC_CLS/DFH/Instances/CameraANA/ANA_01":
                            pass
                        if data_model_real.value == data_tab_eqt:
                            bln_found =  True
                            data_model_real.find = bln_found
                            return bln_found

            return bln_found

        except Exception as exc:
            self.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, "MainController - CreateFileCompar : " + str(exc))




    def CreateFileEqtReal(self, list_reel, list_bus, list_metro, list_tram):
        try:
            strfile = ""

            #Detection des entites absentes dans le fichier eqt metro
            for key, data in list_metro.items():
                print("key:" + key)
                for data_model in data:
                    #valeure recherchée
                    str = "key: " + key + " - value : " + data_model.value
                    print (str)
                    if key in list_reel:
                        list_eqt_not_found = self.FindValueEqt(list_reel[key], data_model)
                        
                        for eqt in list_eqt_not_found:
                            strfile = strfile + key + ";" + data_model.section.value + ";" + eqt + "\n"
                          

            self.FileCtrl.WriteFileNewText(const.DIRECTORY_ORIGIN + const.FILE_OUT_COMPAR_EQT_REEL, strfile)

        except Exception as exc:
            self.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, "MainController - CreateFileEqtReal : " + str(exc))




    def FindValueEqt(self, eqt_reel, data_model_eqt):
        try:
            list_eqt = []
            tab_eqt = data_model_eqt.value.split(';')
            for data_eqt_tab in tab_eqt:
                bln_found = False
                for data_reel in eqt_reel:
                    if data_model_eqt.section == data_reel.section:
                        if data_eqt_tab == data_reel.value:
                            bln_found = True
                            break;

                if (bln_found == False):
                    list_eqt.append(data_eqt_tab)

            return list_eqt

        except Exception as exc:
            self.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, "MainController - CreateFileCompar : " + str(exc))





