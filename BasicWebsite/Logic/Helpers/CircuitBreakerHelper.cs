using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Shared.Models;

namespace Shared.Helpers
{
    public static class CircuitBreakerHelper
    {
        private const string CIRCUIT_BREAKER_CACHE_KEY = "CIRCUIT_BREAKER_CACHE_KEY";
        private const string CIRCUIT_STATUS_TABLE_KEY = "CIRCUIT_STATUS_TABLE";
        private const int CACHE_EXPIRY_MINUTES = 120;
        private const string CRM_LAYER_LABEL = "Lta.Web.Services.Crm";

        #region public

        public static IEnumerable<CircuitModel> AllCircuits()
        {
            var result = new List<CircuitModel>();

            var circuitList = Caching.CachingHelper.GetCacheItem<HashSet<string>>(CIRCUIT_BREAKER_CACHE_KEY, CIRCUIT_STATUS_TABLE_KEY, string.Empty);

            if (circuitList != null)
            {
                foreach (var key in circuitList)
                {
                    var model = GetCachedCircuit(key);
                    if (model != null)
                    {
                        result.Add(model);
                    }
                }
            }

            return result;
        }

        public static void CloseAllCircuits()
        {
            var circuits = AllCircuits();
            foreach (CircuitModel circuit in circuits)
            {
                if (circuit.ErrorCount >= circuit.LimitBreak)
                {
                    circuit.ErrorCount--;
                    SetCachedCircuit(circuit);
                }
            }
        }

        public static void OpenAllCircuits()
        {
            var circuits = AllCircuits();
            foreach (CircuitModel circuit in circuits)
            {
                if (circuit.ErrorCount < circuit.LimitBreak)
                {
                    //set errors to limit to cause instant circuit break
                    circuit.ErrorCount = circuit.LimitBreak;

                    //set last failed attempt to now, to force maximum cooldown period
                    circuit.LastFailedAttemptTimestamp = DateTime.Now;

                    //update cache to save these changes
                    SetCachedCircuit(circuit);
                }
            }
        }

        public static void ClearAllData()
        {
            var circuits = AllCircuits();

            //remove each individual circuit's status
            foreach (CircuitModel circuit in circuits)
            {
                Caching.CachingHelper.ClearCacheItem(CIRCUIT_BREAKER_CACHE_KEY, circuit.MethodKey, string.Empty);
            }

            //remove the circuit reference tracking table
            Caching.CachingHelper.ClearCacheItem(CIRCUIT_BREAKER_CACHE_KEY, CIRCUIT_STATUS_TABLE_KEY, string.Empty);
        }

        public static void ClearData(string methodKey)
        {
            //removing the actual circuit status model
            Caching.CachingHelper.ClearCacheItem(CIRCUIT_BREAKER_CACHE_KEY, methodKey, string.Empty);

            //removing the pointer reference to status model from the tracking table
            var trackingTable = Caching.CachingHelper.GetCacheItem<HashSet<string>>(CIRCUIT_BREAKER_CACHE_KEY, CIRCUIT_STATUS_TABLE_KEY, string.Empty);
            trackingTable.Remove(methodKey);
            Caching.CachingHelper.AddCacheItem(CIRCUIT_BREAKER_CACHE_KEY, CIRCUIT_STATUS_TABLE_KEY, trackingTable, string.Empty, CACHE_EXPIRY_MINUTES);

        }

