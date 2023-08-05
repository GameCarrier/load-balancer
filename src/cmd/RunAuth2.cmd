@CD /D %~dp0\..

Taskkill /IM gcs.exe /F /fi "windowtitle eq AUTH2" 2>nul

del LoadBalancer.Server\Bin\Debug\net6.0\log_auth2.log

cd /D "LoadBalancer.Server\Bin\Debug\net6.0"
start "AUTH2"					"gcs.exe" -c 1 -C "config_auth2.json"
exit