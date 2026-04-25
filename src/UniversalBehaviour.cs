using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
#if INTEROP
using Il2CppInterop.Runtime.Injection;
#endif
#if UNHOLLOWER
using UnhollowerRuntimeLib;
#endif

namespace UniverseLib
{
    /// <summary>
    /// Used for receiving Update events and starting Coroutines.
    /// </summary>
    internal class UniversalBehaviour : MonoBehaviour
    {
        internal static UniversalBehaviour Instance { get; private set; }

        internal static void Setup()
        {
#if CPP && INTEROP
            Universe.Log($"[UniversalBehaviour] Setup() (Interop) begin. Context={Universe.Context}, State={Universe.CurrentGlobalState}");

            if (TrySetupViaBepInExChainloader())
                return;

            Universe.LogWarning("[UniversalBehaviour] Falling back to direct typed GameObject.AddComponent<UniversalBehaviour>() setup.");
#endif

#if CPP
            ClassInjector.RegisterTypeInIl2Cpp<UniversalBehaviour>();
#endif

            GameObject obj = new("UniverseLibBehaviour");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<UniversalBehaviour>();

#if CPP && INTEROP
            if (Instance != null)
            {
                Universe.Log("[UniversalBehaviour] Successfully attached via direct typed GameObject.AddComponent<UniversalBehaviour>().");
            }
            else
            {
                Universe.LogWarning("[UniversalBehaviour] Direct typed AddComponent<UniversalBehaviour>() returned null.");
            }
#endif
        }

#if CPP && INTEROP
        private static bool TrySetupViaBepInExChainloader()
        {
            try
            {
                const string chainloaderTypeName =
                    "BepInEx.Unity.IL2CPP.IL2CPPChainloader, BepInEx.Unity.IL2CPP";

                Type chainloaderType = Type.GetType(chainloaderTypeName, throwOnError: false);
                if (chainloaderType == null)
                {
                    Universe.LogWarning("[UniversalBehaviour] IL2CPPChainloader type not found.");
                    return false;
                }

                MethodInfo addUnityComponent =
                    chainloaderType.GetMethod(
                        "AddUnityComponent",
                        BindingFlags.Public | BindingFlags.Static,
                        binder: null,
                        types: new[] { typeof(Type) },
                        modifiers: null);

                if (addUnityComponent == null)
                {
                    Universe.LogWarning("[UniversalBehaviour] AddUnityComponent(Type) overload not found.");
                    return false;
                }

                object result = addUnityComponent.Invoke(null, new object[] { typeof(UniversalBehaviour) });
                if (result != null)
                {
                    // Use UniverseLib's own casting helpers to get the concrete type.
                    Instance = result.TryCast<UniversalBehaviour>();
                }

                if (Instance != null)
                {
                    Universe.Log("[UniversalBehaviour] Successfully attached via IL2CPPChainloader.AddUnityComponent(typeof(UniversalBehaviour)).");
                    return true;
                }

                string resultType = result?.GetType().FullName ?? "<null>";
                Universe.LogWarning($"[UniversalBehaviour] AddUnityComponent(typeof(UniversalBehaviour)) returned {resultType}, not UniversalBehaviour.");
                return false;
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"[UniversalBehaviour] Exception in Setup using IL2CPPChainloader.AddUnityComponent(Type): {ex}");
                return false;
            }
        }
#endif

        internal void Update()
        {
            Universe.Update();
        }

#if CPP
        public UniversalBehaviour(IntPtr ptr) : base(ptr) { }

        static Delegate queuedDelegate;

        internal static void InvokeDelegate(Delegate method)
        {
            queuedDelegate = method;
            Instance.Invoke(nameof(InvokeQueuedAction), 0f);
        }

        void InvokeQueuedAction()
        {
            try
            {
                Delegate method = queuedDelegate;
                queuedDelegate = null;
                method?.DynamicInvoke();
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"Exception invoking action from IL2CPP thread: {ex}");
            }
        }
#endif
    }
}
