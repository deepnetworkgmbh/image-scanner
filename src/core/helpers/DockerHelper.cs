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

        private readonly string _containerImageUri;
        private readonly string _containerName;
        private readonly IList<string> _cmd;
        private readonly HostConfig _hostConfig;
        private readonly IList<string> _env;

        private string _containerId;

        public DockerHelper(string containerImage, string containerName, IList<string> cmd, HostConfig hostConfig, IList<string> env)
        {
            this._containerImageUri = containerImage;
            this._containerName = containerName;
            this._cmd = cmd;
            this._hostConfig = hostConfig;
            this._env = env;

            // create the docker client
            this._dockerClient = new DockerClientConfiguration(new Uri(DockerApiUri())).CreateClient();

            // pull the container image
            this.PullImage(containerImage);
        }

        public static string CreateRandomContainerName(string prefix, int length)
        {
            var random = new Random();

            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";

            var str = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());

            return prefix + str;
        }

        public async Task StartContainer()
        {
            // create the container
            var response = await this._dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = this._containerImageUri,
                Name = this._containerName,
                Env = this._env,
                Cmd = this._cmd,
                HostConfig = this._hostConfig,
            });

            // get the container id
            this._containerId = response.ID;

            // start the container
            await this._dockerClient.Containers.StartContainerAsync(this._containerId, new ContainerStartParameters());
        }

        public async Task WriteContainerLogsAsync(string logFile)
        {
            if (this._containerId != null)
            {
                // wait until the container finishes running
                await this._dockerClient.Containers.WaitContainerAsync(this._containerId);

                // create parameters for reading logs
                var parameters = new ContainerLogsParameters
                {
                    ShowStdout = true,
                    ShowStderr = true,
                };

                // read logs from stream and save into file
                var logStream = await this._dockerClient.Containers.GetContainerLogsAsync(this._containerId, parameters);
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
            if (this._containerId == null)
            {
                return string.Empty; // return an empty string if container not exists
            }

            // wait until the container finishes running
            await this._dockerClient.Containers.WaitContainerAsync(this._containerId);

            // create parameters for reading logs
            var parameters = new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
            };

            // read logs from stream and save into file
            var logStream = await this._dockerClient.Containers.GetContainerLogsAsync(
                this._containerId,
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
            if (this._containerId == null)
            {
                return new JArray(); // return an empty json array if container not exists
            }

            // wait until the container finishes running
            await this._dockerClient.Containers.WaitContainerAsync(this._containerId);

            var parameters = new GetArchiveFromContainerParameters
            {
                Path = filePath,
            };

            JArray jsonArray;
            try
            {
                var response = await this._dockerClient.Containers.GetArchiveFromContainerAsync(
                    this._containerId,
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
                LogHelper.LogErrorsAndContinue("Error scanning image", this._cmd.Last(), "Docker API responded with status code=NotFound",  e.Message);
            }

            return jsonArray;
        }

        public async Task DisposeAsync()
        {
            if (this._containerId != null)
            {
                // remove the container
                await this._dockerClient.Containers.RemoveContainerAsync(
                    this._containerId,
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

        private void PullImage(string image)
        {
            // split the image name into uri and tag
            var imageStrings = image.Split(':');
            var imageUri = imageStrings[0];
            var imageTag = imageStrings[1];

            try
            {
                // pull the image
                this._dockerClient.Images
                    .CreateImageAsync(
                        new ImagesCreateParameters
                        {
                            FromImage = imageUri,
                            Tag = imageTag,
                        },
                        new AuthConfig(),
                        new Progress<JSONMessage>()).Wait();
            }
            catch (AggregateException ae)
            {
                LogHelper.LogErrorsAndExit(
                    "kube-scanner needs Docker to be installed beforehand.",
                    "If you're running kube-scanner from Docker image, you need to mount /var/run/docker.sock.",
                    ae.Message);
            }
        }
    }
}