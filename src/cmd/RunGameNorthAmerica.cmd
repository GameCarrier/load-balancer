@CD /D %~dp0\..

Taskkill /IM gcs.exe /F /fi "windowtitle eq GAME NORTH AMERICA" 2>nul

del LoadBalancer.Server\Bin\Debug\net6.0\log_gameNorthAmerica.log

cd /D "LoadBalancer.Server\Bin\Debug\net6.0"
start "GAME NORTH AMERICA"		"gcs.exe" -c 1 -C "config_gameNorthAmerica.json"
exit