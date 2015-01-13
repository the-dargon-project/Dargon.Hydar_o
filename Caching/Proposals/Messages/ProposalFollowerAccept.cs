using System;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Caching.Proposals.Messages {
   public class ProposalFollowerAccept : IPortableObject, IProposalMessage {
      private Guid cacheId;
      private Guid proposalId;

      public ProposalFollowerAccept(Guid cacheId, Guid proposalId) {
         this.cacheId = cacheId;
         this.proposalId = proposalId;
      }

      public Guid CacheId { get { return cacheId; } }
      public Guid ProposalId { get { return proposalId; } }

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, cacheId);
         writer.WriteGuid(1, proposalId);
      }

      public void Deserialize(IPofReader reader) {
         cacheId = reader.ReadGuid(0);
         proposalId = reader.ReadGuid(1);
      }
   }
}