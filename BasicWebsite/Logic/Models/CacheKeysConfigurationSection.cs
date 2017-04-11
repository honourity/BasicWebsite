using System.Collections.Generic;

namespace Logic.Models
{
    public class CacheKeysConfigurationSection
    {
        public List<GroupConfigurationElement> Groups { get; set; }
    }

    public class DependencyConfigurationElement
    {
        public KeyConfigurationElement Key { get; set; }
        public GroupConfigurationElement Group { get; set; }
    }

    public class KeyConfigurationElement
    {
        public ExpiryConfigurationElement Expiry { get; set; }
        public GroupConfigurationElement Group { get; set; }
        public string Name { get; set; }
        public List<DependencyConfigurationElement> Dependencies { get; set; }

        public List<KeyConfigurationElement> DependenciesReference { get; set; }
    }

    public class GroupConfigurationElement
    {
        public string Name { get; set; }
        public List<KeyConfigurationElement> Keys { get; set; }
        public List<DependencyConfigurationElement> Dependencies { get; set; }
    }

    public class ExpiryConfigurationElement
    {
        public int Value { get; set; }
    }
}
