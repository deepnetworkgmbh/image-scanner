#!/usr/bin/env bash

while getopts i:u: option 
do
    case "${option}" in
        i) IMAGE_NAME=${OPTARG};;
        u) TRIVY_URL=${OPTARG};;
    esac
done

# exit on any errors (except conditional checks of executed commands)
set -e

# download and extract Trivy executable
wget -qO- ${TRIVY_URL} | tar xvzf -

# build kube-scanner docker image
docker build -t ${IMAGE_NAME} .

# push kube-scanner docker image into registry
docker push ${IMAGE_NAME} 