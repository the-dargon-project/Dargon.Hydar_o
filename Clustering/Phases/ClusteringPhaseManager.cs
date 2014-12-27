﻿using Dargon.Audits;
using Dargon.Hydar.Networking;
using Dargon.Hydar.PortableObjects;
using Dargon.Hydar.Utilities;

namespace Dargon.Hydar.Clustering.Phases {
   public interface ClusteringPhaseManager {
      void Transition(IPhase phase);
      IPhase GetCurrentPhase();
   }

   public class ClusteringPhaseManagerImpl : ClusteringPhaseManager {
      private readonly DebugEventRouter debugEventRouter;
      private readonly InboundEnvelopeBusImpl inboundEnvelopeBus;
      private readonly object synchronization = new object();
      private IPhase currentPhase;

      public ClusteringPhaseManagerImpl(DebugEventRouter debugEventRouter, InboundEnvelopeBusImpl inboundEnvelopeBus) {
         this.debugEventRouter = debugEventRouter;
         this.inboundEnvelopeBus = inboundEnvelopeBus;
      }

      public void Initialize() {
         inboundEnvelopeBus.EventPosted += HandleInboundEnvelope;
      }

      private void HandleInboundEnvelope(EventBus<InboundEnvelope> sender, InboundEnvelope envelope) {
         lock (synchronization) {
            currentPhase.Process(envelope);
         }
      }

      public void Transition(IPhase phase) {
         lock (synchronization) {
            debugEventRouter.ClusteringPhaseManager_Transition(currentPhase, phase);
            currentPhase = phase;
            if (phase != null) {
               phase.Enter();
            }
         }
      }

      public IPhase GetCurrentPhase() {
         return currentPhase;
      }
   }
}