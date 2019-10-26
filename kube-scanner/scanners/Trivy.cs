using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Docker.DotNet.Models;
using kube_scanner.core;
using kube_scanner.helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace kube_scanner.scanners
{
    public class Trivy : IScanner
    {
        private readonly string _containerRegistryAddress;
        private readonly string _containerRegistryUserName;
        private readonly string _containerRegistryPassword;
        private  const string TrivyImage = "aquasec/trivy:latest";
         
        public Trivy(string containerRegistryAddress, string containerRegistryUserName, string containerRegistryPassword)
        {
            _containerRegistryAddress  = containerRegistryAddress;
            _containerRegistryUserName = containerRegistryUserName;
            _containerRegistryPassword = containerRegistryPassword;
        }

        public async Task<ScanResult> Scan(string imageToBeScanned)
        { 
            // give a name to container
            var containerName = "trivy-container-"+(new Random().Next(10, 10000));
            
            // set the output file name that keeps scan result (json)
            var outputFile = "/trivy_scan_results/"+containerName;
            
            // commands that will be executed by trivy container
            var cmd = new[]
            {
                "-c",
                "-f", "json",
                "-o", outputFile,
                imageToBeScanned
            };

            // get the path of the running directory
            var folderPath = Directory.GetCurrentDirectory();
            
            // set the volumes will be mounted to trivy container
            var hostConfig = new HostConfig
            {
                Binds = new List<string>
                {
                    folderPath + "/Library/Caches:/root/.cache/",
                    folderPath + "/trivy_scan_results:/trivy_scan_results"
                },
                DNS = new List<string>{},
                DNSOptions = new List<string>{},
                DNSSearch = new List<string>{},
                PortBindings = new Dictionary<string, IList<PortBinding>>(),
                Devices = new List<DeviceMapping>(),
                BlkioWeightDevice = new List<WeightDevice>(),
                RestartPolicy = new RestartPolicy{Name = RestartPolicyKind.No, MaximumRetryCount = 0}
            };

            // If the provided private Container Registry (CR) name is equal to CR of image to be scanned,
            // add private CR credentials to the trivy container
            var crNameOfImage = imageToBeScanned.Split('/')[0];
            var crNameOfParameter = _containerRegistryAddress.Split('/')[0];
            var env = new List<string>();
            if (crNameOfParameter == crNameOfImage)
            {
                env.AddRange(new string[]
                {
                    "TRIVY_AUTH_URL="+_containerRegistryAddress, 
                    "TRIVY_USERNAME="+_containerRegistryUserName,
                    "TRIVY_PASSWORD="+_containerRegistryPassword             
                });
            }

            // create docker helper
            var dockerHelper = new DockerHelper(TrivyImage, containerName, cmd, hostConfig, env);
            
            // start to scan the image
            await dockerHelper.StartContainer();
            
            // write container logs into log files
            await dockerHelper.WriteContainerLogs();                          

            // remove the container
            await dockerHelper.DisposeAsync();
            
            // read the output file into a JSON array,
            // if the file is empty, put an empty array
            JArray jsonArray;
            try
            {
                jsonArray = JArray.Parse(File.ReadAllText(@folderPath+outputFile));
            }
            catch (JsonReaderException e)
            {
                // write error to the console
                string line;
                var file = new StreamReader(@folderPath + "/logs/"+containerName+".log"); 
                while((line = file.ReadLine()) != null)  
                {  
                    if (line.Contains("FATAL"))
                        Console.WriteLine("Scan ERROR in {0} :"+line, imageToBeScanned);
                } 
                
                jsonArray = new JArray(); // empty array for not scanned images 
            }
            
            // create a scan result object
            var trivyScanResult = new ScanResult {ImageName = imageToBeScanned, ScanResultArray = jsonArray};

            return trivyScanResult;
        }
    }
}