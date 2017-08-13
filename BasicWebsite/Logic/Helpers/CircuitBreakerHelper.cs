using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Logic.Models;

namespace Logic.Helpers
{
    public class CircuitBreakerHelper
    {
        private CachingHelper _cachingHelper;
        private LoggingHelper _loggingHelper;

        private KeyConfigurationElement _hashTableConfigElement;
        private KeyConfigurationElement _circuitConfigElement;

        public CircuitBreakerHelper(CachingHelper cachingHelper, LoggingHelper loggingHelper)
        {
            this._cachingHelper = cachingHelper;

            this._hashTableConfigElement = _cachingHelper.Config.Groups.FirstOrDefault(group => group.Name == "Helpers.CircuitBreakerHelper").Keys.FirstOrDefault(key => key.Name == "HashTable");
            this._circuitConfigElement = _cachingHelper.Config.Groups.FirstOrDefault(group => group.Name == "Helpers.CircuitBreakerHelper").Keys.FirstOrDefault(key => key.Name == "Circuit");
        }

        #region public

        public IEnumerable<CircuitModel> AllCircuits()
        {
            var result = new List<CircuitModel>();

            var circuitList = _cachingHelper.GetCacheItem<HashSet<string>>(_hashTableConfigElement);

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

        public CircuitModel Circuit(string key)
        {
            return _cachingHelper.GetCacheItem<CircuitModel>(_circuitConfigElement, key);
        }

        public IEnumerable<CircuitModel> CloseAllCircuits()
        {
            var circuits = AllCircuits();
            foreach (CircuitModel circuit in circuits)
            {
                CloseCircuit(circuit);
            }

            return AllCircuits();
        }

        public IEnumerable<CircuitModel> OpenAllCircuits()
        {
            var circuits = AllCircuits();
            foreach (CircuitModel circuit in circuits)
            {
                OpenCircuit(circuit);
            }

            return AllCircuits();
        }

        public void SetCachedCircuit(CircuitModel model)
        {
            if (model != null)
            {
                _cachingHelper.AddCacheItem(_circuitConfigElement, model, model.MethodKey);
                AppendToCircuitStatusHashTable(model.MethodKey);
            }
        }

        public void ClearAllData()
        {
            var circuits = AllCircuits();

            //remove each individual circuit's status
            foreach (CircuitModel circuit in circuits)
            {
                ClearCircuit(circuit);
            }

            //disabled this, since ClearCircuit will clear individual entries in the hashTable. Less performance in clearing, but cleaner code
            //remove the circuit reference tracking table
            //_cachingHelper.ClearCacheItem(_hashTableConfigElement);
        }

        public void ClearCircuit(string methodKey)
        {
            //removing the actual circuit status model
            _cachingHelper.ClearCacheItem(_circuitConfigElement, methodKey);

            //removing the pointer reference to status model from the tracking table
            var trackingTable = _cachingHelper.GetCacheItem<HashSet<string>>(_hashTableConfigElement);
            trackingTable.Remove(methodKey);
            _cachingHelper.AddCacheItem(_hashTableConfigElement, trackingTable);
        }

        public void ClearCircuit(CircuitModel circuit)
        {
            ClearCircuit(circuit.MethodKey);
        }

        public CircuitModel OpenCircuit(string methodKey)
        {
            var circuit = Circuit(methodKey);
            return OpenCircuit(circuit);
        }

        public CircuitModel OpenCircuit(CircuitModel circuit)
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

            return circuit;
        }

        public CircuitModel CloseCircuit(string methodKey)
        {
            var circuit = Circuit(methodKey);
            return CloseCircuit(circuit);
        }

        public CircuitModel CloseCircuit(CircuitModel circuit)
        {
            if (circuit.ErrorCount >= circuit.LimitBreak)
            {
                //set errors to limit to cause instant circuit break
                circuit.ErrorCount = circuit.LimitBreak - 1;

                //update cache to save these changes
                SetCachedCircuit(circuit);
            }

            return circuit;
        }

