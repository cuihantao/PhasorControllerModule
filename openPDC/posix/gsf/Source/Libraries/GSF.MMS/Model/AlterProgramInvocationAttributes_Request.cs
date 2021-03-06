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
    [ASN1Sequence(Name = "AlterProgramInvocationAttributes_Request", IsSet = false)]
    public class AlterProgramInvocationAttributes_Request : IASN1PreparedElement
    {
        private static readonly IASN1PreparedElementData preparedData = CoderFactory.getInstance().newPreparedElementData(typeof(AlterProgramInvocationAttributes_Request));
        private Identifier programInvocation_;


        private StartCount startCount_;

        [ASN1Element(Name = "programInvocation", IsOptional = false, HasTag = true, Tag = 0, HasDefaultValue = false)]
        public Identifier ProgramInvocation
        {
            get
            {
                return programInvocation_;
            }
            set
            {
                programInvocation_ = value;
            }
        }

        [ASN1Element(Name = "startCount", IsOptional = false, HasTag = true, Tag = 1, HasDefaultValue = true)]
        public StartCount StartCount
        {
            get
            {
                return startCount_;
            }
            set
            {
                startCount_ = value;
            }
        }


        public void initWithDefaults()
        {
            StartCount param_StartCount =
                null;
            StartCount = param_StartCount;
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