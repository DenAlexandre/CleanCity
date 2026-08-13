import datetime
import sys
import os
import logging
import time
from logging.handlers import RotatingFileHandler

sys.path.append(os.path.join(os.path.dirname(__file__), '..'))
import const


class LoggerController:
    def __new__(self, parent): 
        self.controler = parent
        print("Creating instance LoggerController")
        return super(LoggerController, self).__new__(self)
      
    def __init__(self, parent): 
        self.controler = parent
        self.dt_file = datetime.date.today()
        self.Create_file_handler(self.dt_file)

    def WriteLogger(self, typeLog :const.LoggerTypeEnum, txt):
        try:

            self.controler.mutexLogger.lock()
            dt = datetime.datetime.today().replace(microsecond=0)
            if self.dt_file.day != dt.day:
                self.dt_file = datetime.date.today()
                self.Create_file_handler(self.dt_file)

            if typeLog == const.LoggerTypeEnum.Info:
                self.logger.info(txt)
            elif typeLog == const.LoggerTypeEnum.Debug:
                self.logger.debug(txt)
            elif typeLog == const.LoggerTypeEnum.Error:
                self.logger.error(txt)
            else:
                self.logger.error(txt)
            time.sleep(0.01)


        except Exception as exc:
            raise exc
        finally:
            self.controler.mutexLogger.unlock()

    def Create_file_handler(self, dt_file):

        self.logger = logging.getLogger()
        self.logger.handlers.clear()
        self.logger.setLevel(logging.DEBUG)
        self.formatter = logging.Formatter('%(asctime)s :: %(levelname)s :: %(message)s')

        file_handler = RotatingFileHandler(const.DIRECTORY_ORIGIN + '\\Log\\Error\\logfile_' + str(dt_file) + '.log', 'a', 1000000, 1)
        # on lui met le niveau sur DEBUG, on lui dit qu'il doit utiliser le formateur
        # créé précédement et on ajoute ce handler au loggerCtrl
        file_handler.setLevel(logging.DEBUG)
        file_handler.setFormatter(self.formatter)
        self.logger.addHandler(file_handler)

        steam_handler = logging.StreamHandler()
        steam_handler.setLevel(logging.DEBUG)
        self.logger.addHandler(steam_handler)

