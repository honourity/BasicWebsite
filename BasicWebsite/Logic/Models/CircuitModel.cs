using System;
using System.Linq;
using System.Reflection;
using System.Web.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Logic.Helpers;

namespace Logic.Models
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

        public DateTime ModelCreatedDate { get; internal set; }
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
        public double LastAttemptTimeTaken { get; internal set; }
        public FailureReasonEnum LastAttemptFailureReason { get; internal set; }
        public int ErrorCount { get; internal set; }
        public DateTime? LastFailedAttemptTimestamp { get; internal set; }
        public LogLevelEnum LogLevel { get; internal set; }

        [JsonIgnore]
        public string LastLogSerialized { get; set; }

        public dynamic LastLog
        {
            get
            {
                if (LastLogSerialized == null)
                {
                    return null;
                }
                else
                {
                    return Json.Decode(LastLogSerialized);
                }
            }
        }

        [JsonIgnore]
        public string LastWebServiceLogSerialized { get; set; }

        public string LastWebServiceLog
        {
            get
            {
                return LastWebServiceLogSerialized;
            }
        }

        public CircuitModel(string methodKey, MethodInfo methodInfo)
        {
            ModelCreatedDate = DateTime.Now;

            Type methodType = methodInfo.DeclaringType;

            MethodKey = methodKey;
            MethodAssembly = methodType.Assembly.GetName().Name;

            string rawNamespace = methodType.Namespace;
            int index = rawNamespace.IndexOf(MethodAssembly);
            //+1 on the length to remove, to remove the . at end of namespace (unless the assembly IS the namespace, so we do nothing)
            if (MethodAssembly.Length == rawNamespace.Length)
            {
                MethodNamespace = rawNamespace;
            }
            else
            {
                MethodNamespace = (index < 0) ? rawNamespace : rawNamespace.Remove(index, MethodAssembly.Length + 1);
            }

            MethodDeclaringType = methodType.Name;
            MethodName = methodInfo.Name;

            ErrorCount = 0;
            LastAttemptTimestamp = null;
            LastFailedAttemptTimestamp = null;

            var circuitConfigSection = ConfigHelper.GetConfigValue<CircuitModelConfigurationSection>("CircuitBreakerConfigSection");
            var item = circuitConfigSection.Circuits.FirstOrDefault(match => match.MethodKey == methodKey);
        }
    }
}
