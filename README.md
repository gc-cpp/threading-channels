# System.Threading.Channels
1. docker build --tag tc:master .
2. docker run -e COMPlus_PerfMapEnabled=1 -e COMPlus_EnableEventLog=1 -e COMPlus_ZapDisable=1 -e DbContextConnectionString="User ID=postgres;Password=;Host=172.17.0.4;Port=5432;Database=channels;Connection Lifetime=0;" -p 1012:80 --name threading-channels --cpus=2 --memory=256m tc:master

./perfcollect collect tc -gcwithheap -threadtime

https://github.com/mjrousos/ContainerProfiling