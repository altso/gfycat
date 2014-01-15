@echo off
cls

rem build
@echo Building solution...
%SYSTEMROOT%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe Gfycat.sln /p:Configuration=Release /t:Rebuild


rem init
@echo Copying output files...
cd nuget
rmdir /s /q lib

rem portable-net45+sl50+wp80+win
mkdir lib\portable-net45+sl50+wp80+win
cd lib\portable-net45+sl50+wp80+win
copy ..\..\..\Gfycat\bin\Release\Gfycat.dll
cd ..\..

rem windowsphone8
mkdir lib\windowsphone8
cd lib\windowsphone8
copy ..\..\..\Gfycat\bin\Release\Gfycat.dll
copy ..\..\..\Gfycat.WindowsPhone\bin\Release\Gfycat.WindowsPhone.dll
cd ..\..\..

rem generate package
@echo Generating package
.nuget\nuget pack nuget\Gfycat.nuspec