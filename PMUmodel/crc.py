import ctypes

_crc = ctypes.CDLL('./libcrc.so')
_crc.ComputeCRC.argtypes = (ctypes.POINTER(ctypes.c_char), ctypes.c_char)

def ComputeCRC(msg):
    global _crc
    msg_size = len(msg)
    msg_i = bytes(msg.encode())
    result = _crc.ComputeCRC(ctypes.c_char_p(msg_i) ,ctypes.c_char(msg_size))
    return int(result)
