@CD /D %~dp0\..

Taskkill /IM gcs.exe /F /fi "windowtitle eq AUTH1" 2>nul

del LoadBalancer.Server\Bin\Debug\net6.0\log_auth1.log

cd /D "LoadBalancer.Server\Bin\Debug\net6.0"
start "AUTH1"					"gcs.exe" -c 1 -C "config_auth1.json"
exit