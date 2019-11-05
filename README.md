# kube-scanner

kube-scanner is a command-line tool that helps you scan all images in a Kubernetes cluster.

Scanning all cluster images is as easy as, running the following command:

```bash
    docker run -v $HOME:/root -v /var/run/docker.sock:/var/run/docker.sock \
    deepnetwork/kube-scanner trivy -e File
```

Basically, it retrieves all unique images from the Kubernetes cluster, scans them with the selected scanner and saves the results into the selected exporter.

Since it supports multiple image scanners (e.g. Trivy) and exporters (e.g. File) by design, 
you can select which scanner and exporter to be used along with other options.

Thanks to parallelism support, you can run it concurrently and accelerate the execution.

`ATTENTION:` With the first version, only **Trivy** scanner and **File** exporter is supported. It's planned to add additional
scanners and exporters in future versions.

# Options

1. Select a scanner:
    ```bash
      trivy      Run trivy scanner
    
      clair      Run clair scanner // not supported yet
    
      help       Display more information on a specific command.
    
      version    Display version information.
    
    ```

    `kube-scanner` needs a scanner to be selected. At the moment, only **Trivy** scanner is supported. 

2. Trivy options:
    ```bash
      -a, --trivyCachePath               Folder path of Trivy cache files
    
      -c, --containerRegistryAddress     Container Registry Address
    
      -u, --containerRegistryUserName    Container Registry User Name
    
      -p, --containerRegistryPassword    Container Registry User Password
    
      -k, --kubeConfigPath               File path of Kube Config file
    
      -e, --exporter                     Required. Exporter type (e.g, File)
    
      -f, --fileExporterPath             Folder path of file exporter
    
      -b, --isBulkUpload                 (Default: false) Is bulk upload
    
      -m, --maxParallelismPercentage     (Default: 10) Maximum Degree of Parallelism in Percentage
    
      --help                             Display this help screen.
    
      --version                          Display version information.
    ```

# Getting Started

   `kube-scanner` is developed with [.NET Core 3.0](https://dotnet.microsoft.com/download/dotnet-core/3.0) 
   and has a dependency on only [Docker](https://docs.docker.com/install/) . 

1.	Running From source code

    ```bash
    git clone <https://deepnetwork.visualstudio.com/Deepnetwork/_git/kube-scanner>
    
    cd kube-scanner/kube-scanner
    
    dotnet run
    ```

2. Running via Docker image

    ```bash
    docker run \
        -v $HOME:/root/ \
        -v /var/run/docker.sock:/var/run/docker.sock \
        deepnetwork/kube-scanner
    ```

# Examples

1. Scan with Trivy

    1. Scan a Kubernetes cluster using local kube-config and save outputs into file exporter: 
              
        ```bash
        docker run \
            -v $HOME:/root/ \
            -v /var/run/docker.sock:/var/run/docker.sock \
            deepnetwork/kube-scanner trivy -e File
        ```        
        Scan results (json) and container log files are saved under `$HOME/.kube-scanner/exports` folder.
        
    1. Using a cache directory on your machine:
    
        ```bash
        docker run \
            -v $HOME:/root/ \
            -v /var/run/docker.sock:/var/run/docker.sock \
            deepnetwork/kube-scanner trivy -a [TRIVY_CACHE_PATH] -e File
        ```
        
        Replace [TRIVY_CACHE_PATH] with the cache directory on your machine.

       
    1. Running against a Private Container Registry (CR):
    
        ```bash
        docker run \
            -v $HOME:/root/ \
            -v /var/run/docker.sock:/var/run/docker.sock \
            deepnetwork/kube-scanner trivy -e File -c [CR_NAME] -u [CR_USER] -p [CR_USER_PASSWORD]
        ```
        
        Set following parameters to scan images from private container registry.
        
        ```
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
        
    1.  Saving results into a custom folder:

        ```bash
        docker run \         
            -v $HOME:/root/ \
            -v /var/run/docker.sock:/var/run/docker.sock \
            deepnetwork/kube-scanner trivy -e File -f $HOME/myfolder 
        ```  
    
        This command runs kube-scanner against default Kubernetes cluster and 
        saves export files into folder `$HOME/myfolder`
    
    1. Setting parallelism degree of percentage:
        
        ```    
        docker run \
            -v $HOME:/root/ \
            -v /var/run/docker.sock:/var/run/docker.sock \
            deepnetwork/kube-scanner trivy -e File -m 50
        ```       
        
        To run kube-scanner in parallel, you can set a parallelism degree in terms of percentage. 
        If you have 8 logical CPU in your machine and you select `-m 50`, this means that 4 CPU will be used 
        to create multiple scanner instances.
    
    1. Bulk upload:
    
       By default, kube-scanner uploads scan results into exporter one-by-one, if you select `-b
 true` option all results will be exported in once.
 
        ```bash
        docker run \
            -v $HOME:/root/ \
            -v /var/run/docker.sock:/var/run/docker.sock \
            deepnetwork/kube-scanner -e File -b true
        ```
        
1. Scan with Clair:

    // TO BE ADDED