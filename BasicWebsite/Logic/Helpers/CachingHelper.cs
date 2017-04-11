using System;
using System.Collections.Generic;
using System.Linq;
using Enyim.Caching;
using Logic.Models;

namespace Logic.Helpers
{
    /// <summary>
    /// A help class for using the System.Runtime.MemoryCache provider
    /// </summary>
    public class CachingHelper
    {
        public CacheKeysConfigurationSection Config;

        private string environmentUrl;
        private MemcachedClient memcachedClient;
        private string dependenciesTableKeyFullKey;

        public CachingHelper()
        {
            environmentUrl = ConfigHelper.GetConfigValue<string>("EnvironmentURL") ?? "NoEnvironment";
            memcachedClient = new MemcachedClient();

            Config = (CacheKeysConfigurationSection)System.Web.Configuration.WebConfigurationManager.GetSection("CacheKeysConfigSection");
            foreach (var group in Config.Groups)
            {
                foreach (var key in group.Keys)
                {
                    //back-linking keys to their parent groups for faster runtime lookup
                    key.Group = group;

                    //pre-linking dependency elements to dependent elements for faster runtime lookup
                    foreach (var groupDependency in group.Dependencies)
                    {
                        LinkDependencies(key, groupDependency);
                    }
                    foreach (var keyDependency in key.Dependencies)
                    {
                        LinkDependencies(key, keyDependency);
                    }
                }
            }

            dependenciesTableKeyFullKey = GetKey(Config.Groups.FirstOrDefault(g => g.Name == "Lta.Shared.Caching.CachingHelper").Keys.FirstOrDefault(k => k.Name == "DependencyTable"), null);
        }

        #region DistributedCache

        public void AddCacheItem<T>(KeyConfigurationElement cacheKeyElement, T value, string modifier = null)
        {
            //do a final sanity check on the input data
            if (!typeof(T).IsSerializable)
            {
                throw new MissingFieldException("Object being cached is not marked with Serializable attribute, and was not cached");
            }

            var currentTimestamp = DateTime.Now;

            var dependenciesTable = GetDependenciesTable();

            var fullKey = GetKey(cacheKeyElement, modifier);

            //init cache expirydate
            DateTime? expiryDate = null;
            if (cacheKeyElement.Expiry != null)
            {
                expiryDate = currentTimestamp.AddMinutes(cacheKeyElement.Expiry.Value);
            }

            //process dependencies
            if (modifier == null)
            {
                foreach (var dependency in cacheKeyElement.DependenciesReference)
                {
                    var dependencyKey = GetKey(dependency, null);
                    if (!dependenciesTable.ContainsKey(dependencyKey))
                    {
                        dependenciesTable[dependencyKey] = new HashSet<string>();
                    }

                    dependenciesTable[dependencyKey].Add(fullKey);

                    //adjust cache expirydate based on existing dependency expirydates (so they expire at the same time at most)
                    if (dependency.Expiry != null)
                    {
                        var dependencyExpiry = currentTimestamp.AddMinutes(dependency.Expiry.Value);

                        if (expiryDate == null || expiryDate.Value > dependencyExpiry)
                        {
                            expiryDate = dependencyExpiry;
                        }
                    }
                }

                SetDependenciesTable(dependenciesTable);
            }

            //expire any dependent items
            if (dependenciesTable.ContainsKey(fullKey))
            {
                foreach (var dependentItem in dependenciesTable[fullKey])
                {
                    ClearCacheItem(dependentItem);
                }
            }

            //save to cache
            if (expiryDate.HasValue)
            {
                memcachedClient.Store(Enyim.Caching.Memcached.StoreMode.Set, fullKey, value, expiryDate.Value);
            }
            else
            {
                memcachedClient.Store(Enyim.Caching.Memcached.StoreMode.Set, fullKey, value);
            }
        }

        public T GetCacheItem<T>(KeyConfigurationElement cacheKeyElement, string modifier = null)
        {
            var fullKey = GetKey(cacheKeyElement, modifier);
            return memcachedClient.Get<T>(fullKey);
        }

        public void ClearCacheItem(KeyConfigurationElement cacheKeyElement, string modifier = null)
        {
            var fullKey = GetKey(cacheKeyElement, modifier);
            ClearCacheItem(fullKey);
        }

        public void ClearCacheAll()
        {
            memcachedClient.FlushAll();
        }

        public string CacheStatus()
        {
            var result = new Dictionary<string, Dictionary<string, string>>();

            foreach (Enyim.Caching.Memcached.StatItem statItem in Enum.GetValues(typeof(Enyim.Caching.Memcached.StatItem)))
            {
                var stats = memcachedClient.Stats().GetRaw(statItem.ToString());

                foreach (var stat in stats)
                {
                    var serverKey = stat.Key.Address + ":" + stat.Key.Port;
                    if (!result.ContainsKey(serverKey))
                    {
                        result[serverKey] = new Dictionary<string, string>();
                    }

                    result[serverKey][statItem.ToString()] = memcachedClient.Stats().GetRaw(stat.Key, statItem);
                }
            }

            return result.ToJSON();
        }

        private void ClearCacheItem(string key)
        {
            var dependenciesTable = GetDependenciesTable();

            if (dependenciesTable.ContainsKey(key))
            {
                foreach (var item in dependenciesTable[key])
                {
                    memcachedClient.Remove(item);
                }
            }

            memcachedClient.Remove(key);
        }

        private string GetKey(KeyConfigurationElement keyElement, string modifier)
        {
            string hashedModifier = null;

            if (!string.IsNullOrEmpty(modifier))
            {
                hashedModifier = modifier.ToMD5();
            }

            return string.Format("{0}{1}{2}{3}", environmentUrl, keyElement.Group.Name, keyElement.Name, hashedModifier);
        }

        private Dictionary<string, HashSet<string>> GetDependenciesTable()
        {
            var table = memcachedClient.Get<Dictionary<string, HashSet<string>>>(dependenciesTableKeyFullKey);

            if (table == null)
            {
                table = new Dictionary<string, HashSet<string>>();
                SetDependenciesTable(table);
            }

            return table;
        }

        private void SetDependenciesTable(Dictionary<string, HashSet<string>> table)
        {
            memcachedClient.Store(Enyim.Caching.Memcached.StoreMode.Set, dependenciesTableKeyFullKey, table);
        }

        private void LinkDependencies(KeyConfigurationElement key, DependencyConfigurationElement dependency)
        {
            if (string.IsNullOrEmpty(dependency.Key.Name))
            {
                var subList = Config.Groups.Where(g => g.Name == dependency.Group.Name);
                foreach (var element in subList)
                {
                    foreach (var subElement in element.Keys)
                    {
                        key.DependenciesReference.Add(subElement);
                    }
                }
            }
            else
            {
                var matchingGroup = Config.Groups.FirstOrDefault(g => g.Name == dependency.Group.Name);
                if (matchingGroup != null)
                {
                    var dependentKey = matchingGroup.Keys.FirstOrDefault(k => k.Name == dependency.Key.Name);
                    if (dependentKey != null)
                    {
                        key.DependenciesReference.Add(dependentKey);
                    }
                }
            }
        }

        #endregion
    }
}