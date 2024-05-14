from cffi import FFI
ffi = FFI()
ffi.cdef("const char *GetMacAddressString();")
clib = ffi.dlopen("csharp/x64/MacAddress.dll")

s = ffi.string(clib.GetMacAddressString()).decode()
print("s={}".format(s))
