using System.Collections.Generic;
using System.Linq;
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
            // use the config object to create a client.
            var kubeClient = new Kubernetes(_kubeConfig);

            // get the pod list
            var podList = kubeClient.ListPodForAllNamespaces();
            
            // generate a unique list of images
            var imageList = (from pod in podList.Items from container in pod.Spec.Containers select container.Image).Distinct().ToList();

            return imageList;
        }
    }
}