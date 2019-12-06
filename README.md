# image-scanner

![Build status](https://github.com/deepnetworkgmbh/image-scanner/workflows/ci-master/badge.svg)

`image-scanner` can scan a bunch of docker images for CVEs and serve scan results. It can run as web-application or as cli tool.

The scanner can audit images in a given Kubernetes cluster, or receive the list of tags as input parameter.

To scan all Kubernetes cluster images (**TODO:** review the command):

```bash
docker run -v $HOME:/root deepnetwork/image-scanner trivy -e File
```

At the moment, `image-scanner` support only [trivy](https://github.com/aquasecurity/trivy) as scanner and local file-system as persistence layer. Supporting another scanners and storage implementation is part of [the roadmap](./ROADMAP.md).

## Getting Started

`image-scanner` is developed with [.NET Core 3.0](https://dotnet.microsoft.com/download/dotnet-core/3.0).

1. Running From source code

    ```bash
    git clone <https://github.com/deepnetworkgmbh/image-scanner.git>

    cd image-scanner/src

    dotnet run
    ```

2. Running CLI via Docker image

    ```bash
    docker run -v $HOME:/root/ deepnetwork/image-scanner
    ```

3. Running Web application via Docker image

    **TODO:** add example

## Web application

**TODO:** add web app description and examples

## CLI

**TODO:** add cli description and review examples below

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
    -a, --trivyCachePath               Folder path of Trivy cache files

    -c, --containerRegistryAddress     Container Registry Address

    -u, --containerRegistryUserName    Container Registry User Name

    -p, --containerRegistryPassword    Container Registry User Password

    -k, --kubeConfigPath               File path of Kube Config file

    -e, --exporter                     Required. Exporter type (e.g, File)

    -f, --fileExporterPath             Folder path of file exporter

    -m, --parallelismDegree            (Default: 10) Degree of Parallelism

    --help                             Display this help screen.

    --version                          Display version information.
    ```

### Examples

1. Scan with Trivy

    1. Scan a Kubernetes cluster using local kube-config and save outputs into file exporter:

        ```bash
        docker run \
            -v $HOME:/root/ \
            deepnetwork/image-scanner trivy -e File
        ```

        Scan results (json) and container log files are saved under `$HOME/.image-scanner/exports` folder.

    2. Using a cache directory on your machine:

        ```bash
        docker run \
            -v $HOME:/root/ \
            deepnetwork/image-scanner trivy -a [TRIVY_CACHE_PATH] -e File
        ```

        Replace [TRIVY_CACHE_PATH] with the cache directory on your machine.

    3. Running against a Private Container Registry (CR):

        ```bash
        docker run \
            -v $HOME:/root/ \
            deepnetwork/image-scanner trivy -e File -c [CR_NAME] -u [CR_USER] -p [CR_USER_PASSWORD]
        ```

        Set following parameters to scan images from private container registry.

        ```bash
        -c, --containerRegistryAddress     Container Registry Address

        -u, --containerRegistryUserName    Container Registry User Name

        -p, --containerRegistryPassword    Container Registry User Password
        ```

        For example, to scan images from a private Azure Container Registry (ACR), you should provide parameters like this:

        ```bash
        -c [YOUR_ACR_NAME].azurecr.io
        -u [YOUR_ACR_USER_NAME]
        -p [YOUR_ACR_USER_PASSWORD]
        ```

    4. Saving results into a custom folder:

        ```bash
        docker run \
            -v $HOME:/root/ \
            deepnetwork/image-scanner trivy -e File -f $HOME/myfolder
        ```  

        This command runs image-scanner against default Kubernetes cluster and
        saves export files into folder `$HOME/myfolder`

    5. Setting parallelism degree of percentage:

        ```bash
          docker run \
              -v $HOME:/root/ \
              deepnetwork/image-scanner trivy -e File -m 50
        ```

        To run image-scanner in parallel, you can set a parallelism degree in terms of percentage.
        If you have 8 logical CPU in your machine and you select `-m 50`, this means that 4 CPU will be used
        to create multiple scanner instances.

2. Scan with Clair:

    // TO BE ADDED
