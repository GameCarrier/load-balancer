@CD /D %~dp0\..

Taskkill /IM gcs.exe /F /fi "windowtitle eq JUMP NORTH AMERICA" 2>nul

del LoadBalancer.Server\Bin\Debug\net6.0\log_jumpNorthAmerica.log

cd /D "LoadBalancer.Server\Bin\Debug\net6.0"
start "JUMP NORTH AMERICA"		"gcs.exe" -c 1 -C "config_jumpNorthAmerica.json"
exit