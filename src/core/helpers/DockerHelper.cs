using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Newtonsoft.Json.Linq;
using Task = System.Threading.Tasks.Task;

namespace core.helpers
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

        private static string DockerApiUri()
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
            // split the image name into uri and tag
            var imageStrings = image.Split(':');
            var imageUri = imageStrings[0];
            var imageTag = imageStrings[1];

            try
            {
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
            catch (AggregateException ae)
            {

                LogHelper.LogErrorsAndExit(
                    "kube-scanner needs Docker to be installed beforehand.",
                    "If you're running kube-scanner from Docker image, you need to mount /var/run/docker.sock.",
                    ae.Message
                );
            }
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

        public async Task WriteContainerLogsAsync(string logFile)
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
                
                // read logs from stream and save into file
                var logStream = await _dockerClient.Containers.GetContainerLogsAsync(_containerId, parameters);
                if (logStream != null)
                {
                    using var reader = new StreamReader(logStream, Encoding.UTF8);
                    var log = reader.ReadToEnd();
                    File.AppendAllText(logFile, log);
                }
            }
        }
        
        public async Task<string> GetContainerLogsAsync()
        {
            if (_containerId == null) return string.Empty; // return an empty string if container not exists
            
            // wait until the container finishes running
            await _dockerClient.Containers.WaitContainerAsync(_containerId);

            // create parameters for reading logs
            var parameters = new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true
            };

            // read logs from stream and save into file
            var logStream = await _dockerClient.Containers.GetContainerLogsAsync(_containerId, 
                parameters);
                
            if (logStream == null) return string.Empty; // return an empty string if stream is null
            
            // convert log stream to string
            using var reader = new StreamReader(logStream, Encoding.UTF8);
            var log = reader.ReadToEnd();
            
            return log;

        }

        public async Task<JArray> GetArchiveFromContainerAsync(string filePath)
        {
            if (_containerId == null) return new JArray(); // return an empty json array if container not exists
            
            // wait until the container finishes running
            await _dockerClient.Containers.WaitContainerAsync(_containerId);

            var parameters = new GetArchiveFromContainerParameters
            {
                Path = filePath
            };

            JArray jsonArray;
            try
            {
                var response = await _dockerClient.Containers.GetArchiveFromContainerAsync(_containerId,
                    parameters, false);
                
                var archiveStream = response.Stream;

                if (archiveStream == null) return new JArray(); // return an empty json array if stream is null
                
                // convert archive stream to JArray object
                jsonArray = TarHelper.UnTarIntoJsonArray(archiveStream);
            }
            catch (DockerContainerNotFoundException e)
            {
                jsonArray = new JArray();
                LogHelper.LogErrorsAndContinue("Error scanning image", _cmd.Last(), "Docker API responded with status code=NotFound",  e.Message);
            }
            
            return jsonArray;
        }

        public async Task DisposeAsync()
        {
            if (_containerId != null)
            {
                // remove the container
                await _dockerClient.Containers.RemoveContainerAsync(_containerId, new ContainerRemoveParameters());
            }
        }
        
        public static string CreateRandomContainerName(string prefix, int length)
        { 
            var random = new Random();
        
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            
            var str = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return prefix + str;
        }
    }
}