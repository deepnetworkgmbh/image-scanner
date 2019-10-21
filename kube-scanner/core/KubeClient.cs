using System;
using System.Collections.Generic;
using k8s;
using static k8s.KubernetesClientConfiguration;

namespace kube_scanner.core
{
    public class KubeClient
    {
        
        private readonly KubernetesClientConfiguration _kubeConfig;
        
        public KubeClient(string kubeConfig)
        {
            _kubeConfig = BuildConfigFromConfigFile(kubeConfig);
        }


        public IEnumerable<string> GetImages()
        {
            // Use the config object to create a client.
            var kubeClient = new Kubernetes(_kubeConfig);

            var podList = kubeClient.ListPodForAllNamespaces();

            var imageList = new List<string>();
            
            foreach (var pod in podList.Items)
            {
                foreach (var container in pod.Spec.Containers)
                {
                    imageList.Add(container.Image);
                }
            }

            return imageList;
        }
    }
}