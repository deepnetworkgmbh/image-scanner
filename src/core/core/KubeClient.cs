using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using core.helpers;
using k8s;
using k8s.Exceptions;
using static k8s.KubernetesClientConfiguration;

namespace core.core
{
    public class KubeClient
    {
        private KubernetesClientConfiguration kubeConfig;

        private KubeClient()
        {
        }

        public static Task<KubeClient> CreateAsync(string kubeConfigStr)
        {
            var ret = new KubeClient();
            return ret.InitializeAsync(kubeConfigStr);
        }

        public async Task<IEnumerable<string>> GetImages()
        {
            List<string> imageList = null;

            try
            {
                // use the config object to create a client.
                var kubeClient = new Kubernetes(this.kubeConfig);

                // get the pod list
                var podList = await kubeClient.ListPodForAllNamespacesAsync();

                // generate a unique list of images
                imageList = (from pod in podList.Items from container in pod.Spec.Containers select container.Image).Distinct().ToList();

                return imageList;
            }
            catch (HttpRequestException e)
            {
                // if kubernetes cluster is not accessible
                LogHelper.LogErrorsAndExit(e.Message);
            }

            return imageList;
        }

        private async Task<KubeClient> InitializeAsync(string kubeConfigStr)
        {
            try
            {
                var kubeConfigDefaultLocation = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Path.Combine(Environment.GetEnvironmentVariable("USERPROFILE"), ".kube\\config") : Path.Combine(Environment.GetEnvironmentVariable("HOME"), ".kube/config");

                // check if kube config is accessible
                var configuration = await LoadKubeConfigAsync(kubeConfigStr ?? kubeConfigDefaultLocation);
                this.kubeConfig = BuildConfigFromConfigObject(configuration);
            }
            catch (KubeConfigException e)
            {
                LogHelper.LogErrorsAndExit(e.Message);
            }
            catch (Exception ex) when (ex.Source == "System.Security.Cryptography.X509Certificates")
            {
                LogHelper.LogErrorsAndExit("KubeConfig OpenSsl Certificate Error", ex.Message);
            }
            catch (Exception e)
            {
                LogHelper.LogErrorsAndExit(e.Message);
            }

            return this;
        }
    }
}