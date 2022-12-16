@echo off

call MC7D2D StartupOptimizer.dll /reference:"%PATH_7D2D_MANAGED%\Assembly-CSharp.dll" ^
  /reference:"libs\AssetsTools.NET.dll" Harmony\*.cs 
echo Successfully compiled StartupOptimizer.dll

REM Library\*.cs Utils\*.cs PipeBlocks\*.cs PipeGridManager\*.cs PlantManager\*.cs && ^

pause