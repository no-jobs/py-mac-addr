from cffi import FFI
import json

ffi = FFI()
ffi.cdef("const char *list_nic_active();")
ffi.cdef("const char *list_nic_installed();")
clib = ffi.dlopen("MacAddress.dll")

s = ffi.string(clib.list_nic_installed()).decode()
print("s={}".format(s))
pyobj = json.loads(s)
print(f"pyobj={pyobj}")
for nic in pyobj:
    print(f'nic.name={nic['name']}')
    print(f'nic.addr={nic['addr']}')

s = ffi.string(clib.list_nic_active()).decode()
print("s={}".format(s))
pyobj = json.loads(s)
print(f"pyobj={pyobj}")
for nic in pyobj:
    print(f'nic.name={nic['name']}')
    print(f'nic.addr={nic['addr']}')
