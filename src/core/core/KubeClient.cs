using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using k8s;
using Serilog;

namespace core.core
{
    public class KubeClient
    {
        private static readonly ILogger Logger = Log.ForContext<KubeClient>();

        private KubernetesClientConfiguration kubeConfig;

        private KubeClient()
        {
        }

        public static Task<KubeClient> CreateAsync(string kubeConfigStr)
        {
            var kubeClient = new KubeClient();
            return kubeClient.InitializeAsync(kubeConfigStr);
        }

        public async Task<IEnumerable<ContainerImage>> GetImages()
        {
            try
            {
                // use the config object to create a client.
                var kubeClient = new Kubernetes(this.kubeConfig);

                // get the pod list
                var podList = await kubeClient.ListPodForAllNamespacesAsync();

                var containers = podList
                    .Items
                    .Where(pod => pod.Spec.Containers != null)
                    .SelectMany(pod => pod.Spec.Containers, (pod, container) => container?.Image)
                    .Select(ContainerImage.FromFullName);

                var initContainers = podList
                    .Items
                    .Where(pod => pod.Spec.InitContainers != null)
                    .SelectMany(pod => pod.Spec.InitContainers, (pod, container) => container.Image)
                    .Select(ContainerImage.FromFullName);

                var images = containers
                    .Concat(initContainers)
                    .Distinct()
                    .ToList();

                return images;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "failed to get Images");
            }

            return new List<ContainerImage>(0);
        }

        private async Task<KubeClient> InitializeAsync(string kubeConfigStr)
        {
            try
            {
                var kubeConfigDefaultLocation = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ".kube\\config")
                    : Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".kube/config");

                // check if kube config is accessible
                var configuration = await KubernetesClientConfiguration.LoadKubeConfigAsync(kubeConfigStr ?? kubeConfigDefaultLocation);
                this.kubeConfig = KubernetesClientConfiguration.BuildConfigFromConfigObject(configuration);
            }
            catch (Exception ex) when (ex.Source == "System.Security.Cryptography.X509Certificates")
            {
                Logger
                    .ForContext("KubeConfig", kubeConfigStr)
                    .Error(ex, "KubeConfig OpenSsl Certificate Error");
            }
            catch (Exception ex)
            {
                Log
                    .ForContext("KubeConfig", kubeConfigStr)
                    .Error(ex, "Failed to init kube-client with");
            }

            return this;
        }
    }
}