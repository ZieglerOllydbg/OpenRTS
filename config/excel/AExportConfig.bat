@echo off
@SET EXCEL_FOLDER=.\
@SET JSON_FOLDER=..\..\client\Assets\Resources\Data\Json
@SET C_FILE=..\..\client\Packages\ZLockstep\Runtime\Configs
@SET EXE=..\excel2json\excel2json.exe

@ECHO Converting excel files in folder %EXCEL_FOLDER% ...
for /f "delims=" %%i in ('dir /b /a-d /s %EXCEL_FOLDER%\*.xlsx') do (
    @echo processing %%~nxi
	@CALL %EXE% --excel %EXCEL_FOLDER%\%%~nxi --json %JSON_FOLDER%\%%~ni.json -p %C_FILE%\%%~ni.cs -s --header 3 --encoding utf8-nobom --exclude_prefix #
)
pause