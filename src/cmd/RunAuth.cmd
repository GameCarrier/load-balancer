@CD /D %~dp0\..

Taskkill /IM gcs.exe /F /fi "windowtitle eq AUTH" 2>nul

del LoadBalancer.Server\Bin\Debug\net6.0\log_auth.log

cd /D "LoadBalancer.Server\Bin\Debug\net6.0"
start "AUTH"					"gcs.exe" -c 1 -C "config_auth.json"
exit