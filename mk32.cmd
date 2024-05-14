call py32.cmd -m pip install cffi PyInstaller
call py32.cmd -m PyInstaller -n 32bit --add-binary "csharp/x86/MacAddress.dll;." anycpu.py
