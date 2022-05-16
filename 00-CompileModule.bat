@echo off

call MC7D2D DevDiceRoll.dll /reference:"%PATH_7D2D_MANAGED%\Assembly-CSharp.dll" Harmony\*.cs Library\*.cs && ^
echo Successfully compiled DevDiceRoll.dll

pause