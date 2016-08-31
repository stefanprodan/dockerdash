# dockerdash

Docker dashboard built with ASP.NET Core, Docker.DotNet, SignalR and Vuejs.

Compatible with Docker v1.12, requires docker socket to be mounted as a volume.

### Run

```
docker run -d -p 5050:5050 -v /var/run/docker.sock:/var/run/docker.sock --name dockerdash stefanprodan/dockerdash
```
