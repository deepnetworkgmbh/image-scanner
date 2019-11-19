#!/usr/bin/env bash

while getopts a:i:u: option 
do
    case "${option}" in
        a) APP=${OPTARG};;
        i) IMAGE_NAME=${OPTARG};;
        u) TRIVY_URL=${OPTARG};;
    esac
done

# exit on any errors (except conditional checks of executed commands)
set -e

# download and extract Trivy executable
wget -qO- ${TRIVY_URL} | tar xvzf -

# build kube-scanner docker image
docker build -t ${IMAGE_NAME} -f dockerfiles/${APP} .

# push kube-scanner docker image into registry
docker push ${IMAGE_NAME}