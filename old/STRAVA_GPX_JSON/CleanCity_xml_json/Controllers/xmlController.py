from collections import UserDict
import os
import sys
import time
from xml.dom import minidom
import glob
import os.path

from xml.etree import ElementTree
from lxml import etree
from Models.DataModel import DataModel

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import const


class xmlController:
    def __new__(self, parent):
        self.controler = parent
        print("Creating instance xmlController")
        return super(xmlController, self).__new__(self);

    def __init__(self, parent):
        self.controler = parent
        print("Init is called");



    def get_xml_lxml_in_dico(self, filename):
        try:
            tree = etree.parse(filename)
                        
            # Affiche les attributs des balises :
            #for user in tree.xpath("/gpx/trk/trkseg/trkpt"):
             #   print(user.get("lat"))

            return tree

        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, 'Une erreur est survenue dans la fonction get_xml_in_dico!\n' + str(exc))


    def get_item_reel_in_section(self, tree, section):
        try:

            dataModels = dict()
            list_path = []

            tree_root =  tree.xpath(section)

            for user in tree_root:
                latitude = str(user.get("lat"))
                longitude = str(user.get("lon"))
                _ele = user.xpath("ele")
                elem = str(_ele[0].text)
                _time = user.xpath("time")
                dt = str(_time[0].text)
                
                data = DataModel(dt, elem, latitude, longitude)
                list_path.append(data)
                dataModels[dt] = data
                
            return dataModels

        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, 'Une erreur est survenue dans la fonction get_xml_in_dico!\n' + str(exc))


    def get_item_eqt_in_section(self, tree, section):
        try:

            dataModels = dict()
            list_path = []

            tree_root =  tree.xpath("/UNIT/OBJECT")

            for user in tree_root:

                #<OBJECT CLASS="Folder" ID="EPL" MODULE="General">                
                name_class = str(user.get("CLASS"))
                name_module = str(user.get("MODULE"))
                name_id = str(user.get("ID"))
#<OBJECT CLASS="FullNameEquipement" ID="ALS" MODULE="GTC">
                if name_class == "FullNameEquipement":
                    if name_module == "GTC":
                        list_path.clear()
                        for temp1 in user.findall('PROP'):
                            id_comp = str(temp1.get("ID"))
                            if (id_comp == "listeapifullname"):
                                value = str(temp1.get("VALUE"))
                                data = DataModel(name_id, id_comp, value, const.SectionTypeEnum.API)
                                list_path.append(data)
                            if (id_comp == "listecameraanafullname"):
                                value = str(temp1.get("VALUE"))
                                data = DataModel(name_id, id_comp, value, const.SectionTypeEnum.CAMERA_ANA)
                                list_path.append(data)  

                                """
                                
                            if (id_comp == "listeatvoipfullname"):
                                value = str(temp1.get("VALUE"))
                                data = DataModel(name_id, id_comp, value, const.SectionTypeEnum.ATVOIP)
                                list_path.append(data)    
                            if (id_comp == "listecameraanafullname"):
                                value = str(temp1.get("VALUE"))
                                data = DataModel(name_id, id_comp, value, const.SectionTypeEnum.CAMERA_ANA)
                                list_path.append(data)  
                            if (id_comp == "listecameraaxisfullname"):
                                value = str(temp1.get("VALUE"))
                                data = DataModel(name_id, id_comp, value, const.SectionTypeEnum.CAMERA_AXIS)
                                list_path.append(data)  
                            if (id_comp == "listeeacsysfullname"):
                                value = str(temp1.get("VALUE"))
                                data = DataModel(name_id, id_comp, value, const.SectionTypeEnum.EAC_SYS)
                                list_path.append(data)                                  
                            if (id_comp == "listeeivfullname"):
                                value = str(temp1.get("VALUE"))
                                data = DataModel(name_id, id_comp, value, const.SectionTypeEnum.EIV)
                                list_path.append(data)  
                            if (id_comp == "listeswitchfullname"):
                                value = str(temp1.get("VALUE"))
                                data = DataModel(name_id, id_comp, value, const.SectionTypeEnum.SWITCH)
                                list_path.append(data)           
                            if (id_comp == "listeuvr60fullname"):
                                value = str(temp1.get("VALUE"))
                                data = DataModel(name_id, id_comp, value, const.SectionTypeEnum.UVR60)
                                list_path.append(data)   
                                
                                """
 
                
                if len(list_path) > 0:
                    dataModels[name_id] = list(list_path)

            return dataModels

        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, 'Une erreur est survenue dans la fonction get_xml_in_dico!\n' + str(exc))




    def get_xml_with_minidom_in_dico(self, filename):
        try:
            doc = minidom.parse(filename)
            #elements = doc.getElementsByTagName("item")
            return doc
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, 'Une erreur est survenue dans la fonction get_xml_in_dico!\n' + str(exc))


    def get_xml_file(self, filename):
        try:      
            #os.chdir('/Users/gmull/Documents/Python/Tables')
  
            for filename in glob.iglob(filename, recursive=True):

                    XmlFile = ElementTree.parse(filename)
                    Rec = XmlFile.findall('api21e_e01_composant')

                    for lInfo in Rec:
                        print(lInfo.text)

        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, 'Une erreur est survenue dans la fonction get_xml_in_dico!\n' + str(exc))

