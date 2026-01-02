using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Triggernometry.Core;

public partial class RealPlugin
{
    public class NamedCallback
    {

        public int Id { get; set; }
        public string Name { get; set; }
        public Delegate Callback { get; set; }
        public object Obj { get; set; }
        public string Registrant { get; set; }
        public DateTime RegistrationTime { get; set; }
        public DateTime? LastInvoked { get; set; }

        public void Invoke(string val)
        {
            Callback.DynamicInvoke(Obj, val);
            LastInvoked = DateTime.Now;
        }

    }

    public Dictionary<int, NamedCallback> callbacksById = new Dictionary<int, NamedCallback>();
    public Dictionary<string, List<NamedCallback>> callbacksByName = new Dictionary<string, List<NamedCallback>>(StringComparer.OrdinalIgnoreCase);

    public void InvokeNamedCallback(string name, string val)
    {
        List<NamedCallback> cbs = new List<NamedCallback>();
        lock (callbacksByName)
        {
            if (callbacksByName.ContainsKey(name))
            {
                cbs.AddRange(callbacksByName[name]);
            }
        }
        foreach (NamedCallback nc in cbs)
        {
            nc.Invoke(val);
            /*
            try
            {
                nc.Invoke(val);
            }
            catch (Exception ex)
            {
                Exception inner = ex;
                while (inner.InnerException != null)
                {
                    inner = inner.InnerException;
                }
                FilteredAddToLog(DebugLevelEnum.Error, I18n.Translate("internal/NamedCallback/exception",
                    "Exception occurred when invoking named callback {0}:\n {1}", name, inner.ToString()));
            }
             */
        }
    }

    public void RegisterNamedCallback(int id, string name, Delegate callback, object o, string registrant)
    {
        NamedCallback nc = new NamedCallback
        {
            Id = id,
            Callback = callback,
            Obj = o,
            Name = name,
            Registrant = registrant,
            RegistrationTime = DateTime.Now
        };
        lock (callbacksById)
        {
            callbacksById[id] = nc;
            if (!callbacksByName.ContainsKey(name))
            {
                callbacksByName[name] = new List<NamedCallback>();
            }
            callbacksByName[name].Add(nc);
        }
    }

    // used in scripts
    public int RegisterNamedCallback(string name, Delegate callback, object o = null, bool allowDuplicatedName = false, string registrant = "Triggernometry Script")
    {
        if (!allowDuplicatedName)
        {
            UnregisterNamedCallback(name);
        }

        lock (callbacksById)
        {
            // Find the first free positive integer ID
            int id = Enumerable.Range(1, int.MaxValue).Where(n => !callbacksById.ContainsKey(n)).First();
            RegisterNamedCallback(id, name, callback, o, registrant);
            return id;
        }
    }

        /// <summary>
        /// Unregisters the callback with the specified ID.
        /// This method is used by ProxyPlugin via reflection. <br />
        /// Not intended for use in user scripts or external plugins. <br />
        /// </summary>
        public void UnregisterNamedCallback(int id)
    {
        lock (callbacksById)
        {
            NamedCallback nc = null;
            if (!callbacksById.ContainsKey(id))
            {
                return;
            }
            nc = callbacksById[id];
            callbacksById.Remove(id);
            callbacksByName[nc.Name].Remove(nc);
            if (callbacksByName[nc.Name].Count == 0)
            {
                callbacksByName.Remove(nc.Name);
            }
        }
    }

        /// <summary>
        /// Unregisters all callbacks with the given name.
        /// </summary>
    public void UnregisterNamedCallback(string name)
        {
        lock (callbacksById)
        {
            if (!callbacksByName.ContainsKey(name))
            {
                return;
            }
            foreach (NamedCallback nc in callbacksByName[name])
            {
                callbacksById.Remove(nc.Id);
            }
            callbacksByName.Remove(name);
        }
    }

}
