@CD /D %~dp0\..

SET BUILD_TYPE=Release
set PLATFORM=net472
dotnet build /p:Configuration=%BUILD_TYPE% /p:Platform="Any CPU" || @goto error

SET InstallationDir=C:\Program Files (x86)\Game Carrier\
SET ClientDir=LoadBalancer.Client\bin\%BUILD_TYPE%\%PLATFORM%\
SET TargetDir=..\UnityFPS\Assets\Scripts\libs\
mkdir "%TargetDir%"

@echo Copy Managed Libraries
copy /Y "%InstallationDir%\Client\gcclient.dll"				"%TargetDir%"
copy /Y "%ClientDir%\GC.Common.dll"							"%TargetDir%"
copy /Y "%ClientDir%\GC.Clients.dll"						"%TargetDir%"
copy /Y "%ClientDir%\LoadBalancer.dll"						"%TargetDir%"
copy /Y "%ClientDir%\LoadBalancer.Client.dll"               "%TargetDir%"
copy /Y "%ClientDir%\LoadBalancer.Client.pdb"               "%TargetDir%"

pause
exit

:error
echo Script failed
pause