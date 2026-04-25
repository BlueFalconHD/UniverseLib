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
            // On CoreCLR-based IL2CPP loaders (like BepInEx 6 IL2CPP on macOS arm64),
            // avoid our own injection path entirely and delegate to BepInEx's
            // IL2CPPChainloader.AddUnityComponent(Type) helper, which is known to be safe.
            Universe.Log($"[UniversalBehaviour] Setup() (Interop) begin. Context={Universe.Context}, State={Universe.CurrentGlobalState}");

            try
            {
                const string chainloaderTypeName =
                    "BepInEx.Unity.IL2CPP.IL2CPPChainloader, BepInEx.Unity.IL2CPP";

                Type chainloaderType = Type.GetType(chainloaderTypeName, throwOnError: false);
                if (chainloaderType == null)
                {
                    Universe.LogWarning("[UniversalBehaviour] IL2CPPChainloader type not found; aborting Setup.");
                    return;
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
                    Universe.LogWarning("[UniversalBehaviour] AddUnityComponent(Type) overload not found; aborting Setup.");
                    return;
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
                }
                else
                {
                    Universe.LogWarning("[UniversalBehaviour] AddUnityComponent(typeof(UniversalBehaviour)) returned null or wrong type; aborting Setup.");
                }
            }
            catch (Exception ex)
            {
                Universe.LogWarning($"[UniversalBehaviour] Exception in Setup using IL2CPPChainloader.AddUnityComponent(Type): {ex}");
            }
#else
#if CPP
            ClassInjector.RegisterTypeInIl2Cpp<UniversalBehaviour>();
#endif

            GameObject obj = new("UniverseLibBehaviour");
            GameObject.DontDestroyOnLoad(obj);
            obj.hideFlags |= HideFlags.HideAndDontSave;
            Instance = obj.AddComponent<UniversalBehaviour>();
#endif
        }

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
