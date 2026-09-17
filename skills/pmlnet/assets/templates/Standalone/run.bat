@echo off
rem ---------------------------------------------------------------------------
rem  Runs the standalone tool with AVEVA E3D's environment already set.
rem
rem  Standalone.Start() needs the same environment variables E3D itself needs -
rem  AVEVA_DESIGN_EXE, PMLLIB, projects_dir, and PATH extended with the
rem  executable folder so the native libraries are found.
rem
rem  Set the password out of band so it never reaches the process list:
rem      set E3D_PASSWORD=...
rem ---------------------------------------------------------------------------

setlocal

set AVEVA_EXE=C:\AVEVA\Plant\Everything3D3.1\

if not exist "%AVEVA_EXE%evars.bat" (
    echo Cannot find "%AVEVA_EXE%evars.bat" - edit AVEVA_EXE in this file.
    exit /b 1
)

call "%AVEVA_EXE%evars.bat" "%AVEVA_EXE%"

if "%E3D_PASSWORD%"=="" (
    echo E3D_PASSWORD is not set.
    exit /b 1
)

"%~dp0bin\Release\MyCompany.E3D.Reports.exe" %*
exit /b %ERRORLEVEL%
