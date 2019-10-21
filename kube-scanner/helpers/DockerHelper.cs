using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace kube_scanner.helpers
{
    public class DockerHelper
    {
        private readonly DockerClient _dockerClient;

        private string _containerId;
        private readonly string _containerImageUri;
        private readonly string _containerName;
        private readonly IList<string> _cmd;
        private readonly HostConfig _hostConfig;


        public DockerHelper(string containerImage, string containerName, IList<string> cmd, HostConfig hostConfig)
        {
            _containerImageUri = containerImage;
            _containerName = containerName;
            _cmd = cmd;
            _hostConfig = hostConfig;

            _dockerClient = new DockerClientConfiguration(new Uri(DockerApiUri())).CreateClient();
            
            PullImage(containerImage).Wait();
        }

        private string DockerApiUri()
        {
            var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            if (isWindows)
            {
                return "npipe://./pipe/docker_engine";
            }

            var isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

            if (isLinux)
            {
                return "unix:/var/run/docker.sock";
            }

            throw new Exception(
                "Was unable to determine what OS this is running on, does not appear to be Windows or Linux!?");
        }

        private async Task PullImage(string image)
        {
            var imageData = image.Split(':');
            var imageUri = imageData[0];
            var imageTag = imageData[1];

            // pull the image
            await _dockerClient.Images
                .CreateImageAsync(new ImagesCreateParameters
                    {
                        FromImage = imageUri,
                        Tag       = imageTag
                    },
                    new AuthConfig(),
                    new Progress<JSONMessage>());
        }

        public async Task StartContainer(string image)
        {
            PullImage(image).Wait();
            
            // create the container
            var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = _containerImageUri,
                Name  = _containerName,
                Cmd   = _cmd,
                HostConfig = _hostConfig,               
            });

            _containerId = response.ID;

            // start the container    
            await _dockerClient.Containers.StartContainerAsync(_containerId, null);
        }

        public async Task DisposeAsync()
        {
            if (_containerId != null)
            {
                // wait until the container finishes running
                await _dockerClient.Containers.WaitContainerAsync(_containerId);
                
                // remove the container
                await _dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters());
            }
        }
    }
}