        //C# only supports up to 16 arguments in a method call and overload methods are much more efficient to call than using reflection so...
        public static TResult Call<TResult>(Func<TResult> method)
        {
            Delegate delegateMethod = new Func<TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod);
        }
        public static TResult Call<TArg1, TResult>(Func<TArg1, TResult> method, TArg1 arg1)
        {
            Delegate delegateMethod = new Func<TArg1, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1);
        }
        public static TResult Call<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> method, TArg1 arg1, TArg2 arg2)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }
        public static TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TResult>(method);
            return Call_Internal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }

        #endregion

        #region private

        private static TResult Call_Internal<TResult>(MethodInfo methodInfo, Delegate method, params object[] args)
        {
            string methodKey = methodInfo.DeclaringType.FullName + "." + methodInfo.Name.Split('.').Last();

            var result = default(TResult);

            if (!Convert.ToBoolean(ConfigurationManager.AppSettings["MaintenanceModeCRM"]) || !methodKey.StartsWith(CRM_LAYER_LABEL))
            {
                var model = GetCachedCircuit(methodKey);
                if (model == null) model = new CircuitModel(methodKey, methodInfo);

                switch (model.BreakStatus)
                {
                    case CircuitModel.BreakStatusEnum.Closed:
                    case CircuitModel.BreakStatusEnum.HalfOpen:
                        result = MonitorTask(Task.Run(() => { return (TResult)method.DynamicInvoke(args); }), method, args, model);
                        break;
                    case CircuitModel.BreakStatusEnum.Open:
                        UpdateCircuit(model, CircuitModel.FailureReasonEnum.OpenCircuit, Stopwatch.StartNew());
                        if (model.LogLevel >= CircuitModel.LogLevelEnum.OpenCircuitFailures) Task.Run(() => LogCallDetails(method, args, model, default(TResult)));
                        break;
                    default:
                        break;
                }
            }

            return result;
        }

        private static TResult MonitorTask<TResult>(Task<TResult> task, Delegate method, object[] args, CircuitModel model)
        {
            Stopwatch sw = Stopwatch.StartNew();

            TResult result = default(TResult);

            try
            {
                if (!task.Wait(model.Timeout * 1000))
                {
                    sw.Stop();
                    UpdateCircuit(model, CircuitModel.FailureReasonEnum.Timeout, sw);
                    if (model.LogLevel >= CircuitModel.LogLevelEnum.Timeouts) Task.Run(() => LogCallDetails<TResult>(method, args, model, default(TResult), sw));
                }
                else
                {
                    sw.Stop();
                    result = task.Result;
                    UpdateCircuit(model, CircuitModel.FailureReasonEnum.None, sw);
                    if (model.LogLevel >= CircuitModel.LogLevelEnum.SuccessfulCalls) Task.Run(() => LogCallDetails(method, args, model, result, sw));
                }
            }
            catch (AggregateException aggregateException)
            {
                sw.Stop();
                UpdateCircuit(model, CircuitModel.FailureReasonEnum.Exception, sw);
                if (model.LogLevel >= CircuitModel.LogLevelEnum.Errors) LogExceptionDetails<TResult>(method, args, model, aggregateException, sw);
                throw aggregateException;
            }

            return result;
        }

        private static async void LogExceptionDetails<TResult>(Delegate method, object[] args, CircuitModel model, AggregateException exception, Stopwatch sw = null)
        {
            var log = new Dictionary<string, object>();

            log["Method"] = model.MethodKey;

            //look back in the stacktrace to find parent calling method
            var methodBase = (new StackTrace()).GetFrame(6).GetMethod();
            var classType = methodBase.ReflectedType;
            var namespaceName = classType.Namespace;
            log["CalledFrom"] = namespaceName + "." + classType.Name + "." + methodBase.Name;

            if (sw != null)
            {
                log["TimeTakenSeconds"] = GetTimeTaken(sw);
            }

            if (args != null)
            {
                var definitionParams = method.Method.GetParameters();
                var argslist = new Dictionary<string, object>();
                for (int i = 0; i < args.Length; i++)
                {
                    argslist[definitionParams[i].Name] = args[i];
                }
                if (argslist.Count > 0)
                {
                    log["Request"] = argslist;
                }
            }

            var loggableExceptionData = new Dictionary<string, object>();
            if (exception != null)
            {
                var innerBaseException = exception.GetBaseException().InnerException;
                loggableExceptionData["ClassName"] = innerBaseException.GetType().FullName;
                loggableExceptionData["Message"] = innerBaseException.Message;
                loggableExceptionData["InnerException"] = innerBaseException.InnerException;
                loggableExceptionData["StackTraceString"] = innerBaseException.StackTrace;
                loggableExceptionData["Source"] = innerBaseException.Source;
            }
            log["Exception"] = loggableExceptionData;

            await LoggingHelper.Log(log);
        }

        private static async void LogCallDetails<TResult>(Delegate method, object[] args, CircuitModel model, TResult result = default(TResult), Stopwatch sw = null)
        {
            var log = new Dictionary<string, object>();

            log["Method"] = model.MethodKey;

            if (sw != null)
            {
                log["TimeTakenSeconds"] = GetTimeTaken(sw);
            }

            if (model.LastAttemptFailureReason != CircuitModel.FailureReasonEnum.None) log["Failed"] = model.LastAttemptFailureReason;

            var argslist = new Dictionary<string, object>();
            if (args != null)
            {
                var definitionParams = method.Method.GetParameters();
                for (int i = 0; i < args.Length; i++)
                {
                    argslist[definitionParams[i].Name] = args[i];
                }
            }
            log["Request"] = argslist;

            //some objects don't have paramaterless constructors,
            // and some objects have properties which call other services (which means recursive .ToJSON() could indirectly trigger more service calls to siebel)
            ////if (result != null && result.ToJSON().ToMD5().Equals(Activator.CreateInstance(result.GetType()).ToJSON().ToMD5())) log.EmptyResponse = true;

            if (result != null)
            {
                try
                {
                    result.ToJSON();
                    log["Response"] = result;
                }
                catch (Exception e)
                {
                    log["ResponseSerializationFailure"] = e.Message;
                }
            }

            await LoggingHelper.Log(log);
        }

        private static double GetTimeTaken(Stopwatch sw)
        {
            return Convert.ToDouble(sw.Elapsed.Seconds) + (Convert.ToDouble(sw.Elapsed.Milliseconds) / 1000.0);
        }

        private static void SetCachedCircuit(CircuitModel model)
        {
            if (model != null)
            {
                Caching.CachingHelper.AddCacheItem(CIRCUIT_BREAKER_CACHE_KEY, model.MethodKey, model, string.Empty, CACHE_EXPIRY_MINUTES);
                AppendToCircuitStatusHashTable(model.MethodKey);
            }
        }

        private static CircuitModel GetCachedCircuit(string methodName)
        {
            var model = Caching.CachingHelper.GetCacheItem<CircuitModel>(CIRCUIT_BREAKER_CACHE_KEY, methodName, string.Empty);
            return model;
        }

        private static void AppendToCircuitStatusHashTable(string value)
        {
            var circuitList = Caching.CachingHelper.GetCacheItem<HashSet<string>>(CIRCUIT_BREAKER_CACHE_KEY, CIRCUIT_STATUS_TABLE_KEY, string.Empty);

            if (circuitList == null)
            {
                circuitList = new HashSet<string>();
            }

            circuitList.Add(value);

            Caching.CachingHelper.AddCacheItem(CIRCUIT_BREAKER_CACHE_KEY, CIRCUIT_STATUS_TABLE_KEY, circuitList, string.Empty, CACHE_EXPIRY_MINUTES);
        }

        private static void UpdateCircuit(CircuitModel model, CircuitModel.FailureReasonEnum failure, Stopwatch sw)
        {
            var timeNow = DateTime.Now;

            //keeping a running tally of total calls and attempt stats (includes open circuit status attempts)
            model.Calls++;
            model.LastAttemptTimeTaken = sw.Elapsed.Seconds;
            model.LastAttemptTimestamp = timeNow;

            //moves between closed/half-open/open depending on current cooldown period, status, and recent call attempt outcome
            if (failure != CircuitModel.FailureReasonEnum.OpenCircuit)
            {
                if (failure == CircuitModel.FailureReasonEnum.None)
                {
                    //reduce errocount with each successfull call attempt
                    if (model.ErrorCount > 0)
                    {
                        model.LastFailedAttemptTimestamp = null;
                        model.ErrorCount--;
                    }
                }
                else
                {
                    model.LastFailedAttemptTimestamp = timeNow;
                    model.ErrorCount++;

                    if (model.BreakStatus == CircuitModel.BreakStatusEnum.Open)
                    {
                        //use EmailHelper to queue an email with model data
                        // maybe EmailHelper should handle a quota of failures before it triggers
                        // or trigger a few times before giving up
                        // or wait before sending 1-2 emails, to make sure its not 50 emails, the either send
                        //  1 email "crm is down" or 2 emails for individual methods
                    }
                }

                //irrelevant of success or failure, update FailureReasonEnum with success/failure status to keep it fresh
                model.LastAttemptFailureReason = failure;
            }
            else
            {
                //if circuit is open, and either the last attempt was not a failure, or last failure was older than cooling off period
                if ((model.LastFailedAttemptTimestamp == null) || (timeNow.Subtract(model.LastFailedAttemptTimestamp.Value).TotalSeconds > model.Cooldown))
                {
                    //pretend there are no errors
                    model.LastFailedAttemptTimestamp = null;
                    model.LastAttemptFailureReason = CircuitModel.FailureReasonEnum.None;

                    //reduce error count by 1 (this will allow 1 call attempt before the circuit re-opens in case of failure)
                    if (model.ErrorCount > 0)
                    {
                        model.ErrorCount--;
                    }
                }
            }

            //saves the state of the breaker for subsequent call attempts to reuse
            SetCachedCircuit(model);
        }

        #endregion
    }
}
