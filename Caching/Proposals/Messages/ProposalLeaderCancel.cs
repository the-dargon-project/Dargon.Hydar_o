﻿using System;
using Dargon.PortableObjects;

namespace Dargon.Hydar.Caching.Proposals.Messages {
   public class ProposalLeaderCancel : IPortableObject, IProposalMessage {
      private Guid proposalId;

      public ProposalLeaderCancel(Guid proposalId) {
         this.proposalId = proposalId;
      }

      public Guid ProposalId { get { return proposalId; } }

      public void Serialize(IPofWriter writer) {
         writer.WriteGuid(0, proposalId);
      }

      public void Deserialize(IPofReader reader) {
         proposalId = reader.ReadGuid(0);
      }
   }
}