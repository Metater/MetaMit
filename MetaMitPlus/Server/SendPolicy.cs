using System;
using System.Collections.Generic;
using System.Text;

namespace MetaMitPlus.Server
{
    public struct SendPolicy
    {
        public bool hasDataLengthCompressionThreshhold;
        public int dataLengthCompressionThreshold;

        public SendPolicy(bool hasDataLengthCompressionThreshhold, int dataLengthCompressionThreshold)
        {
            this.hasDataLengthCompressionThreshhold = hasDataLengthCompressionThreshhold;
            this.dataLengthCompressionThreshold = dataLengthCompressionThreshold;
        }
    }
}
