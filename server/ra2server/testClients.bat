@echo off
echo RA2 多人游戏测试脚本
echo =====================

:menu
echo 请选择要测试的房间类型:
echo 1. SOLO (1人)
echo 2. DUO (2人)
echo 3. TRIO (3人)
echo 4. QUAD (4人)
echo 5. OCTO (8人)
echo 6. 退出
echo.

set /p choice=请输入选项 (1-6): 

if "%choice%"=="1" goto solo
if "%choice%"=="2" goto duo
if "%choice%"=="3" goto trio
if "%choice%"=="4" goto quad
if "%choice%"=="5" goto octo
if "%choice%"=="6" goto exit

echo 无效选项，请重新选择
echo.
goto menu

:solo
echo 启动单人测试...
start "Player1" cmd /k "cd %~dp0 && ..\gradlew.bat runClient1"
goto menu

:duo
echo 启动双人测试...
start "Player1" cmd /k "cd %~dp0 && ..\gradlew.bat runClient1"
start "Player2" cmd /k "cd %~dp0 && ..\gradlew.bat runClient2"
goto menu

:trio
echo 启动三人测试...
start "Player1" cmd /k "cd %~dp0 && ..\gradlew.bat runClient1"
start "Player2" cmd /k "cd %~dp0 && ..\gradlew.bat runClient2"
start "Player3" cmd /k "cd %~dp0 && ..\gradlew.bat runClient3"
goto menu

:quad
echo 启动四人测试...
start "Player1" cmd /k "cd %~dp0 && ..\gradlew.bat runClient1"
start "Player2" cmd /k "cd %~dp0 && ..\gradlew.bat runClient2"
start "Player3" cmd /k "cd %~dp0 && ..\gradlew.bat runClient3"
start "Player4" cmd /k "cd %~dp0 && ..\gradlew.bat runClient4"
goto menu

:octo
echo 启动八人测试...
start "Player1" cmd /k "cd %~dp0 && ..\gradlew.bat runClient1"
start "Player2" cmd /k "cd %~dp0 && ..\gradlew.bat runClient2"
start "Player3" cmd /k "cd %~dp0 && ..\gradlew.bat runClient3"
start "Player4" cmd /k "cd %~dp0 && ..\gradlew.bat runClient4"
start "Player5" cmd /k "cd %~dp0 && ..\gradlew.bat runClient5"
start "Player6" cmd /k "cd %~dp0 && ..\gradlew.bat runClient6"
start "Player7" cmd /k "cd %~dp0 && ..\gradlew.bat runClient7"
start "Player8" cmd /k "cd %~dp0 && ..\gradlew.bat runClient8"
goto menu

:exit
exit /b