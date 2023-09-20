# System.Threading.Channels
1. docker build --tag tc:master .
2. docker run  -e DbContextConnectionString="User ID=postgres;Password=pgpassw0rd;Host=172.17.0.3;Port=5432;Database=channels;Connection Lifetime=0;" -p 1030:80 --name threading-channels --cpus=2 --memory=256m tc:master
3. docker exec -it -w //tools threading-channels sh
4. ./dotnet-counters collect --process-id 1 --refresh-interval 3 --counters System.Runtime,Microsoft.AspNetCore.Hosting -o tctrace.csv
5. ./dotnet-trace collect --process-id 1 -o tctrace.nettrace
6. docker cp threading-channels:/tools/tctrace.csv . 
7. docker cp threading-channels:/tools/tctrace.nettrace .

https://www.mytechramblings.com/posts/profiling-a-net-app-with-dotnet-cli-diagnostic-tools/
./dotnet-counters monitor --process-id 1 --refresh-interval 3 --counters System.Runtime,Microsoft.AspNetCore.Hosting
https://www.csvplot.com/