//
// This file was generated by the BinaryNotes compiler.
// See http://bnotes.sourceforge.net 
// Any modifications to this file will be lost upon recompilation of the source ASN.1. 
//

using System.Runtime.CompilerServices;
using GSF.ASN1;
using GSF.ASN1.Attributes;
using GSF.ASN1.Coders;

namespace GSF.MMS.Model
{
    
    [ASN1PreparedElement]
    [ASN1Sequence(Name = "DeleteVariableAccess_Response", IsSet = false)]
    public class DeleteVariableAccess_Response : IASN1PreparedElement
    {
        private static readonly IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(DeleteVariableAccess_Response));
        private Unsigned32 numberDeleted_;
        private Unsigned32 numberMatched_;

        [ASN1Element(Name = "numberMatched", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
        public Unsigned32 NumberMatched
        {
            get
            {
                return numberMatched_;
            }
            set
            {
                numberMatched_ = value;
            }
        }


        [ASN1Element(Name = "numberDeleted", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
        public Unsigned32 NumberDeleted
        {
            get
            {
                return numberDeleted_;
            }
            set
            {
                numberDeleted_ = value;
            }
        }


        public void initWithDefaults()
        {
        }


        public IASN1PreparedElementData PreparedData
        {
            get
            {
                return preparedData;
            }
        }
    }
}