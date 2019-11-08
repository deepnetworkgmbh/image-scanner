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
    public class DockerContainer
    {
        public IList<string> Env { get; set; }

        public HostConfig HostConfig { get; set; }

        public IList<string> Cmd { get; set; }

        public string ContainerName { get; set; }

        public string ContainerImage { get; set; }

        private readonly DockerClient dockerClient;

        private string containerId;

        public DockerContainer()
        {
            // create the docker client
            this.dockerClient = new DockerClientConfiguration(new Uri(DockerApiUri())).CreateClient();
        }

        public static string CreateRandomContainerName(string prefix, int length)
        {
            var random = new Random();

            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

            var str = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return prefix + str;
        }

        public async Task PullImage()
        {
            // split the image name into uri and tag
            var imageStrings = this.ContainerImage.Split(':');
            var imageUri = imageStrings[0];
            var imageTag = imageStrings[1];

            try
            {
                // pull the image
                await this.dockerClient.Images
                    .CreateImageAsync(
                        new ImagesCreateParameters
                        {
                            FromImage = imageUri,
                            Tag = imageTag,
                        },
                        new AuthConfig(),
                        new Progress<JSONMessage>());
            }
            catch (AggregateException ae)
            {
                LogHelper.LogErrorsAndExit(
                    "kube-scanner needs Docker to be installed beforehand.",
                    "If you're running kube-scanner from Docker image, you need to mount /var/run/docker.sock.",
                    ae.Message);
            }
        }

        public async Task StartContainer()
        {
            // create the container
            var response = await this.dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = this.ContainerImage,
                Name = this.ContainerName,
                Env = this.Env,
                Cmd = this.Cmd,
                HostConfig = this.HostConfig,
            });

            // get the container id
            this.containerId = response.ID;

            // start the container
            await this.dockerClient.Containers.StartContainerAsync(this.containerId, new ContainerStartParameters());
        }

        public async Task<string> GetContainerLogsAsync()
        {
            if (this.containerId == null)
            {
                return string.Empty; // return an empty string if container not exists
            }

            // wait until the container finishes running
            await this.dockerClient.Containers.WaitContainerAsync(this.containerId);

            // create parameters for reading logs
            var parameters = new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
            };

            // read logs from stream and save into file
            var logStream = await this.dockerClient.Containers.GetContainerLogsAsync(
                this.containerId,
                parameters);

            if (logStream == null)
            {
                return string.Empty; // return an empty string if stream is null
            }

            // convert log stream to string
            using var reader = new StreamReader(logStream, Encoding.UTF8);
            var log = reader.ReadToEnd();

            return log;
        }

        public async Task<JArray> GetArchiveFromContainerAsync(string filePath)
        {
            if (this.containerId == null)
            {
                return new JArray(); // return an empty json array if container not exists
            }

            // wait until the container finishes running
            await this.dockerClient.Containers.WaitContainerAsync(this.containerId);

            var parameters = new GetArchiveFromContainerParameters
            {
                Path = filePath,
            };

            JArray jsonArray;
            try
            {
                var response = await this.dockerClient.Containers.GetArchiveFromContainerAsync(
                    this.containerId,
                    parameters,
                    false);

                var archiveStream = response.Stream;

                if (archiveStream == null)
                {
                    return new JArray(); // return an empty json array if stream is null
                }

                // convert archive stream to JArray object
                jsonArray = TarHelper.UnTarIntoJsonArray(archiveStream);
            }
            catch (DockerContainerNotFoundException e)
            {
                jsonArray = new JArray();
                LogHelper.LogErrorsAndContinue("Error scanning image", this.Cmd.Last(), "Docker API responded with status code=NotFound",  e.Message);
            }

            return jsonArray;
        }

        public async Task DisposeAsync()
        {
            if (this.containerId != null)
            {
                // remove the container
                await this.dockerClient.Containers.RemoveContainerAsync(
                    this.containerId,
                    new ContainerRemoveParameters());
            }
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
    }
}