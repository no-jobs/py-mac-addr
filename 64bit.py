from cffi import FFI
import json

ffi = FFI()
ffi.cdef("const char *GetMacAddressString();")
clib = ffi.dlopen("csharp/x64/MacAddress.dll")

s = ffi.string(clib.GetMacAddressString()).decode()
print("s={}".format(s))

pyobj = json.loads(s)
print(f"pyobj={pyobj}")

for nic in pyobj:
    print(f'nic.name={nic['name']}')
    print(f'nic.addr={nic['addr']}')
