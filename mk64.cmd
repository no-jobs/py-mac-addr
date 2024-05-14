call py64.cmd -m pip install cffi PyInstaller
call py64.cmd -m PyInstaller -n 64bit --add-binary "csharp/x64/MacAddress.dll;." anycpu.py
