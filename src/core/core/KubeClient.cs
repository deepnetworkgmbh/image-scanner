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

        public async Task<IEnumerable<ContainerImage>> GetImages(params string[] namespaces)
        {
            try
            {
                // use the config object to create a client.
                var kubeClient = new Kubernetes(this.kubeConfig);

                // get the pod list
                var podList = await kubeClient.ListPodForAllNamespacesAsync();

                var pods = podList.Items;

                if (namespaces?.Length > 0)
                {
                    if (namespaces.Contains("default"))
                    {
                        pods = podList
                            .Items
                            .Where(p => namespaces.Contains(p.Metadata.NamespaceProperty) || string.IsNullOrEmpty(p.Metadata.NamespaceProperty))
                            .ToArray();
                    }
                    else
                    {
                        pods = podList
                            .Items
                            .Where(p => namespaces.Contains(p.Metadata.NamespaceProperty))
                            .ToArray();
                    }
                }

                var containers = pods
                    .Where(pod => pod.Spec.Containers != null)
                    .SelectMany(pod => pod.Spec.Containers, (pod, container) => container?.Image)
                    .Select(ContainerImage.FromFullName);

                var initContainers = pods
                    .Where(pod => pod.Spec.InitContainers != null)
                    .SelectMany(pod => pod.Spec.InitContainers, (pod, container) => container.Image)
                    .Select(ContainerImage.FromFullName);

                var images = containers
                    .Concat(initContainers)
                    .Distinct()
                    .ToList();

                Logger.Information(
                        "Returning {ImageCount} unique images from {PodsCount} pods across {Namespaces} namespaces",
                        images.Count,
                        pods.Count,
                        namespaces != null ? string.Join(',', namespaces) : "all");

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