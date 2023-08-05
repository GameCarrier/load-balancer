@CD /D %~dp0\..

Taskkill /IM gcs.exe /F /fi "windowtitle eq LOAD BALANCER" 2>nul

del LoadBalancer.Server\Bin\Debug\net6.0\log_allInOne.log

cd /D "LoadBalancer.Server\Bin\Debug\net6.0"
start "LOAD BALANCER"   "gcs.exe" -c 1 -C "config.json"
exit