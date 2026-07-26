@echo off
REM Double-click me on Windows.
cd /d "%~dp0"

where py >nul 2>nul && (set PY=py) || (set PY=python)

%PY% -c "import UnityPy, PIL" >nul 2>nul
if errorlevel 1 (
    echo ==^> First run: installing UnityPy and Pillow ^(one time, ~20s^)
    %PY% -m pip install --quiet --upgrade UnityPy Pillow
)

%PY% gambonanza-assets.py %*
echo.
echo GambonanzaAssets has stopped. You can close this window.
pause
