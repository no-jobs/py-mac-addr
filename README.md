# py-mac-addr

## クローン

```
cd /d C:\
git clone https://github.com/no-jobs/py-mac-addr.git
cd py-mac-addr
```

## py32.cmd、py64.cmd 編集

py32.cmd と py64.cmd を python 32bit/64bit がインストールされている環境(パス)に合わせて編集します。

## テスト実行

32bit.cmd
64bit.cmd

をそれぞれ実行します。

ロードするDLLのパスを
32bit.py
64bit.py
では書き分けています。

# EXE作成

mk32.cmd
mk64.cmd

で EXE ができます。

ただし、PyInstaller のオプションに「-F」をつけてないので完全なOne Exe にはなってません。
おこのみでPyInstaller のオプションに「-F」をつけてください。
