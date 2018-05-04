/********************************************************           
BEDROCKFRAMEWORK : https://github.com/GainDeveloper/BedrockFramework
Receives all data from the host.
Stores list of connections.
// TODO: Ownership, which allows connections to take control of updating.
********************************************************/
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using BedrockFramework.Pool;

namespace BedrockFramework.Network
{
    interface INetworkComponent
    {
        int NumNetVars { get; }
        void WriteUpdatedNetVars(NetworkWriter toWrite, ref bool[] updatedVars, int currentPosition);
        void ReadUpdatedNetVars(NetworkReader reader);
    }

    [HideMonoScript, AddComponentMenu("BedrockFramework/Network GameObject")]
    public class NetworkGameObject : MonoBehaviour, IPool
    {
        public byte updatesPerSecond  = 3;
        public NetworkGameObjectTransform networkTransform = new NetworkGameObjectTransform();

        [ReadOnly, ShowInInspector]
        private PoolDefinition poolDefinition;
        [ReadOnly, ShowInInspector]
        private short networkID = 0;

        private Coroutine activeLoop;
        private List<INetworkComponent> activeNetworkComponents;

        public short NetworkID { get { return networkID; } }

        void Awake()
        {
            networkTransform.observed = gameObject.transform;

            activeNetworkComponents = GetComponents<INetworkComponent>().ToList();
            if (networkTransform.enabled)
                activeNetworkComponents.Add((INetworkComponent)networkTransform);
        }

        // Pool
        PoolDefinition IPool.PoolDefinition { set { poolDefinition = value; } }

        void IPool.OnSpawn()
        {
            ServiceLocator.NetworkService.OnBecomeHost += HostGameObject;
            if (ServiceLocator.NetworkService.IsActive && ServiceLocator.NetworkService.IsHost)
                HostGameObject();
        }

        void IPool.OnDeSpawn()
        {
            ServiceLocator.NetworkService.OnBecomeHost -= HostGameObject;
            NetworkService_OnStop();
        }

        private void HostGameObject()
        {
            if (!ServiceLocator.NetworkService.IsHost)
                DevTools.Logger.LogError(NetworkService.NetworkLog, "None host is trying to host a network game object!");

            if (networkID == 0)
                networkID = ServiceLocator.NetworkService.UniqueNetworkID;

            ServiceLocator.NetworkService.AddNetworkGameObject(this);

            // Tell all active connections about our creation.
            if (ServiceLocator.NetworkService.IsActive)
            {
                Host_SendGameObject(ServiceLocator.NetworkService.ActiveSocket.ActiveConnections().Where(x => x.CurrentState == NetworkConnectionState.Ready).ToArray());
            }

            activeLoop = StartCoroutine(UpdateLoop());
            ServiceLocator.NetworkService.OnNetworkConnectionReady += OnNetworkConnectionReady;
            ServiceLocator.NetworkService.OnStop += NetworkService_OnStop;
        }

        private void NetworkService_OnStop()
        {
            if (activeLoop != null)
            {
                StopCoroutine(activeLoop);
                activeLoop = null;
            }

            ServiceLocator.NetworkService.RemoveNetworkGameObject(this);
            ServiceLocator.NetworkService.OnNetworkConnectionReady -= OnNetworkConnectionReady;
            ServiceLocator.NetworkService.OnStop -= NetworkService_OnStop;
        }

        private void OnNetworkConnectionReady(NetworkConnection readyConnection)
        {
            if (!ServiceLocator.NetworkService.IsHost)
                DevTools.Logger.LogError(NetworkService.NetworkLog, "None host is trying to handle a connection ready network game object request!");

            Host_SendGameObject(new NetworkConnection[] { readyConnection });
        }

        //
        // Initialization
        //

        void Host_SendGameObject(NetworkConnection[] receivers)
        {
            NetworkWriter writer = ServiceLocator.NetworkService.ActiveSocket.Writer.Setup(ServiceLocator.NetworkService.ActiveSocket.ReliableChannel, MessageTypes.BRF_Client_Receive_GameObject);
            writer.Write(ServiceLocator.SaveService.SavedObjectReferences.GetSavedObjectID(poolDefinition));
            writer.Write(networkID);

            //TODO: Write out everything we want to send as initial data.

            for (int i = 0; i < receivers.Length; i++)
                ServiceLocator.NetworkService.ActiveSocket.Writer.Send(receivers[i], () => "NetworkGameObject Init");
        }

        public void Client_ReceiveGameObject(NetworkReader reader)
        {
            networkID = reader.ReadInt16();
            ServiceLocator.NetworkService.AddNetworkGameObject(this);
        }

        //
        // Update Loop
        //

        int NumNetVars { get {
                int i = 0;
                foreach (INetworkComponent comp in activeNetworkComponents)
                    i += comp.NumNetVars;
                return i;
            } }

        // Collects all NetVars in active network components.
        IEnumerator UpdateLoop()
        {
            bool[] updatedNetVars = new bool[NumNetVars];

            while (true)
            {
                yield return new WaitForSecondsRealtime(1f / updatesPerSecond);

                // Write updated net vars.
                NetworkWriter writer = ServiceLocator.NetworkService.ActiveSocket.Writer.Setup(ServiceLocator.NetworkService.ActiveSocket.UnreliableChannel, MessageTypes.BRF_Client_Update_GameObject);
                writer.Write(networkID);

                int currentPosition = 0;
                for (int i = 0; i < activeNetworkComponents.Count; i++)
                {
                    activeNetworkComponents[i].WriteUpdatedNetVars(writer, ref updatedNetVars, currentPosition);
                    currentPosition += activeNetworkComponents[i].NumNetVars;
                }

                // Check if any NetVars were updated.
                bool hasUpdatedNetVar = false;
                for (int i = 0; i < updatedNetVars.Length; i++)
                {
                    if (updatedNetVars[i])
                    {
                        hasUpdatedNetVar = true;
                        break;
                    }
                }

                // Send NetVars
                if (hasUpdatedNetVar)
                {
                    foreach (NetworkConnection active in ServiceLocator.NetworkService.ActiveSocket.ActiveConnections())
                    {
                        if (active.CurrentState != NetworkConnectionState.Ready)
                            continue;

                        ServiceLocator.NetworkService.ActiveSocket.Writer.Send(active, () => "NetworkGameObject Update");
                    }
                }

                //TODO: Need to send the bool array with what has been updated.

                for (int i = 0; i < updatedNetVars.Length; i++)
                    updatedNetVars[i] = false;
            }
        }

        public void Client_ReceiveGameObjectUpdate(NetworkReader reader)
        {
            // TODO: Read the bool array with what has been updated.

            int currentPosition = 0;
            for (int i = 0; i < activeNetworkComponents.Count; i++)
            {
                activeNetworkComponents[i].ReadUpdatedNetVars(reader);
                currentPosition += activeNetworkComponents[i].NumNetVars;
            }
        }
    }
}