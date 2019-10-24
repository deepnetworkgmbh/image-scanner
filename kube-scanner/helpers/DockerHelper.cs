using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;

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
            
            PullImage(containerImage);
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

        private void PullImage(string image)
        {
            // split image uri and image tag
            var imageStrings = image.Split(':');
            var imageUri = imageStrings[0];
            var imageTag = imageStrings[1];

            // pull the image
            _dockerClient.Images
                .CreateImageAsync(new ImagesCreateParameters
                    {
                        FromImage = imageUri,
                        Tag       = imageTag
                    },
                    new AuthConfig(),
                    new Progress<JSONMessage>()).Wait();
        }

        public async Task StartContainer(string image)
        {
            PullImage(image);
            
            // create the container
            var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = _containerImageUri,
                Name  = _containerName,
                Cmd   = _cmd,
                HostConfig = _hostConfig,               
            });
    
            // get the container id
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