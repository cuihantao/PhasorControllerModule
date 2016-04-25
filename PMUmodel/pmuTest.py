########################
# channon@hawk.iit.edu #
########################

import crc 

################################

def Parse():
    # parse command packets
    i=0

####            ####
# Command Handlers #
####            ####
    
def CMD_off():
    # stop sending messages
    data = off

def CMD_on():
    # begin message transmission
    data = on

def CMD_hdr():
    # send HDR frame
    HDR = createHDR()
    send(HDR)

def CMD_cfg1():
    # send CFG-1 frame
    cfg = createCFG1()
    send(cfg)

def CMD_cfg2():
    # send CFG-2 frame
    cfg = createCFG2()
    send(cfg)

def CMD_cfg3(): # optional command
    # send CFG-3 frame
    cfg = createCFG3()
    send(cfg)

####                ####
# End Command Handlers #
####                ####

    
def send_data():
    data = getData()
    send(data)

#############################33

####
# network stuff
####
    
def send(msg):
    #send trhough UDP
    pass

####
# End Network
####

####
# Synchronization
####
    
def getData():
    # get next datum to send
    pass

####
# End Synchronization
####
    
    
####
# Protocol helpers
####
    
def get_CRC(msg):
    #calculate crc
    return crc.ComputeCRC(msg) #int

#def read_config(config_file):
    #read in file
    
def createHDR():
    pass

def createCFG1():
    with open(cfg_config,'r') as cfg:
        for line in ins:
    
####
# End Protocol helpers
####

class PMU:
    'class to hold pmu info'
    def __init__(self):
        self.version=1
        self.id_code=0
        self.
