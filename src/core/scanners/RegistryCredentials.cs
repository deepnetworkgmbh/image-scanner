namespace core.scanners
{
    /// <summary>
    /// Container Registry credentials.
    /// </summary>
    public class RegistryCredentials
    {
        /// <summary>
        /// Container registry address. For example, https://registry.hub.docker.com or myacr.azurecr.io.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// The username to connect to the registry.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password to connect to the registry.
        /// </summary>
        public string Password { get; set; }
    }
}