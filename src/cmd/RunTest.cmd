@CD /D %~dp0\..

Taskkill /IM gcs.exe /F /fi "windowtitle eq TEST" 2>nul

del LoadBalancer.Server\Bin\Debug\net6.0\log_test.log

cd /D "LoadBalancer.Server\Bin\Debug\net6.0"
start "TEST"					"gcs.exe" -c 1 -C "config_test.json"
exit