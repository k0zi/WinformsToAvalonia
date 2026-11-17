using Newtonsoft.Json;

namespace SemanticApp.Services
{
    // Names a NuGet package's attribute: carried over, together with the package reference.
    public class Snapshot
    {
        [JsonProperty("total")]
        public int Total { get; set; }
    }
}
