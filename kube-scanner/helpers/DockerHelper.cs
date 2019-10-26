using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
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
        private readonly IList<string> _env;


        public DockerHelper(string containerImage, string containerName, IList<string> cmd, HostConfig hostConfig, IList<string> env)
        {
            _containerImageUri = containerImage;
            _containerName = containerName;
            _cmd = cmd;
            _hostConfig = hostConfig;
            _env = env;

            // create the docker client
            _dockerClient = new DockerClientConfiguration(new Uri(DockerApiUri())).CreateClient();
            
            // pull the container image
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
            string imageUri = string.Empty, imageTag;
            
            // split the image name into uri and tag
            var imageStrings = image.Split(':');
            
            try
            {
                imageUri = imageStrings[0];
                imageTag = imageStrings[1];
            }
            catch (Exception e)
            {
                imageTag = "latest"; // if an image doesn't own a tag, then it is the latest
            }

            // pull the image
            _dockerClient.Images
                .CreateImageAsync(new ImagesCreateParameters
                    {
                        FromImage = imageUri,
                        Tag = imageTag
                    },
                    new AuthConfig(),
                    new Progress<JSONMessage>()).Wait();            
        }

        public async Task StartContainer()
        {
            // create the container
             var response = await _dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = _containerImageUri,
                Name  = _containerName,
                Env = _env,
                Cmd   = _cmd,
                HostConfig = _hostConfig,               
            });
    
            // get the container id
            _containerId = response.ID;

            // start the container    
            await _dockerClient.Containers.StartContainerAsync(_containerId, new ContainerStartParameters());
        }

        public async Task WriteContainerLogs()
        {
            if (_containerId != null)
            {
                // wait until the container finishes running
                await _dockerClient.Containers.WaitContainerAsync(_containerId);

                // create parameters for reading logs
                var parameters = new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true
                };

                // create the log directory
                var folderPath = Directory.GetCurrentDirectory();
                var fp = $"{folderPath}/logs/";
                if (!Directory.Exists(fp))
                    Directory.CreateDirectory(fp);

                // read logs from stream and save into file
                var logStream = await _dockerClient.Containers.GetContainerLogsAsync(_containerId, parameters, default);
                if (logStream != null)
                {
                    using (var reader = new StreamReader(logStream, Encoding.UTF8))
                    {
                        var log = reader.ReadToEnd();
                        File.AppendAllText(fp + _containerName + ".log", log);
                    }
                }
            }
        }

        public async Task DisposeAsync()
        {
            if (_containerId != null)
            {
                // remove the container
                await _dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters());
            }
        }
    }
}