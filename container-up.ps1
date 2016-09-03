$ErrorActionPreference = "Stop"

# build image
if(docker images -q dockerdash){
    "using existing dockerdash image" 
}else{
    docker build -t dockerdash -f Dockerfile.debug .
}

# run container
docker run --name dockerdash -d -p 5050:5050 -v /var/run/docker.sock:/var/run/docker.sock -t dockerdash