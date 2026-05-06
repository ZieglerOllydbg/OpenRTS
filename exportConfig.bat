@echo on
@SET EXCEL_FOLDER=.\excel
@SET JSON_FOLDER=.\json
@SET C_FILE=.\cs
@SET EXE=.\excel2json\excel2json.exe

@ECHO Converting excel files in folder %EXCEL_FOLDER% ...
for /f "delims=" %%i in ('dir /b /a-d /s %EXCEL_FOLDER%\*.xlsx') do (
    @echo processing %%~nxi
	@CALL %EXE% --excel %EXCEL_FOLDER%\%%~nxi --json %JSON_FOLDER%\%%~ni.json -p %C_FILE%\%%~ni.cs --header 3 --encoding utf8-nobom --exclude_prefix #
)
pause