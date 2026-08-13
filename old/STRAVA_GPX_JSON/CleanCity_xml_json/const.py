import os
from enum import IntEnum, auto, Enum


# ********************************************************************************************
#       Configuration du panel (par defaut)
# ********************************************************************************************
DIRECTORY_ORIGIN = os.path.dirname(__file__);
FILE_EQT_PANO_REEL = "\\ExternalFiles\\UNIT_reel.CFG"
FILE_EQT_PANO_BUS = "\\ExternalFiles\\FullNameEquipement\\BUS\\UNIT.CFG"
FILE_EQT_PANO_METRO = "\\ExternalFiles\\FullNameEquipement\\METRO\\UNIT.CFG"
FILE_EQT_PANO_TRAM = "\\ExternalFiles\\FullNameEquipement\\TRAM\\UNIT.CFG"
FILE_TEST_XML = "\\ExternalFiles\\test.xml"
FILE_STRAVA_XML = "\\ExternalFiles\\strava.xml"
FILE_COUNTRY_XML = "\\ExternalFiles\\country.xml"
FILE_OUT_COMPAR_REEL_EQT = "\\ExternalFiles\\Out\\compar_reel_eqt.csv"
FILE_OUT_COMPAR_EQT_REEL = "\\ExternalFiles\\Out\\compar_eqt_reel.csv"
FILE_OUT_STRAVA_JSON = "\\ExternalFiles\\Out\\strava.json"

SEARCH_WORD_IN_XML = "composant;test"

# ********************************************************************************************
#       Configuration du log
# ********************************************************************************************
class LoggerTypeEnum(IntEnum):
    Info = auto()
    Debug = auto()
    Error = auto()

class SectionTypeEnum(Enum):
    API = "API"
    ATVOIP= "ATVOIP"
    CAMERA_ANA = "CAMERA_ANA"
    CAMERA_AXIS = "CAMERA_AXIS"
    EAC_SYS = "EAC_SYS"
    EIV = "EIV"
    SWITCH = "SWITCH"
    UVR60 = "UVR60"  