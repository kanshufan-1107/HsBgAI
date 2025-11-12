@echo off
setlocal

:: 创建目录
if not exist "\BepInExPlugins" mkdir "\BepInExPlugins"

:: 将生成后的 dll 文件复制到该目录
copy /y "%~dp0bin\Debug\net48\HsBgAI.dll" "\BepInExPlugins"
copy /y "%~dp0bin\Debug\net48\HsBgAI.dll" "D:\Program Files\Hearthstone\BepInEx\plugins"

endlocal