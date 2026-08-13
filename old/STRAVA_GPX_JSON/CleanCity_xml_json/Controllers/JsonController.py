import json
import os
import sys
import time

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import const


class JsonController:
    def __new__(self, parent):
        self.controler = parent
        print("Creating instance JsonController")
        return super(JsonController, self).__new__(self);

    def __init__(self, parent):
        self.controler = parent
        print("Init is called");

    def set_json_in_file(self, datajson, filename):
        try:
            self.controler.mutexJson.lock()
            # writing JSON file
            with open(filename, 'w', encoding='utf8') as fd:
                json.dump(datajson, fd, ensure_ascii=False, indent=4)


        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, 'Une erreur est survenue dans la fonction set_json_in_file!\n' + exc)
        finally:
            self.controler.mutexJson.unlock()


    def get_json_in_dico(self, filename):
        try:
            self.controler.mutexJson.lock()
            # Opening JSON file
            with open(filename, 'r') as read_file:
                datafile = json.load(read_file);
                 # Pretty Printing JSON string back
                #datadumps = json.dumps(datafile, indent = 4, sort_keys=True);
                #print(datadumps);

                return datafile
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, 'Une erreur est survenue dans la fonction get_json_in_dico!\n' + str(exc))
        finally:
            self.controler.mutexJson.unlock()



    def get_json_dumps_in_dico(self, filename):
        try:
            self.controler.mutexJson.lock()
            # Opening JSON file
            with open(filename, 'r') as read_file:
                datafile = json.load(read_file)
                 # Pretty Printing JSON string back
                datadumps = json.dumps(datafile)
                #print(datadumps);

                return datadumps
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, 'Une erreur est survenue dans la fonction get_json_dumps_in_dico!\n' + str(exc))
            return
        finally:
            self.controler.mutexJson.unlock()

    def save_token_config(self, text, filename):
        try:

            with open(filename, 'r') as read_file:
                datafile = json.load(read_file);
                datafile['token'] = text
                self.set_json_in_file(datafile, filename)
                self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Info, 'Enregistrement du token dans le fichier de configuration appli ok')
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, 'Une erreur est survenue dans la fonction save_token_config!\n' + exc)


    def save_refreshtoken_config(self, text, filename):
        try:

            with open(filename, 'r') as read_file:
                datafile = json.load(read_file)
                datafile['refreshToken'] = text
                self.set_json_in_file(datafile, filename)
                self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Info, 'Enregistrement du refreshToken dans le fichier de configuration appli ok')
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error, 'Une erreur est survenue dans la fonction save_refreshtoken_config!\n' + exc)



    #********************************************************************************
    # Permet de remplacer les données issue de la requête dans le fichier local
    #********************************************************************************
    def ReplaceInJsonFile(self, data_requete, data_local_file):
        try:
            for value1 in data_requete:
                if isinstance(data_local_file, dict):
                    self.SearchDico(data_local_file, data_requete, value1)
                elif isinstance(data_local_file, list):
                    self.SearchList(data_local_file, data_requete, value1)
                else:
                    print(value1)
                    pass
                #pour chaque mot clef trouvé, on modifie le fichier
                print (value1, self.response);
                data_local_file[value1] = data_requete[value1]

            #ecriture du fichier avec les nouvelles valeures
            self.set_json_in_file(data_local_file, const.FILE_SYNCHRONIZE_JSON);
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Info, 'Ecriture du fichier Json ok')
            return True
        except Exception as exc:
            self.controler.loggerCtrl.WriteLogger(const.LoggerTypeEnum.Error,
                                              'Une erreur est survenue dans la fonction ReplaceInJsonFile!\n' + exc)







    def SearchList(self, data_local_file, data_requete, keyWord):
        for value1 in data_local_file:
            if (value1 == keyWord) :
                self.response = data_requete[value1]
                data_local_file[value1] = data_requete[value1]
                break;



    def SearchDico(self, data_local_file, data_requete, keyWord):
        for key1, value1 in data_local_file.items():
            if (key1 == keyWord) :
                self.response = data_requete[key1]
                data_local_file[key1] = data_requete[key1]
                break;


