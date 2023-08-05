@CD /D %~dp0\..

Taskkill /IM gcs.exe /F /fi "windowtitle eq GAME EUROPE" 2>nul

del LoadBalancer.Server\Bin\Debug\net6.0\log_gameEurope.log

cd /D "LoadBalancer.Server\Bin\Debug\net6.0"
start "GAME EUROPE"				"gcs.exe" -c 1 -C "config_gameEurope.json"
exit