        #region Public Call Overloads
        //C# only supports up to 16 arguments in a method call and overload methods are much more efficient to call than using reflection so...
        public TResult Call<TResult>(Func<TResult> method)
        {
            Delegate delegateMethod = new Func<TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod);
        }
        public TResult Call<TArg1, TResult>(Func<TArg1, TResult> method, TArg1 arg1)
        {
            Delegate delegateMethod = new Func<TArg1, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1);
        }
        public TResult Call<TArg1, TArg2, TResult>(Func<TArg1, TArg2, TResult> method, TArg1 arg1, TArg2 arg2)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2);
        }
        public TResult Call<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }
        public TResult Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TResult>(Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TResult> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16)
        {
            Delegate delegateMethod = new Func<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16, TResult>(method);
            return CallInternal<TResult>(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }

        //additional set of overloads to handle void method calls
        public void Call(Action method)
        {
            Delegate delegateMethod = new Action(method);
            CallInternalVoid(method.Method, delegateMethod);
        }
        public void Call<TArg1>(Action<TArg1> method, TArg1 arg1)
        {
            Delegate delegateMethod = new Action<TArg1>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1);
        }
        public void Call<TArg1, TArg2>(Action<TArg1, TArg2> method, TArg1 arg1, TArg2 arg2)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2);
        }
        public void Call<TArg1, TArg2, TArg3>(Action<TArg1, TArg2, TArg3> method, TArg1 arg1, TArg2 arg2, TArg3 arg3)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4>(Action<TArg1, TArg2, TArg3, TArg4> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5>(Action<TArg1, TArg2, TArg3, TArg4, TArg5> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15);
        }
        public void Call<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>(Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16> method, TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4, TArg5 arg5, TArg6 arg6, TArg7 arg7, TArg8 arg8, TArg9 arg9, TArg10 arg10, TArg11 arg11, TArg12 arg12, TArg13 arg13, TArg14 arg14, TArg15 arg15, TArg16 arg16)
        {
            Delegate delegateMethod = new Action<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TArg8, TArg9, TArg10, TArg11, TArg12, TArg13, TArg14, TArg15, TArg16>(method);
            CallInternalVoid(method.Method, delegateMethod, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16);
        }
        
        #endregion

        #endregion

        #region private

        private void CallInternalVoid(MethodInfo methodInfo, Delegate method, params object[] args)
        {
            CallInternal<string>(methodInfo, method, args);
        }

        private TResult CallInternal<TResult>(MethodInfo methodInfo, Delegate method, params object[] args)
        {
            string methodKey = methodInfo.DeclaringType.FullName + "." + methodInfo.Name.Split('.').Last();

            var result = default(TResult);

            if (!Convert.ToBoolean(ConfigHelper.GetConfigValue<string>("MaintenanceMode")))
            {
                DateTime attemptStartedDateTime = DateTime.Now;
                var model = GetCachedCircuit(methodKey);
                if (model == null)
                {
                    model = new CircuitModel(methodKey, methodInfo);
                    SetCachedCircuit(model);
                }

                switch (model.BreakStatus)
                {
                    case CircuitModel.BreakStatusEnum.Closed:
                    case CircuitModel.BreakStatusEnum.HalfOpen:
                        Stopwatch sw = Stopwatch.StartNew();

                        try
                        {
                            result = (TResult)method.DynamicInvoke(args);
                            sw.Stop();

                            if (sw.ElapsedMilliseconds > (model.Timeout * 1000))
                            {
                                //timeout

                                //get latest fresh model data from cache
                                model = GetCachedCircuit(methodKey) ?? new CircuitModel(methodKey, methodInfo);

                                UpdateCircuit(model, CircuitModel.FailureReasonEnum.Timeout, sw, attemptStartedDateTime);
                                if (model.LogLevel >= CircuitModel.LogLevelEnum.Timeouts) LogCallDetails(method, args, model, default(TResult), sw);
                                SetCachedCircuit(model);
                            }
                            else
                            {
                                //everything went ok

                                //get latest fresh model data from cache
                                model = GetCachedCircuit(methodKey) ?? new CircuitModel(methodKey, methodInfo);

                                UpdateCircuit(model, CircuitModel.FailureReasonEnum.None, sw, attemptStartedDateTime);
                                if (model.LogLevel >= CircuitModel.LogLevelEnum.SuccessfulCalls) LogCallDetails(method, args, model, result, sw);
                                SetCachedCircuit(model);
                            }
                        }
                        catch (Exception exception)
                        {
                            //exception thrown

                            //get latest fresh model data from cache
                            model = GetCachedCircuit(methodKey) ?? new CircuitModel(methodKey, methodInfo);

                            if (exception is TargetInvocationException)
                            {
                                if (exception.InnerException != null)
                                {
                                    exception = exception.InnerException;
                                }
                            }

                            UpdateCircuit(model, CircuitModel.FailureReasonEnum.Exception, sw, attemptStartedDateTime);
                            if (model.LogLevel >= CircuitModel.LogLevelEnum.Errors) LogExceptionDetails<TResult>(method, args, model, exception, sw);
                            SetCachedCircuit(model);

                            throw exception;
                        }

                        break;
                    case CircuitModel.BreakStatusEnum.Open:
                        //open circuit

                        //get latest fresh model data from cache
                        model = GetCachedCircuit(methodKey) ?? new CircuitModel(methodKey, methodInfo);

                        UpdateCircuit(model, CircuitModel.FailureReasonEnum.OpenCircuit, Stopwatch.StartNew(), attemptStartedDateTime);

                        SetCachedCircuit(model);

                        if (model.BreakStatus != CircuitModel.BreakStatusEnum.Open)
                        {
                            result = CallInternal<TResult>(methodInfo, method, args);

                            if (model.BreakStatus != CircuitModel.BreakStatusEnum.Open) model.ErrorCount++;
                            SetCachedCircuit(model);
                        }
                        else
                        {
                            if (model.LogLevel >= CircuitModel.LogLevelEnum.OpenCircuitFailures) LogCallDetails(method, args, model, default(TResult));
                        }

                        break;
                    default:
                        break;
                }
            }

            return result;
        }

        private void LogExceptionDetails<TResult>(Delegate method, object[] args, CircuitModel model, Exception exception, Stopwatch sw = null)
        {
            var log = new Dictionary<string, object>();

            log["Method"] = model.MethodKey;

            //look back in the stacktrace to find parent calling method
            var methodBase = (new StackTrace()).GetFrame(6).GetMethod();
            var classType = methodBase.ReflectedType;
            if (classType != null)
            {
                var namespaceName = classType.Namespace;
                log["CalledFrom"] = namespaceName + "." + classType.Name + "." + methodBase.Name;
            }

            if (sw != null)
            {
                log["TimeTaken"] = sw.Elapsed;
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
                loggableExceptionData["ClassName"] = exception.GetType().FullName;
                loggableExceptionData["Message"] = exception.Message;
                loggableExceptionData["InnerException"] = exception.InnerException;
                loggableExceptionData["StackTraceString"] = exception.GetType().FullName + exception.StackTrace.Replace("\r\n", "");
                loggableExceptionData["Source"] = exception.Source;
            }
            log["Exception"] = loggableExceptionData;

            model.LastLogSerialized = log.ToJSON();
            Task.Run(() => _loggingHelper.Log(log));
        }

        private void LogCallDetails<TResult>(Delegate method, object[] args, CircuitModel model, TResult result = default(TResult), Stopwatch sw = null)
        {
            var log = new Dictionary<string, object>();

            log["Method"] = model.MethodKey;

            if (sw != null)
            {
                log["TimeTaken"] = sw.Elapsed;
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
                    //this might seem pointless, but if ToJSON fails, it will cause an error here as a timeout attempt.
                    // Instead of the logging system recursively writing logs and calling siebel over and over if the result has a GET property which calls a service
                    // If there is a safe way to remove this line, performance could be increased on the circuitbreaker while logging is active
                    result.ToJSON();

                    log["Response"] = result;
                }
                catch (Exception e)
                {
                    log["ResponseSerializationFailure"] = e.Message;
                }
            }

            model.LastLogSerialized = log.ToJSON();
            Task.Run(() => _loggingHelper.Log(log));
        }

        private CircuitModel GetCachedCircuit(string methodName)
        {
            var model = _cachingHelper.GetCacheItem<CircuitModel>(_circuitConfigElement, methodName);
            return model;
        }

        private void AppendToCircuitStatusHashTable(string value)
        {
            var circuitList = _cachingHelper.GetCacheItem<HashSet<string>>(_hashTableConfigElement);

            if (circuitList == null)
            {
                circuitList = new HashSet<string>();
            }

            circuitList.Add(value);

            _cachingHelper.AddCacheItem(_hashTableConfigElement, circuitList);
        }

        private void UpdateCircuit(CircuitModel model, CircuitModel.FailureReasonEnum failure, Stopwatch sw, DateTime attemptStartedDateTime)
        {
            //keeping a running tally of total calls and attempt stats (includes open circuit status attempts)
            model.Calls++;
            model.LastAttemptTimeTaken = sw.Elapsed.TotalMilliseconds;
            model.LastAttemptTimestamp = attemptStartedDateTime;

            //moves between closed/half-open/open depending on current cooldown period, status, and recent call attempt outcome
            if (failure != CircuitModel.FailureReasonEnum.OpenCircuit)
            {
                if (failure == CircuitModel.FailureReasonEnum.None)
                {
                    //reduce errocount with each successfull call attempt
                    if (model.ErrorCount > 0)
                    {
                        model.LastFailedAttemptTimestamp = null;
                        model.LastLogSerialized = null;
                        model.ErrorCount--;
                    }
                }
                else
                {
                    model.LastFailedAttemptTimestamp = attemptStartedDateTime;
                    model.ErrorCount++;

                    if (model.BreakStatus == CircuitModel.BreakStatusEnum.Open)
                    {
                        //model.BreakStatus;
                        //model.LastAttemptFailureReason;
                        //model;

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
                if ((model.LastFailedAttemptTimestamp == null) || (attemptStartedDateTime.Subtract(model.LastFailedAttemptTimestamp.Value).TotalSeconds > model.Cooldown))
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
        }

        #endregion
    }
}
