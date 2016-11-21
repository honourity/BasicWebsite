using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Shared.Settings;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Shared.Models
{
    [Serializable]
    public class CircuitModel
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum BreakStatusEnum
        {
            Closed,
            HalfOpen,
            Open
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum FailureReasonEnum
        {
            None,
            Timeout,
            Exception,
            OpenCircuit
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum LogLevelEnum
        {
            None = 0,
            Errors = 1,
            Timeouts = 2,
            SuccessfulCalls = 3,
            OpenCircuitFailures = 4,
            All = 5
        }

        public string MethodKey { get; internal set; }
        public string MethodAssembly { get; internal set; }
        public string MethodNamespace { get; internal set; }
        public string MethodDeclaringType { get; internal set; }
        public string MethodName { get; internal set; }
        public int Timeout { get; internal set; }
        public int LimitBreak { get; internal set; }
        public int Cooldown { get; internal set; }
        public BreakStatusEnum BreakStatus
        {
            get
            {
                if (ErrorCount <= 0)
                {
                    return BreakStatusEnum.Closed;
                }
                if (ErrorCount >= LimitBreak)
                {
                    return BreakStatusEnum.Open;
                }
                else
                {
                    return BreakStatusEnum.HalfOpen;
                }
            }
        }
        public ulong Calls { get; internal set; }
        public DateTime? LastAttemptTimestamp { get; internal set; }
        public int LastAttemptTimeTaken { get; internal set; }
        public FailureReasonEnum LastAttemptFailureReason { get; internal set; }
        public int ErrorCount { get; internal set; }
        public DateTime? LastFailedAttemptTimestamp { get; internal set; }
        public LogLevelEnum LogLevel { get; internal set; }

        public CircuitModel(string methodKey, MethodInfo methodInfo)
        {
            Type methodType = methodInfo.DeclaringType;

            MethodKey = methodKey;
            MethodAssembly = methodType.Assembly.GetName().Name;

            string rawNamespace = methodType.Namespace;
            int index = rawNamespace.IndexOf(MethodAssembly);
            //+1 on the length to remove, to remove the . at end of namespace
            MethodNamespace = (index < 0) ? rawNamespace : rawNamespace.Remove(index, MethodAssembly.Length + 1);

            MethodDeclaringType = methodType.Name;
            MethodName = methodInfo.Name;

            ErrorCount = 0;
            LastAttemptTimestamp = null;
            LastFailedAttemptTimestamp = null;

            var circuitConfigSection = (Shared.Models.CircuitModelConfigurationSection)System.Web.Configuration.WebConfigurationManager.GetSection("CircuitBreakerConfigSection");

            var item = circuitConfigSection.Circuits.FirstOrDefault(match => match.Name == methodKey);
            if (item == null) item = circuitConfigSection.Circuits.FirstOrDefault(match => match.Name == "Default");

            if (item != null)
            {
                if (item.Timeout != null) Timeout = item.Timeout.Value;

                if (item.LimitBreak != null) LimitBreak = item.LimitBreak.Value;

                if (item.Cooldown != null) Cooldown = item.Cooldown.Value;

                if (item.LogLevel != null) LogLevel = item.LogLevel.Value;
            }
            else
            {
                throw new Exception("CircuitModel configuration invalid. Check to make sure CircuitBreaker.config is setup properly.");
            }
        }

        public string GenerateLogsQueryMostRecent(int logsToFetch)
        {
            return string.Format("SELECT TOP {0} * FROM logs WHERE logs.Data.Method = \"{1}\" AND logs.Environment = \"{2}\" ORDER BY logs.TimeStamp desc", logsToFetch, MethodKey, ConfigHelper.GetConfigValue<string>("EnvironmentURL"));
        }
    }

    public class CircuitModelConfigurationSection : ConfigurationSection
    {
        [ConfigurationProperty("Circuits", IsRequired = false)]
        public CircuitModelConfigurationElementCollection Circuits
        {
            get { return base["Circuits"] as CircuitModelConfigurationElementCollection; }
        }
    }

    [ConfigurationCollection(typeof(CircuitModelConfigurationElement), AddItemName = "Circuit", CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class CircuitModelConfigurationElementCollection : ConfigurationElementCollection, IEnumerable<CircuitModelConfigurationElement>
    {
        public CircuitModelConfigurationElement this[int index]
        {
            get { return (CircuitModelConfigurationElement)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        public void Add(CircuitModelConfigurationElement elem)
        {
            BaseAdd(elem);
        }

        public void Clear()
        {
            BaseClear();
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new CircuitModelConfigurationElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((CircuitModelConfigurationElement)element).Name;
        }

        public void Remove(CircuitModelConfigurationElement elem)
        {
            BaseRemove(elem.Name);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(String name)
        {
            BaseRemove(name);
        }

        public new IEnumerator<CircuitModelConfigurationElement> GetEnumerator()
        {
            int count = base.Count;
            for (int i = 0; i < count; i++)
            {
                yield return base.BaseGet(i) as CircuitModelConfigurationElement;
            }
        }
    }

    public class CircuitModelConfigurationElement : ConfigurationElement
    {
        [ConfigurationProperty("Name", DefaultValue = "Default", IsRequired = false)]
        public string Name
        {
            get
            {
                return this["Name"] as string;
            }
            set
            {
                this["Name"] = value;
            }
        }

        [ConfigurationProperty("Timeout", IsRequired = false)]
        public CircuitModelConfigurationValueProperty<int> Timeout
        {
            get
            {
                return this["Timeout"] as CircuitModelConfigurationValueProperty<int>;
            }
            set
            { this["Timeout"] = value; }
        }

        [ConfigurationProperty("LimitBreak", IsRequired = false)]
        public CircuitModelConfigurationValueProperty<int> LimitBreak
        {
            get
            {
                return this["LimitBreak"] as CircuitModelConfigurationValueProperty<int>;
            }
            set
            { this["LimitBreak"] = value; }
        }

        [ConfigurationProperty("Cooldown", IsRequired = false)]
        public CircuitModelConfigurationValueProperty<int> Cooldown
        {
            get
            {
                return this["Cooldown"] as CircuitModelConfigurationValueProperty<int>;
            }
            set
            { this["Cooldown"] = value; }
        }

        [ConfigurationProperty("LogLevel", IsRequired = false)]
        public CircuitModelConfigurationValueProperty<CircuitModel.LogLevelEnum> LogLevel
        {
            get
            {
                return this["LogLevel"] as CircuitModelConfigurationValueProperty<CircuitModel.LogLevelEnum>;
            }
            set
            { this["LogLevel"] = value; }
        }
    }

    public class CircuitModelConfigurationValueProperty<T> : ConfigurationElement
    {
        [ConfigurationProperty("Value", IsRequired = false)]
        public T Value
        {
            get
            { return (T)this["Value"]; }
            set
            { this["Value"] = value; }
        }
    }
}
