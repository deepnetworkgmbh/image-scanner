#!/usr/bin/env bash

while getopts a:i:u: option 
do
    case "${option}" in
        a) APP=${OPTARG};;
        i) IMAGE_NAME=${OPTARG};;
    esac
done

# exit on any errors (except conditional checks of executed commands)
set -e

# build kube-scanner docker image
docker build -t ${IMAGE_NAME} -f dockerfiles/${APP} .

# push kube-scanner docker image into registry
docker push ${IMAGE_NAME}