@CD /D %~dp0\..

Taskkill /IM gcs.exe /F /fi "windowtitle eq JUMP EUROPE" 2>nul

del LoadBalancer.Server\Bin\Debug\net6.0\log_jumpEurope.log

cd /D "LoadBalancer.Server\Bin\Debug\net6.0"
start "JUMP EUROPE"				"gcs.exe" -c 1 -C "config_jumpEurope.json"
exit