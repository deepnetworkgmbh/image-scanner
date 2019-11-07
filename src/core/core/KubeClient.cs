using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using core.helpers;
using k8s;
using k8s.Exceptions;
using static k8s.KubernetesClientConfiguration;

namespace core.core
{
    public class KubeClient
    {
        private readonly KubernetesClientConfiguration kubeConfig;

        public KubeClient(string kubeConfig)
        {
            try
            {
                // check if kube config is accessible
                this.kubeConfig = BuildConfigFromConfigFile(kubeConfig);
            }
            catch (KubeConfigException e)
            {
                LogHelper.LogErrorsAndExit(e.Message);
            }
            catch (Exception ex) when (ex.Source == "System.Security.Cryptography.X509Certificates")
            {
                LogHelper.LogErrorsAndExit("KubeConfig OpenSsl Certificate Error", ex.Message);
            }
        }

        public IEnumerable<string> GetImages()
        {
            List<string> imageList = null;

            try
            {
                // use the config object to create a client.
                var kubeClient = new Kubernetes(this.kubeConfig);

                // get the pod list
                var podList = kubeClient.ListPodForAllNamespaces();

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
    }
}