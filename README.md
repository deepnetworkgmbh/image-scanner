# image-scanner

![Build status](https://github.com/deepnetworkgmbh/image-scanner/workflows/ci-master/badge.svg)
[![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=deepnetworkgmbh/image-scanner)](https://dependabot.com)

`image-scanner` can scan a bunch of docker images for CVEs and serve scan results. It can run as web-application or as cli tool.

The scanner can audit images in a given Kubernetes cluster, or receive the list of tags as input parameter.

To scan all Kubernetes cluster images:

- default kube-config:

    ```bash
    docker run --rm -v $HOME:/root deepnetwork/image-scanner-cli trivy -e File -i File
    ```

- a custom k8s cluster: 
    ```bash
    docker run --rm -v $HOME:/root deepnetwork/image-scanner-cli trivy -e File -i File -k /root/.kube/custom_k8s_config
    ```

To scan a list of images:

A sample image list can be found [here](samples/sample-image-list)

```bash
docker run --rm -v $HOME:/root deepnetwork/image-scanner-cli trivy -e File -i File -l /root/Repos/image-scanner/samples/sample-image-list 
```

At the moment, `image-scanner` support only [trivy](https://github.com/aquasecurity/trivy) as scanner and local file-system as persistence layer. Supporting another scanners and storage implementation is part of [the roadmap](./ROADMAP.md).

## Getting Started

`image-scanner` is developed with [.NET Core 3.0](https://dotnet.microsoft.com/download/dotnet-core/3.0).

1. Running From source code

    ```bash
    git clone https://github.com/deepnetworkgmbh/image-scanner.git

    cd image-scanner/src/cli

    dotnet run
    ```

2. Running CLI via Docker image

    ```bash
    docker run --rm -v $HOME:/root deepnetwork/image-scanner-cli trivy -e File -i File
    ```

3. Running Web application via Docker image

    ```bash
    docker run --rm \
            -p 8080:8080 \
            -v $HOME:/$HOME deepnetwork/image-scanner-web \
            -e IMAGE_SCANNER_CONFIG_FILE_PATH="$HOME/Repos/image-scanner/src/tests/image-scanner.config-sample.yaml"
    ```
    Then, navigate to http://localhost:8080/swagger/index.html address on your machine and run APIs to start scans.

## Web application

**TODO:** add web app description and examples

## CLI

   The image-scanner CLI tool helps to scan a k8s cluster or a list of images. Following CLI options are used in scanning operations.

### Options

1. Select a scanner:

    ```bash
    trivy      Run trivy scanner

    clair      Run clair scanner // not supported yet

    help       Display more information on a specific command.

    version    Display version information.
    ```

    `image-scanner` needs a scanner to be selected. At the moment, only **Trivy** scanner is supported.

2. Trivy options:

    ```bash
    -t, --trivyBinaryPath              Binary path of Trivy executable (Default: /usr/local/bin/trivy) 

    -a, --trivyCachePath               Folder path of Trivy cache files
    
    -r, --registries                   The path of Container Registry Credentials file    

    -k, --kubeConfigPath               File path of Kube Config file

    -e, --exporter                     Required. Exporter type (e.g, File)
    
    -i, --importer                     Required. Importer type (e.g, File)    

    -f, --fileExporterPath             Folder path of file exporter
    
    -b, --isBulkUpload                 Is bulk upload (Default: false)    

    -m, --parallelismDegree            (Default: 10) Degree of Parallelism
    
    -l, --listOfImagesPath             The path of images list file
    
    --help                             Display this help screen.

    --version                          Display version information.
    ```

### Examples

1. Scan with Trivy

    1. Scan a Kubernetes cluster using local kube-config and save outputs into file exporter:

        ```bash
        docker run --rm\
            -v $HOME:/root/ \
            deepnetwork/image-scanner-cli trivy -e File -i File
        ```

        Scan results (json) and container log files are saved under `$HOME/.image-scanner/exports` folder.

    2. Using a cache directory on your machine:

        ```bash
        docker run --rm\
            -v $HOME:/root/ \
            deepnetwork/image-scanner-cli trivy -a [TRIVY_CACHE_PATH] -e File
        ```

        Replace [TRIVY_CACHE_PATH] with the cache directory on your machine.

    3. Running against a Private Container Registry (CR):
    
        Prepare your private CR list in a format like in this [file](samples/registries.config-sample.yaml)

        ```bash
        docker run --rm\
            -v $HOME:/root/ \
            deepnetwork/image-scanner-cli trivy -e File -i File -r /root/Repos/image-scanner/samples/registries.config-sample.yaml
        ```

    4. Saving results into a custom folder:

        ```bash
        docker run --rm \
            -v $HOME:/root/ \
            deepnetwork/image-scanner-cli trivy -e File -i File -f /root/myfolder
        ```  

        This command runs image-scanner against default Kubernetes cluster and
        saves export files into folder `/root/myfolder`

    5. Setting parallelism degree of percentage:

        ```bash
          docker run --rm\
              -v $HOME:/root/ \
              deepnetwork/image-scanner-cli trivy -e File -i File -m 50
        ```

         The maximum parallelism degree means that the number of the scanner (e.g.Trivy) processes to be run in parallel. The default value is 10.

2. Scan with Clair:

    // TO BE ADDED
