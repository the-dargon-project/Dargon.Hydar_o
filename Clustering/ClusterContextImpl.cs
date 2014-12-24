using Dargon.Hydar.Clustering.Phases;
using Dargon.Hydar.Networking;
using Dargon.Hydar.PortableObjects;
using Dargon.Hydar.Utilities;
using ItzWarty;
using ItzWarty.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dargon.Hydar.Clustering {
   public class ClusterContextImpl : ManageableClusterContext {
      public event NewEpochHandler NewEpoch;
      private readonly HydarContext context;
      private readonly PeerStatusFactory peerStatusFactory;
      private readonly ClusteringConfiguration configuration;
      private readonly NodePhaseFactory phaseFactory;
      private readonly Dictionary<Guid, EpochDescriptor> epochsById = new Dictionary<Guid, EpochDescriptor>();
      private readonly Dictionary<Guid, ManageablePeerStatus> peerStatusById = new Dictionary<Guid, ManageablePeerStatus>();
      private readonly object synchronization = new object();
      private EpochDescriptor currentEpoch = new EpochDescriptorImpl(Guid.Empty, new DateTimeInterval(),  Guid.Empty, new ItzWarty.Collections.HashSet<Guid>(), new SortedList<Guid, ManageablePeerStatus>(), Guid.Empty);
      private IPhase currentPhase;

      public ClusterContextImpl(HydarContext context, PeerStatusFactory peerStatusFactory, ClusteringConfiguration configuration, NodePhaseFactory phaseFactory) {
         this.context = context;
         this.peerStatusFactory = peerStatusFactory;
         this.configuration = configuration;
         this.phaseFactory = phaseFactory;
      }

      public void Initialize() {
         currentPhase = phaseFactory.CreateInitializationPhase();
         Transition(currentPhase);
      }

      #region ClusterContext Implementation
      public EpochDescriptor GetCurrentEpoch() {
         return currentEpoch;
      }

      public IReadOnlyDictionary<Guid, PeerStatus> GetPeerStatuses() {
         throw new NotImplementedException();
      }

      public IPhase __DebugCurrentPhase { get { return currentPhase; } }
      #endregion

      #region ManageableClusterContext Implementation
      public void Tick() {
         lock (synchronization) {
            currentPhase.Tick();
         }
      }

      public void Transition(IPhase phase) {
         lock (synchronization) {
            phase.ThrowIfNull("phase");
            Log("=> " + phase);
            currentPhase = phase;
            currentPhase.Enter();
         }
      }

      public bool Process(IRemoteIdentity senderIdentity, HydarMessage message) {
         lock (synchronization) {
            return currentPhase.Process(senderIdentity, message);
         }
      }

      public void EnterEpoch(Guid epochId, DateTimeInterval epochTimeInterval, Guid leaderGuid, IReadOnlySet<Guid> participantGuids, Guid previousEpochId) {
         lock (synchronization) {
            if (currentEpoch.Id == epochId) {
               // todo: rejoin epoch logic
               Log("Rejoining epoch " + epochId.ToString("n").Substring(0, 8));
            } else {
               var participantStatusesByGuid = participantGuids.Aggregate(new SortedList<Guid, ManageablePeerStatus>(), (list, x) => list.Add(x, GetOrCreatePeerStatus(x)));
               var epoch = currentEpoch = new EpochDescriptorImpl(epochId, epochTimeInterval, leaderGuid, participantGuids, participantStatusesByGuid, previousEpochId);
               epochsById.Add(epochId, epoch);
               foreach (var kvp in participantStatusesByGuid) {
                  var peerGuid = kvp.Key;
                  var peerStatus = kvp.Value;
                  peerStatus.HandleNewEpoch(peerGuid == leaderGuid, participantStatusesByGuid.IndexOfKey(peerGuid));
               }
               var capture = NewEpoch;
               if (capture != null) {
                  capture(epoch);
               }
            }
         }
      }

      public void HandlePeerHeartBeat(Guid peerGuid) {
         lock (synchronization) {
            GetOrCreatePeerStatus(peerGuid).HandleHeartBeat();
         }
      }

      private ManageablePeerStatus GetOrCreatePeerStatus(Guid guid) {
         lock (synchronization) {
            ManageablePeerStatus result;
            if (!peerStatusById.TryGetValue(guid, out result)) {
               result = peerStatusFactory.Create(guid);
               peerStatusById.Add(guid, result);
            }
            return result;
         }
      }
      #endregion

      public void Log(string x) {
         context.Log(context.Node.Identifier.ToString("n").Substring(0, 8) + " " + x);
      }
   }
}