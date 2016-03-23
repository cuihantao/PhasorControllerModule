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
    [ASN1Sequence(Name = "VMDSupportParameters", IsSet = false)]
    public class VMDSupportParameters : IASN1PreparedElement
    {
        private static readonly IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(VMDSupportParameters));
        private MMSString extendedDerivation_;
        private MMSString localDetail_;

        [ASN1Element(Name = "localDetail", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
        public MMSString LocalDetail
        {
            get
            {
                return localDetail_;
            }
            set
            {
                localDetail_ = value;
            }
        }


        [ASN1Element(Name = "extendedDerivation", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = false)]
        public MMSString ExtendedDerivation
        {
            get
            {
                return extendedDerivation_;
            }
            set
            {
                extendedDerivation_ = value;
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