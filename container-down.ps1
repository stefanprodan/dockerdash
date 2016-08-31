$ErrorActionPreference = "Stop"

# stop container if running 
docker stop dockerdash

# remove container
docker rm dockerdash

# remove image
docker rmi -f dockerdash