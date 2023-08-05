@CD /D %~dp0\..

Taskkill /IM gcs.exe /F /fi "windowtitle eq AUTH" 2>nul
Taskkill /IM gcs.exe /F /fi "windowtitle eq AUTH1" 2>nul
Taskkill /IM gcs.exe /F /fi "windowtitle eq AUTH2" 2>nul
Taskkill /IM gcs.exe /F /fi "windowtitle eq JUMP NORTH AMERICA" 2>nul
Taskkill /IM gcs.exe /F /fi "windowtitle eq JUMP EUROPE" 2>nul
Taskkill /IM gcs.exe /F /fi "windowtitle eq GAME NORTH AMERICA" 2>nul
Taskkill /IM gcs.exe /F /fi "windowtitle eq GAME EUROPE" 2>nul

del LoadBalancer.Server\Bin\Debug\net6.0\log_auth.log
del LoadBalancer.Server\Bin\Debug\net6.0\log_auth1.log
del LoadBalancer.Server\Bin\Debug\net6.0\log_auth2.log
del LoadBalancer.Server\Bin\Debug\net6.0\log_jumpNorthAmerica.log
del LoadBalancer.Server\Bin\Debug\net6.0\log_jumpEurope.log
del LoadBalancer.Server\Bin\Debug\net6.0\log_gameNorthAmerica.log
del LoadBalancer.Server\Bin\Debug\net6.0\log_gameEurope.log

cd /D "LoadBalancer.Server\Bin\Debug\net6.0"
start "AUTH"					"gcs.exe" -c 1 -C "config_auth.json"
start "AUTH1"					"gcs.exe" -c 1 -C "config_auth1.json"
start "AUTH2"					"gcs.exe" -c 1 -C "config_auth2.json"
start "JUMP NORTH AMERICA"		"gcs.exe" -c 1 -C "config_jumpNorthAmerica.json"
start "JUMP EUROPE"				"gcs.exe" -c 1 -C "config_jumpEurope.json"
start "GAME NORTH AMERICA"		"gcs.exe" -c 1 -C "config_gameNorthAmerica.json"
start "GAME EUROPE"				"gcs.exe" -c 1 -C "config_gameEurope.json"
exit